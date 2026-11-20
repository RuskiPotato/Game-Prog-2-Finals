using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Everything on screen. No game rules here: it shows a BattleContext and raises events.
//
// Layout (see README_Layout.txt):
//   left edge  : Morale bar (vertical)          right edge : Suspicion bar (vertical)
//   top centre : Fate diamond, Gold beside it
//   gap between Morale bar and Fate     : hero profile (name, HP, Audacity pips)
//   gap between Fate and Suspicion bar  : enemy profile (name, HP)
//   under the field: turn-order text row
public class BattleHud : MonoBehaviour, IBattleView {
    [Header(" Fate (hidden until the reveal)")]
    public GameObject fatePanel;               // holds the diamond + texts
    public DiamondMeter fateMeter;             // fills 0-100
    public TMP_Text fateValueText;             // 70
    public TMP_Text tierText;                  // Furious
    public TMP_Text maliceText;                // "Malice dice: 3 (rolling 4)"
    public TMP_Text goldText;                  // beside the Fate frame; keep it OUTSIDE fatePanel so it is always visible

    [Header("Suspicion")]
    public GameObject suspicionPanel;
    public Slider suspicionBar;                // Min 0, Max 1, Direction Bottom To Top
    public TMP_Text suspicionTierText;         // Watching
    public TMP_Text suspicionDetailText;       // 30/100  -  check DC 11

    [Header("Morale")]
    public GameObject moralePanel;             // hidden until the reveal, and always hidden in solo (FateState.SoloMode)
    public Slider moraleBar;                   // Min 0, Max 1, Direction Bottom To Top
    public TMP_Text moraleTierText;            // Steady
    public TMP_Text moraleDetailText;          // 55/100  -  rolls +0

    [Header("Profiles (the gaps beside the Fate frame)")]
    public GameObject heroProfilePanel;        // between Morale bar and Fate
    public TMP_Text heroText;                  // "Rogue  HP 24/30"
    public Slider heroHpBar;                   // optional, Min 0, Max 1
    public AudacityPips audacityPips;          // optional
    public TMP_Text audacityText;              // optional, "Audacity: 3"
    public bool heroProfileAlwaysVisible = false;   // false: only while the hero is selected

    public GameObject enemyProfilePanel;       // between Fate and Suspicion bar; only while the enemy is selected
    public TMP_Text enemyText;                 // "Goblin Hustler  HP 9/14" (no HP in the scripted fight)
    public Slider enemyHpBar;                  // optional, hidden in the scripted fight

    [Header("Game Master taunt")]
    public GameObject gmPanel;
    public TMP_Text gmText;
    public float gmAutoHideSeconds = 0f;       // 0 = stays until the next line

    [Header("Field")]
    public TMP_Text logText;
    public TMP_Text oddsText;                  // optional
    public DiceTray diceTray;                  // the hero's real dice, above the hero
    public DiceTray enemyDiceTray;             // the enemy's VISUAL-ONLY dice, above the enemy (optional)
    public TMP_Text turnOrderText;             // "Round 2   Rogue > Goblin Hustler" (active unit highlighted)
    public string activeColorHex = "FFD35A";
    public string turnOrderSeparator = "  vs  ";
    public bool showEnemyLandChance = false;    // Real mode: show "X% to land if you miss" on the enemy card

    [Header("Clash cards")]
    public GameObject heroActionPanel;         // shows what the hero committed to; hidden until they choose
    public TMP_Text heroActionText;            // "Attack  -  27.5% to hit"
    public GameObject enemyActionPanel;        // the enemy's telegraphed attack
    public TMP_Text enemyActionText;           // "Goblin Claw\nd4+1 damage\n4 dice"

    [Header("Passives (side panels, shown only while that unit is selected)")]
    public UnitSelectable heroSelectable, enemySelectable;
    public PassivesPanel heroPassives, enemyPassives;

    [Header("Hand")]
    public HandView hand;

    [Header("Pop-ups")]
    public GameObject reactionPanel;
    public Button acceptBtn, rerollBtn, continueBtn;

    public event Action AcceptClicked, RerollClicked, ContinueClicked;
    public event Action<ICard> CardClicked;

    enum Selected { None, Hero, Enemy }
    Selected selected = Selected.None;
    BattleContext lastCtx;
    Coroutine gmHide;
    List<Unit> turnOrder = new List<Unit>();

    static void SetText(TMP_Text t, string s) { if (t != null) t.text = s; }
    static void SetActive(GameObject g, bool on) { if (g != null && g.activeSelf != on) g.SetActive(on); }

