using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DiceLabController : MonoBehaviour {
    [Header("Targets")] public int dc = 12;
    public int tutorialDc = 6;
    [Header("Mode")] public bool soloMode = true;
    [Header("Scripted first encounter")] public int scriptedLossRolls = 4;
    public string[] firstUnlocks = { "loaded_dice", "bargain_fate", "sleight_of_hand" };

    [Header("UI")]
    public GameObject fatePanel;        // holds FateBar, TierText, MaliceText
    public GameObject moraleRoot;       // holds MoraleBar
    public Slider fateBar, suspicionBar, moraleBar;      // Min 0, Max 1
    public TMP_Text tierText, maliceText, oddsText, audacityText, goldText, logText;
    public Image[] diceSlots;           // 5 squares, each with a TMP_Text child
    public Button rollBtn, loadedBtn, sleightBtn, bargainBtn, fateUpBtn, fateDownBtn,
                  finishTutorialBtn, forceEndBtn;
    public Button endEncounterBtn;      // optional: applies the per-encounter Suspicion decay

    FateState s;
    ProgressState progress;
    HeroUnit hero;
    int removedDice, firstRolls;
    bool sleightArmed, rolling, cheatedThisEncounter, failedThisEncounter;

    int CurrentDc => progress.Phase == GamePhase.Tutorial ? tutorialDc : dc;
    int RollMod => hero.AttackModifier + s.MoraleModifier;
    int CurrentDice => Mathf.Max(1, s.TotalDice - removedDice);

    void Start() {
        progress = new ProgressState();
        s = new FateState { SoloMode = soloMode };
        hero = new HeroUnit(GameManager.Instance.Config.classes.Default);
        s.Audacity = hero.Class.startingAudacity;
        s.Changed += Refresh;

        rollBtn.onClick.AddListener(() => StartCoroutine(DoRoll()));
        loadedBtn.onClick.AddListener(UseLoadedDice);
        sleightBtn.onClick.AddListener(UseSleight);
        bargainBtn.onClick.AddListener(UseBargain);
        fateUpBtn.onClick.AddListener(() => s.AddFate(10));
        fateDownBtn.onClick.AddListener(() => s.AddFate(-10));
        finishTutorialBtn.onClick.AddListener(() => {
            progress.Phase = GamePhase.FirstEncounter; ApplyPhase();
            Log("First encounter. Good luck.");
        });
        forceEndBtn.onClick.AddListener(OnFirstEncounterEnd);
        if (endEncounterBtn != null) endEncounterBtn.onClick.AddListener(EndEncounter);

        ApplyPhase();
        Log("Tutorial: hit the dummy.");
    }

    void ApplyPhase() {
        s.SetPhase(progress.Phase);
        fatePanel.SetActive(progress.FateBarRevealed);
        moraleRoot.SetActive(!soloMode && progress.Phase == GamePhase.Normal);
        finishTutorialBtn.gameObject.SetActive(progress.Phase == GamePhase.Tutorial);
        forceEndBtn.gameObject.SetActive(progress.Phase == GamePhase.FirstEncounter);
        Refresh();
    }

    void Refresh() {
        fateBar.value = s.Fate / 100f;
        suspicionBar.value = s.Suspicion / 100f;
        moraleBar.value = s.Morale / 100f;
        tierText.text = s.TierName;
        maliceText.text = $"Malice dice: {s.MaliceDice}  (rolling {CurrentDice})";

        // Odds stay hidden during the scripted first encounter ("rigged but legible" starts at the reveal)
        oddsText.gameObject.SetActive(progress.Phase != GamePhase.FirstEncounter);
        float p = Odds.HitChance(CurrentDc, RollMod, CurrentDice) * 100f;
        oddsText.text = $"Hit chance: {p:0.0}%  (need {CurrentDc - RollMod}+ on die)";

        audacityText.text = $"Audacity: {s.Audacity}";
        goldText.text = $"Gold: {s.Gold}";
        loadedBtn.gameObject.SetActive(progress.IsUnlocked("loaded_dice"));
        sleightBtn.gameObject.SetActive(progress.IsUnlocked("sleight_of_hand"));
        bargainBtn.gameObject.SetActive(progress.IsUnlocked("bargain_fate"));
        fateUpBtn.gameObject.SetActive(progress.Phase != GamePhase.Tutorial);
        fateDownBtn.gameObject.SetActive(progress.Phase != GamePhase.Tutorial);
    }

    bool TryPlayCheat(string cheatId, int baseCost, string label) {
        int cost = hero.CheatCost(cheatId, baseCost) + s.CheatCostBump;
        if (rolling || s.Audacity < cost) { Log($"Need {cost} Audacity."); return false; }
        s.Audacity -= cost;
        bool pass = s.SuspicionCheck(hero.Cunning);
        s.AddMorale(-3);
        cheatedThisEncounter = true;
        if (!pass) failedThisEncounter = true;
        Log(pass ? $"{label}: unnoticed." : $"{label}: the GM saw that!");
        return true;
    }

    void UseLoadedDice() {
        if (removedDice == 0 && TryPlayCheat("loaded_dice", 1, "Loaded Dice")) { removedDice = 2; Refresh(); }
    }

    void UseSleight() {
        if (!sleightArmed && TryPlayCheat("sleight_of_hand", 2, "Sleight of Hand")) { sleightArmed = true; Refresh(); }
    }

    void UseBargain() {
        if (rolling) return;
        int cost = s.BargainCost;                // 2 gold per point of Fate; no Suspicion check
        if (s.Gold < cost) { Log($"Need {cost} gold."); return; }
        s.Gold -= cost;
        s.Fate = 0;
        s.Notify();
        Log($"Bargained with Fate for {cost} gold.");
    }

    RollResult OneRoll(int n, int needed, bool scripted) =>
        scripted ? DiceService.RollForcedMiss(n, needed) : DiceService.RollMalice(n);

    IEnumerator DoRoll() {
        if (rolling) yield break;
        rolling = true;

        int n = CurrentDice;
        int needed = CurrentDc - RollMod;
        bool scripted = progress.Phase == GamePhase.FirstEncounter;

        var result = OneRoll(n, needed, scripted);
        if (sleightArmed) {                      // reroll, keep the better result
            var second = OneRoll(n, needed, scripted);
            if (second.Lowest > result.Lowest) result = second;
        }

        // placeholder tumble
        for (float t = 0; t < 0.6f; t += 0.06f) {
            for (int i = 0; i < diceSlots.Length; i++) SetDie(i, i < n ? Random.Range(1, 21) : -1, false);   // cosmetic only, not the seeded stream
            yield return new WaitForSeconds(0.06f);
        }
        for (int i = 0; i < diceSlots.Length; i++) {
            bool lowest = i < n && result.Dice[i] == result.Lowest;
            SetDie(i, i < n ? result.Dice[i] : -1, lowest);
        }

        int total = result.Lowest + RollMod;
        bool success = total >= CurrentDc;
        s.ApplyRollOutcome(success, total - CurrentDc, result.Fumble);
        Log(result.Fumble ? $"FUMBLE! ({total} vs DC {CurrentDc})"
          : success ? $"Hit! {total} vs DC {CurrentDc}" : $"Miss. {total} vs DC {CurrentDc}");

        // end-of-round stand-in
        removedDice = 0; sleightArmed = false;
        s.Audacity = Mathf.Min(5, s.Audacity + 1);

        Refresh();
        rolling = false;

        if (scripted && ++firstRolls >= scriptedLossRolls) OnFirstEncounterEnd();
    }

    void EndEncounter() {
        s.EndEncounter(cheatedThisEncounter, failedThisEncounter);
        cheatedThisEncounter = failedThisEncounter = false;
        Log("Encounter over. Suspicion decay applied.");
    }

    void OnFirstEncounterEnd() {
        if (progress.Phase != GamePhase.FirstEncounter) return;
        Log("Party wipe. Destiny: \"Is that really the best you can do?\"");
        progress.Phase = GamePhase.Normal;
        foreach (var id in firstUnlocks) progress.Unlock(id);
        ApplyPhase();
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
