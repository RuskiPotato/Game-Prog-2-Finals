using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum BattleState { Initiative, HeroTurn, Roll, Resolve, EnemyTurn, RoundEnd, Victory, Defeat }

// One hero vs one enemy. Handles the Tutorial (Dummy Target), the scripted First Encounter,
// and normal run fights. Reads everything from GameManager.
public class BattleController : MonoBehaviour {
    [Header("Scripted first encounter")]
    public int scriptedLossRounds = 4;
    [TextArea] public string[] taunts = {
        "Oh, was that supposed to hit?",
        "Roll again. It will not help.",
        "Adorable.",
        "The dice and I have an understanding."
    };

    [Header("Placeholders")]
    public int snackHeal = 10;      // used when a Snack Break node is skipped (map not built yet)

    [Header("UI")]
    public GameObject fatePanel;
    public GameObject reactionPanel;      // holds AcceptBtn and RerollBtn; start inactive
    public Slider fateBar, suspicionBar;  // Max Value 1
    public TMP_Text tierText, maliceText, oddsText, audacityText, goldText,
                    heroText, enemyText, logText, gmText;
    public Image[] diceSlots;             // 5 squares, each with a TMP_Text child
    public Button attackBtn, defendBtn, loadedBtn, sleightBtn, bargainBtn,
                  acceptBtn, rerollBtn, continueBtn;

    GameManager gm;
    FateState fate;
    ProgressState progress;
    HeroUnit hero;
    EnemyData enemy;

    enum Choice { None, Attack, Defend }
    enum Reaction { None, Accept, Reroll }

    BattleState state;
    Choice chosen;
    Reaction reaction;
    RollResult lastRoll;
    int enemyHp, round, removedDice;
    bool sleightArmed, defending, continuePressed, cheatedThisFight, failedThisFight;

    bool Scripted => progress.Phase == GamePhase.FirstEncounter;
    bool CanAct => state == BattleState.HeroTurn && chosen == Choice.None;
    int RollMod => hero.AttackModifier + fate.MoraleModifier;
    int CurrentDice => Mathf.Max(1, fate.TotalDice - removedDice);

    // ---------- setup ----------

    void Start() {
        gm = GameManager.Instance;
        if (gm.Fate == null) gm.NewRun();          // pressing Play inside Battle starts a fresh run
        fate = gm.Fate; progress = gm.Progress; hero = gm.Hero;

        reactionPanel.SetActive(false);
        continueBtn.gameObject.SetActive(false);

        attackBtn.onClick.AddListener(() => Choose(Choice.Attack));
        defendBtn.onClick.AddListener(() => Choose(Choice.Defend));
        loadedBtn.onClick.AddListener(UseLoadedDice);
        sleightBtn.onClick.AddListener(UseSleight);
        bargainBtn.onClick.AddListener(UseBargain);
        acceptBtn.onClick.AddListener(() => reaction = Reaction.Accept);
        rerollBtn.onClick.AddListener(() => reaction = Reaction.Reroll);
        continueBtn.onClick.AddListener(() => continuePressed = true);

        if (progress.Phase == GamePhase.Normal) SkipNonCombatNodes();
        enemy = PickEnemy();
        if (enemy == null) {
            logText.text = "No enemy found. Run Paperboard God > Create Default Assets.";
            return;
        }
        fate.Changed += Refresh;                   // unsubscribed in OnDestroy (fate outlives this scene)
        StartCoroutine(BattleLoop());
    }

    void OnDestroy() { if (fate != null) fate.Changed -= Refresh; }

    EnemyData PickEnemy() {
        var cfg = gm.Config;
        if (progress.Phase == GamePhase.Tutorial) return cfg.dummy;
        if (progress.Phase == GamePhase.FirstEncounter) return cfg.scriptedBrute;
        if (gm.Run.Finished) return null;

        var node = gm.Run.Current;
        if (node.type == NodeType.Boss && cfg.bosses.Count > 0)
            return cfg.bosses[Mathf.Min(node.act, cfg.bosses.Count - 1)];
        var list = (node.type == NodeType.Elite && cfg.elites.Count > 0) ? cfg.elites : cfg.fights;
        if (list.Count == 0) return null;
        return list[Rng.Get(RngStream.Events).Next(list.Count)];
    }