    void Awake() {
        acceptBtn.onClick.AddListener(() => AcceptClicked?.Invoke());
        rerollBtn.onClick.AddListener(() => RerollClicked?.Invoke());
        continueBtn.onClick.AddListener(() => ContinueClicked?.Invoke());
        hand.CardClicked += card => CardClicked?.Invoke(card);
        reactionPanel.SetActive(false);
        continueBtn.gameObject.SetActive(false);

        if (heroSelectable != null) heroSelectable.Clicked += _ => Select(Selected.Hero);
        if (enemySelectable != null) enemySelectable.Clicked += _ => Select(Selected.Enemy);
        SetActive(gmPanel, false);
        if (heroPassives != null) heroPassives.Hide();
        if (enemyPassives != null) enemyPassives.Hide();
        SetActive(heroProfilePanel, heroProfileAlwaysVisible);
        SetActive(enemyProfilePanel, false);
    }

    void Update() {
        if (selected != Selected.None && Input.GetKeyDown(KeyCode.Escape)) Select(Selected.None);
    }

    public void Initialize(IEnumerable<ICard> cards) => hand.Build(cards);

    // Called by BattleController once initiative is rolled.
    public void SetTurnOrder(List<Unit> order) {
        turnOrder = order ?? new List<Unit>();
        if (lastCtx != null) RefreshTurnOrder(lastCtx);
    }

    // Clicking the selected unit again, or pressing Esc, clears the selection.
    void Select(Selected which) {
        selected = selected == which ? Selected.None : which;
        if (lastCtx != null) RefreshSelection(lastCtx);
    }

    public void Refresh(BattleContext ctx) {
        lastCtx = ctx;
        var fate = ctx.Fate;
        bool revealed = ctx.Progress.FateBarRevealed;

        // Fate: diamond
        SetActive(fatePanel, revealed);
        if (fateMeter != null) fateMeter.SetValue(fate.Fate / 100f);
        SetText(fateValueText, fate.Fate.ToString());
        SetText(tierText, fate.TierName);
        SetText(maliceText, $"Malice dice: {fate.MaliceDice}  (rolling {ctx.CurrentDice})");
        SetText(goldText, $"Gold: {fate.Gold}");

        // Suspicion: right-edge bar
        SetActive(suspicionPanel, revealed);
        if (suspicionBar != null) suspicionBar.value = fate.Suspicion / 100f;
        var sTier = SuspicionInfo.TierOf(fate.Suspicion);
        SetText(suspicionTierText, sTier.ToString());
        SetText(suspicionDetailText, $"{fate.Suspicion}/100  -  check DC {SuspicionInfo.CheckDc(fate.Suspicion)}");

        // Morale: left-edge bar (party only)
        SetActive(moralePanel, revealed && !fate.SoloMode);
        if (moraleBar != null) moraleBar.value = fate.Morale / 100f;
        string mName = MoraleName(fate.Morale);
        SetText(moraleTierText, mName);
        SetText(moraleDetailText, $"{fate.Morale}/100  -  rolls {fate.MoraleModifier:+0;-0;+0}");

        if (oddsText != null) {
            oddsText.gameObject.SetActive(!ctx.Scripted);            // odds hidden until the reveal
            float p = Odds.HitChance(ctx.Enemy.DefenseDc, ctx.RollMod, ctx.CurrentDice) * 100f;
            oddsText.text = $"Hit chance: {p:0.0}%  (need {ctx.Enemy.DefenseDc - ctx.RollMod}+ on die)";
        }

        // Profiles
        SetText(heroText, $"{ctx.Hero.DisplayName}  HP {ctx.Hero.Hp}/{ctx.Hero.MaxHp}");
        if (heroHpBar != null) heroHpBar.value = ctx.Hero.MaxHp > 0 ? (float)ctx.Hero.Hp / ctx.Hero.MaxHp : 0f;
        SetText(audacityText, $"Audacity: {fate.Audacity}");
        if (audacityPips != null) audacityPips.Set(fate.Audacity);

        SetText(enemyText, ctx.Scripted ? ctx.Enemy.DisplayName
                                        : $"{ctx.Enemy.DisplayName}  HP {ctx.Enemy.Hp}/{ctx.Enemy.MaxHp}");
        if (enemyHpBar != null) {
            enemyHpBar.gameObject.SetActive(!ctx.Scripted);          // enemy HP stays hidden in the scripted fight
            enemyHpBar.value = ctx.Enemy.MaxHp > 0 ? (float)ctx.Enemy.Hp / ctx.Enemy.MaxHp : 0f;
        }

        if (ctx.State == BattleState.HeroTurn) {               // new round: wipe last round's dice
            if (diceTray != null) diceTray.Clear();
            if (enemyDiceTray != null) enemyDiceTray.Clear();
        }
        RefreshClashCards(ctx);
        RefreshSelection(ctx);
        RefreshTurnOrder(ctx);
        hand.Refresh(ctx);
    }

    // ---- tiers ----
    // Same thresholds as FateState.MoraleModifier: 61+, 50-60, 30-49, 0-29
    static string MoraleName(int morale) =>
        morale >= 61 ? "Confident" : morale >= 50 ? "Steady" : morale >= 30 ? "Wavering" : "Breaking";

