using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleHud : MonoBehaviour, IBattleView {
    [Header("Top centre: Fate (hidden until the reveal)")]
    public GameObject fatePanel;               // holds the diamond + texts
    public DiamondMeter fateMeter;             // fills 0-100
    public TMP_Text fateValueText;             // "70"
    public TMP_Text tierText;                  // "Furious"
    public TMP_Text maliceText;                // "Malice dice: 3 (rolling 4)"

    [Header("Suspicion")]
    public GameObject suspicionPanel;          // optional
    public Slider suspicionBar;                // Min 0, Max 1
    public TMP_Text suspicionTierText;         // "Watching"
    public TMP_Text suspicionDetailText;       // "30/100  -  check DC 11"
    public Image suspicionFill;                //tinted by tier
    public Color unnoticedColor = new Color(0.55f, 0.75f, 0.55f), watchingColor = new Color(0.95f, 0.8f, 0.3f),
                 suspiciousColor = new Color(0.95f, 0.5f, 0.2f), caughtColor = new Color(0.85f, 0.2f, 0.2f);

    [Header("Game Master taunt")]
    public GameObject gmPanel;                 
    public TMP_Text gmText;
    public float gmAutoHideSeconds = 0f;      

    [Header("Field")]
    public TMP_Text heroText, enemyText, logText;
    public TMP_Text oddsText;                  
    public DiceTray diceTray;                  // keep it above the Hero/Enemy

    [Header("Passives (side panels, shown only while a unit is selected)")]
    public UnitSelectable heroSelectable, enemySelectable;
    public PassivesPanel heroPassives, enemyPassives;

    [Header("Hand")]
    public HandView hand;            
    public TMP_Text goldText;                  

    [Header("Pop-ups")]
    public GameObject reactionPanel;
    public Button acceptBtn, rerollBtn, continueBtn;

    public event Action AcceptClicked, RerollClicked, ContinueClicked;
    public event Action<ICard> CardClicked;

    enum Selected { None, Hero, Enemy }
    Selected selected = Selected.None;
    BattleContext lastCtx;
    Coroutine gmHide;

    static void SetText(TMP_Text t, string s) { if (t != null) t.text = s; }

    void Awake() {
        acceptBtn.onClick.AddListener(() => AcceptClicked?.Invoke());
        rerollBtn.onClick.AddListener(() => RerollClicked?.Invoke());
        continueBtn.onClick.AddListener(() => ContinueClicked?.Invoke());
        hand.CardClicked += card => CardClicked?.Invoke(card);
        reactionPanel.SetActive(false);
        continueBtn.gameObject.SetActive(false);

        if (heroSelectable != null) heroSelectable.Clicked += _ => Select(Selected.Hero);
        if (enemySelectable != null) enemySelectable.Clicked += _ => Select(Selected.Enemy);
        if (gmPanel != null) gmPanel.SetActive(false);
        if (heroPassives != null) heroPassives.Hide();
        if (enemyPassives != null) enemyPassives.Hide();
    }

    void Update() {
        if (selected != Selected.None && Input.GetKeyDown(KeyCode.Escape)) Select(Selected.None);
    }

    public void Initialize(IEnumerable<ICard> cards) => hand.Build(cards);

    // Clicking the selected unit again, or pressing Esc, clears the selection.
    void Select(Selected which) {
        selected = selected == which ? Selected.None : which;
        if (lastCtx != null) RefreshPassives(lastCtx);
    }

    public void Refresh(BattleContext ctx) {
        lastCtx = ctx;
        var fate = ctx.Fate;

        // Fate: diamond meter
        bool revealed = ctx.Progress.FateBarRevealed;
        if (fatePanel != null) fatePanel.SetActive(revealed);
        if (fateMeter != null) fateMeter.SetValue(fate.Fate / 100f);
        SetText(fateValueText, fate.Fate.ToString());
        SetText(tierText, fate.TierName);
        SetText(maliceText, $"Malice dice: {fate.MaliceDice}  (rolling {ctx.CurrentDice})");

        // Suspicion
        if (suspicionPanel != null) suspicionPanel.SetActive(revealed);
        if (suspicionBar != null) suspicionBar.value = fate.Suspicion / 100f;
        var tier = SuspicionInfo.TierOf(fate.Suspicion);
        SetText(suspicionTierText, tier.ToString());
        SetText(suspicionDetailText, $"{fate.Suspicion}/100  -  check DC {SuspicionInfo.CheckDc(fate.Suspicion)}");
        if (suspicionFill != null) suspicionFill.color = TierColor(tier);

        if (oddsText != null) {
            oddsText.gameObject.SetActive(!ctx.Scripted);            // odds hidden until the reveal
            float p = Odds.HitChance(ctx.Enemy.DefenseDc, ctx.RollMod, ctx.CurrentDice) * 100f;
            oddsText.text = $"Hit chance: {p:0.0}%  (need {ctx.Enemy.DefenseDc - ctx.RollMod}+ on die)";
        }

        SetText(goldText, $"Gold: {fate.Gold}");
        SetText(heroText, $"{ctx.Hero.DisplayName}  HP {ctx.Hero.Hp}/{ctx.Hero.MaxHp}");
        SetText(enemyText, ctx.Scripted ? ctx.Enemy.DisplayName
                                        : $"{ctx.Enemy.DisplayName}  HP {ctx.Enemy.Hp}/{ctx.Enemy.MaxHp}");

        RefreshPassives(ctx);
        hand.Refresh(ctx);
    }

    Color TierColor(SuspicionTier t) {
        switch (t) {
            case SuspicionTier.Watching: return watchingColor;
            case SuspicionTier.Suspicious: return suspiciousColor;
            case SuspicionTier.Caught: return caughtColor;
            default: return unnoticedColor;
        }
    }

    void RefreshPassives(BattleContext ctx) {
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

    // IBattleView
    public void Log(string message) => SetText(logText, message);

    public void SetGm(string line) {
        SetText(gmText, line);
        if (gmPanel != null) gmPanel.SetActive(!string.IsNullOrEmpty(line));
        if (gmHide != null) { StopCoroutine(gmHide); gmHide = null; }
        if (gmAutoHideSeconds > 0f && !string.IsNullOrEmpty(line)) gmHide = StartCoroutine(HideGmLater());
    }

    IEnumerator HideGmLater() {
        yield return new WaitForSeconds(gmAutoHideSeconds);
        if (gmPanel != null) gmPanel.SetActive(false);
        gmHide = null;
    }

    public IEnumerator Tumble(int diceCount) => diceTray.Tumble(diceCount);
    public void ShowRoll(RollResult roll) => diceTray.ShowRoll(roll);
    public void ShowReactionPanel(bool visible) => reactionPanel.SetActive(visible);
    public void ShowContinue(bool visible) => continueBtn.gameObject.SetActive(visible);
}