    // Placeholder until the map screen exists: skip Shop / Snack Break / Rules Dispute nodes.
    void SkipNonCombatNodes() {
        var run = gm.Run;
        while (!run.Finished && !run.Current.IsCombat) {
            var n = run.Current;
            if (n.type == NodeType.SnackBreak) {
                hero.Hp = Mathf.Min(hero.MaxHp, hero.Hp + snackHeal);
                fate.AddSuspicion(-10);
            }
            Debug.Log($"[placeholder] skipped {n.type} node (act {n.act + 1}, node {n.index + 1})");
            run.CompleteCurrent();
        }
    }

    // ---------- flow ----------

    IEnumerator BattleLoop() {
        state = BattleState.Initiative;
        enemyHp = enemy.hp;
        fate.Audacity = hero.Class.startingAudacity;      // Audacity resets at each encounter start
        Refresh();

        int heroInit = DiceService.D20() + hero.Agi;
        int enemyInit = DiceService.D20() + enemy.agi;
        bool heroFirst = Scripted || heroInit >= enemyInit;
        Log($"{enemy.displayName} appears. Initiative: you {heroInit}, them {enemyInit}.");
        yield return new WaitForSeconds(0.8f);

        while (true) {
            round++;
            for (int i = 0; i < 2; i++) {
                bool heroActs = (i == 0) == heroFirst;
                if (heroActs) yield return HeroTurn(); else yield return EnemyTurn();

                if (hero.Hp <= 0) { yield return OnDefeat(); yield break; }
                if (enemyHp <= 0) { yield return OnVictory(); yield break; }
            }
            state = BattleState.RoundEnd;
            fate.Audacity = Mathf.Min(5, fate.Audacity + 1);
            Refresh();
        }
    }