    // ---- clash cards ----
    void RefreshClashCards(BattleContext ctx) {
        if (enemyActionText != null) {
            var d = ctx.Enemy.Data;
            string name = string.IsNullOrEmpty(d.attackName) ? "Attack" : d.attackName;
            SetActive(enemyActionPanel, d.attacks);
            enemyActionText.text = ctx.Scripted
                ? name
                : $"{name}\nd{d.damageDie}{d.damageBonus:+0;-0;+0} damage\n{1 + ctx.Fate.MaliceDice} dice, keep highest"
                  + (showEnemyLandChance
                      ? $"\n{Odds.EnemyHitChance(ctx.Hero.DefenseDc, ctx.Enemy.AttackModifier, 1 + ctx.Fate.MaliceDice) * 100f:0.0}% to land if you miss"
                      : "");
        }

        bool committed = ctx.ChosenAction != PlayerAction.None;
        SetActive(heroActionPanel, committed);
        if (heroActionText != null && committed) {
            if (ctx.ChosenAction == PlayerAction.Attack)
                heroActionText.text = ctx.Scripted ? "Attack"
                    : $"Attack\n{Odds.HitChance(ctx.Enemy.DefenseDc, ctx.RollMod, ctx.CurrentDice) * 100f:0.0}% to hit";
            else heroActionText.text = "Defend\nHalve the hit";
        }
    }

    // ---- selection: profiles + passives ----
    void RefreshSelection(BattleContext ctx) {
        SetActive(heroProfilePanel, heroProfileAlwaysVisible || selected == Selected.Hero);
        SetActive(enemyProfilePanel, selected == Selected.Enemy);
        if (heroSelectable != null) heroSelectable.SetSelected(selected == Selected.Hero);
        if (enemySelectable != null) enemySelectable.SetSelected(selected == Selected.Enemy);

        if (selected == Selected.Hero && heroPassives != null) {
            var lines = new List<string>(ctx.Hero.Class.passives);
            if (ctx.Progress.FateBarRevealed && ctx.Fate.MaliceDice > 0)
                lines.Insert(0, $"Jinxed: Destiny adds {ctx.Fate.MaliceDice} Malice dice (keep lowest)");
            heroPassives.Show(ctx.Hero.DisplayName, lines);
        } else if (heroPassives != null) heroPassives.Hide();

        if (selected == Selected.Enemy && enemyPassives != null)
            enemyPassives.Show(ctx.Enemy.DisplayName, ctx.Enemy.Data.passives);
        else if (enemyPassives != null) enemyPassives.Hide();
    }

    // ---- turn order: "Round 2   Rogue > Goblin Hustler", active unit highlighted ----
    void RefreshTurnOrder(BattleContext ctx) {
        if (turnOrderText == null) return;
        if (turnOrder.Count == 0) { turnOrderText.text = ""; return; }

        Unit active = null;
        switch (ctx.State) {
            case BattleState.HeroTurn: case BattleState.Roll: case BattleState.Resolve: active = ctx.Hero; break;
            case BattleState.EnemyTurn: active = ctx.Enemy; break;
        }

        var sb = new StringBuilder();
        if (ctx.Round > 0) sb.Append("Round ").Append(ctx.Round).Append("   ");
        for (int i = 0; i < turnOrder.Count; i++) {
            if (i > 0) sb.Append(turnOrderSeparator);
            var u = turnOrder[i];
            string name = u.IsDead ? $"<s>{u.DisplayName}</s>" : u.DisplayName;
            if (u == active) sb.Append("<b><color=#").Append(activeColorHex).Append('>').Append(name).Append("</color></b>");
            else sb.Append(name);
        }
        turnOrderText.text = sb.ToString();
    }

    // ---- IBattleView ----
    public void Log(string message) => SetText(logText, message);

    public void SetGm(string line) {
        SetText(gmText, line);
        SetActive(gmPanel, !string.IsNullOrEmpty(line));
        if (gmHide != null) { StopCoroutine(gmHide); gmHide = null; }
        if (gmAutoHideSeconds > 0f && !string.IsNullOrEmpty(line)) gmHide = StartCoroutine(HideGmLater());
    }

    IEnumerator HideGmLater() {
        yield return new WaitForSeconds(gmAutoHideSeconds);
        SetActive(gmPanel, false);
        gmHide = null;
    }

    public IEnumerator Tumble(int diceCount) => diceTray.Tumble(diceCount);
    public void ShowRoll(RollResult roll) => diceTray.ShowRoll(roll);

    public IEnumerator TumbleEnemy(int diceCount) {
        if (enemyDiceTray == null) yield break;
        yield return enemyDiceTray.Tumble(diceCount);
    }
    public void ShowEnemyRoll(int[] dice, int keptIndex) { if (enemyDiceTray != null) enemyDiceTray.ShowValues(dice, keptIndex); }
    public void ShowReactionPanel(bool visible) => reactionPanel.SetActive(visible);
    public void ShowContinue(bool visible) => continueBtn.gameObject.SetActive(visible);
}