    IEnumerator HeroTurn() {
        state = BattleState.HeroTurn;
        chosen = Choice.None; removedDice = 0; sleightArmed = false;
        Log("Your turn.");
        Refresh();

        yield return new WaitUntil(() => chosen != Choice.None);

        if (chosen == Choice.Attack) {
            yield return DoAttack();
        } else {
            defending = true;
            fate.Audacity = Mathf.Min(5, fate.Audacity + 1);
            Log("You brace. +1 Audacity.");
            Refresh();
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator DoAttack() {
        state = BattleState.Roll;
        int mod = RollMod;
        int dc = enemy.defenseDc;
        int needed = dc - mod;
        int n = CurrentDice;
        bool boosted = removedDice > 0 || sleightArmed;

        yield return RollHero(n, needed, true);
        state = BattleState.Resolve;
        var r = lastRoll;
        int total = r.Lowest + mod;
        bool success = total >= dc;

        // Reaction window: Loaded Dice after the roll cancels a miss and rerolls once
        if (!success && CanLoadedReroll()) {
            reaction = Reaction.None;
            reactionPanel.SetActive(true);
            Log($"Miss ({total} vs {dc}). Accept it, or cancel it with Loaded Dice?");
            yield return new WaitUntil(() => reaction != Reaction.None);
            reactionPanel.SetActive(false);
            if (reaction == Reaction.Reroll && PayCheat("loaded_dice", 1, "Loaded Dice (cancel and reroll)")) {
                boosted = true;
                yield return RollHero(n, needed, false);
                r = lastRoll;
                total = r.Lowest + mod;
                success = total >= dc;
            }
        }

        bool defiant = success && boosted && total - dc >= 10;
        if (defiant) fate.AddFate(fate.DefiantCritDelta);
        else fate.ApplyRollOutcome(success, total - dc, r.Fumble);

        if (success) {
            int dmg = DiceService.Die(hero.Class.weaponDie) + hero.AttackStatValue;
            if (defiant) dmg *= 2;
            enemyHp = Mathf.Max(0, enemyHp - dmg);
            Log(defiant ? $"DEFIANT CRIT! {total} vs {dc}. {dmg} damage."
                        : $"Hit! {total} vs {dc}. {dmg} damage.");
            if (enemyHp == 0) fate.AddFate(fate.EnemyDefeatedDelta);
        } else {
            Log(r.Fumble ? $"FUMBLE! ({total} vs {dc})" : $"Miss. {total} vs {dc}");
            if (progress.Phase != GamePhase.Tutorial)
                gmText.text = taunts[Random.Range(0, taunts.Length)];
        }
        Refresh();
        yield return new WaitForSeconds(0.6f);
    }

    IEnumerator RollHero(int n, int needed, bool allowSleight) {
        bool scripted = Scripted;
        lastRoll = scripted ? DiceService.RollForcedMiss(n, needed) : DiceService.RollMalice(n);
        if (allowSleight && sleightArmed) {          // reroll, keep the better result
            var second = scripted ? DiceService.RollForcedMiss(n, needed) : DiceService.RollMalice(n);
            if (second.Lowest > lastRoll.Lowest) lastRoll = second;
        }

        for (float t = 0; t < 0.6f; t += 0.06f) {    // cosmetic tumble: Unity's random, not the seeded stream
            for (int i = 0; i < diceSlots.Length; i++) SetDie(i, i < n ? Random.Range(1, 21) : -1, false);
            yield return new WaitForSeconds(0.06f);
        }
        for (int i = 0; i < diceSlots.Length; i++) {
            bool lowest = i < n && lastRoll.Dice[i] == lastRoll.Lowest;
            SetDie(i, i < n ? lastRoll.Dice[i] : -1, lowest);
        }
    }

    IEnumerator EnemyTurn() {
        state = BattleState.EnemyTurn;
        Refresh();
        yield return new WaitForSeconds(0.4f);

        if (!enemy.attacks) {
            Log($"{enemy.displayName} just stands there.");
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        bool hit;
        int dmg = 0;
        if (Scripted) {
            hit = true;
            dmg = Balance.ScriptedLossDamage(hero.MaxHp, hero.Hp, scriptedLossRounds, round, defending);
            Log($"{enemy.displayName} hits you for {dmg}.");
        } else {
            int dice = 1 + fate.MaliceDice;                       // Destiny's favor: extra dice, keep highest
            int total = DiceService.RollHighest(dice) + enemy.attackMod;
            int def = Balance.HeroDefenseDc(hero.Agi);
            hit = total >= def;
            if (hit) {
                dmg = DiceService.Die(enemy.damageDie) + enemy.damageBonus;
                if (defending) dmg = (dmg + 1) / 2;
            }
            Log(hit ? $"{enemy.displayName} rolls {total} vs {def}: hits for {dmg}."
                    : $"{enemy.displayName} rolls {total} vs {def}: misses.");
        }
        defending = false;

        if (hit) {
            hero.Hp = Mathf.Max(0, hero.Hp - dmg);
            if (hero.Hp == 0) fate.AddFate(fate.HeroKODelta);
            // Later: Flashback window goes here, before the knockout resolves
        }
        Refresh();
        yield return new WaitForSeconds(0.5f);
    }

    // ---------- endings ----------

    IEnumerator OnVictory() {
        state = BattleState.Victory;
        Refresh();
        fate.EndEncounter(cheatedThisFight, failedThisFight);

        if (progress.Phase == GamePhase.Tutorial) {
            yield return WaitContinue("Dummy defeated. Party setup: solo Rogue (party setup comes later).");
            progress.Phase = GamePhase.FirstEncounter;
            fate.SetPhase(GamePhase.FirstEncounter);          // Fate 70, bar still hidden
            gm.LoadScene("Battle");
            yield break;
        }
        if (Scripted) { yield return OnDefeat(); yield break; }   // should be impossible

        fate.Gold += enemy.goldReward;
        var run = gm.Run;
        int act = run.Current.act;
        run.CompleteCurrent();
        string extra = "";
        if (!run.Finished && run.Current.act != act) {
            fate.Suspicion = 0; fate.Notify();
            extra = " New act: Suspicion resets.";
        }
        yield return WaitContinue($"{enemy.displayName} defeated. +{enemy.goldReward} gold.{extra}");

        if (run.Finished) {
            yield return WaitContinue("Run complete! Destiny's table is yours.");
            gm.LoadScene("MainMenu");
        } else {
            gm.LoadScene("Battle");
        }
    }

    IEnumerator OnDefeat() {
        state = BattleState.Defeat;
        Refresh();

        if (Scripted) {
            gmText.text = "Destiny: \"Is that really the best you can do?\"";
            yield return WaitContinue("Table Wipe.");

            progress.Phase = GamePhase.Normal;                // reveal: bar appears, first cheats unlock
            foreach (var id in GameManager.FirstUnlocks) progress.Unlock(id);
            fate.SetPhase(GamePhase.Normal);
            hero.Hp = hero.MaxHp;
            Refresh();
            yield return WaitContinue("The Fate bar appears. Destiny is rigging every roll. Time to cheat back. " +
                                      "Unlocked: Loaded Dice, Bargain with Fate, Sleight of Hand.");
            gm.LoadScene("Battle");
        } else {
            yield return WaitContinue("Table Wipe. The run is over.");
            gm.LoadScene("MainMenu");
        }
    }

    IEnumerator WaitContinue(string message) {
        Log(message);
        continuePressed = false;
        continueBtn.gameObject.SetActive(true);
        yield return new WaitUntil(() => continuePressed);
        continueBtn.gameObject.SetActive(false);
    }

    // ---------- player input ----------

    void Choose(Choice c) {
        if (!CanAct) return;
        chosen = c;
        Refresh();
    }

    bool PayCheat(string id, int baseCost, string label) {
        int cost = hero.CheatCost(id, baseCost) + fate.CheatCostBump;
        if (fate.Audacity < cost) { Log($"Need {cost} Audacity."); return false; }
        fate.Audacity -= cost;
        bool pass = fate.SuspicionCheck(hero.Cunning);
        fate.AddMorale(-3);
        cheatedThisFight = true;
        if (!pass) failedThisFight = true;
        Log(pass ? $"{label}: unnoticed." : $"{label}: the GM saw that!");
        return true;
    }

    bool CanLoadedReroll() =>
        progress.IsUnlocked("loaded_dice") &&
        fate.Audacity >= hero.CheatCost("loaded_dice", 1) + fate.CheatCostBump;

    void UseLoadedDice() {
        if (CanAct && removedDice == 0 && PayCheat("loaded_dice", 1, "Loaded Dice")) { removedDice = 2; Refresh(); }
    }

    void UseSleight() {
        if (CanAct && !sleightArmed && PayCheat("sleight_of_hand", 2, "Sleight of Hand")) { sleightArmed = true; Refresh(); }
    }

    void UseBargain() {
        if (!CanAct) return;
        int cost = fate.BargainCost;               // 2 gold per point of Fate; no Suspicion check
        if (fate.Gold < cost) { Log($"Need {cost} gold."); return; }
        fate.Gold -= cost;
        fate.Fate = 0;
        fate.Notify();
        Log($"Bargained with Fate for {cost} gold.");
    }

    // ---------- HUD ----------

    void Refresh() {
        if (enemy == null || hero == null) return;
        fatePanel.SetActive(progress.FateBarRevealed);
        fateBar.value = fate.Fate / 100f;
        suspicionBar.value = fate.Suspicion / 100f;
        tierText.text = fate.TierName;
        maliceText.text = $"Malice dice: {fate.MaliceDice}  (rolling {CurrentDice})";

        oddsText.gameObject.SetActive(!Scripted);       // odds stay hidden until the reveal
        float p = Odds.HitChance(enemy.defenseDc, RollMod, CurrentDice) * 100f;
        oddsText.text = $"Hit chance: {p:0.0}%  (need {enemy.defenseDc - RollMod}+ on die)";

        audacityText.text = $"Audacity: {fate.Audacity}";
        goldText.text = $"Gold: {fate.Gold}";
        heroText.text = $"{hero.Class.displayName}  HP {hero.Hp}/{hero.MaxHp}";
        enemyText.text = Scripted ? enemy.displayName : $"{enemy.displayName}  HP {enemyHp}/{enemy.hp}";

        bool act = CanAct;
        attackBtn.interactable = act;
        defendBtn.interactable = act;
        loadedBtn.gameObject.SetActive(progress.IsUnlocked("loaded_dice"));
        sleightBtn.gameObject.SetActive(progress.IsUnlocked("sleight_of_hand"));
        bargainBtn.gameObject.SetActive(progress.IsUnlocked("bargain_fate"));
        loadedBtn.interactable = act;
        sleightBtn.interactable = act;
        bargainBtn.interactable = act;
    }

    void SetDie(int i, int v, bool lowest) {
        var img = diceSlots[i];
        img.gameObject.SetActive(v >= 0);
        if (v < 0) return;
        img.GetComponentInChildren<TMP_Text>().text = v.ToString();
        img.color = lowest ? new Color(0.7f, 0.3f, 1f) : Color.white;
    }

    void Log(string m) => logText.text = m;
}
