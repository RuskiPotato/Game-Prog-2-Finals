using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

// The HUD's front door. It implements IBattleView for the battle logic and owns the things that are about input:
// the hand, the cheat panel, the reaction pop-up, the log and the GM taunt.
// Numbers/profiles live in BattleStatusView; dice + clash animation live in ClashPresenter. Put all three on the same object.
public partial class BattleHud : MonoBehaviour, IBattleView {
    [Header("Parts (auto-found on this object if left empty)")]
    public BattleStatusView statusView;
    public ClashPresenter clashPresenter;

    [Header("Game Master taunt")]
    public GameObject gmPanel;
    public TMP_Text gmText;
    public float gmAutoHideSeconds = 0f;

    [Header("Log")]
    public TMP_Text logText;

    [Header("Hand")]
    public HandView hand;

    [Header("Cheat Panel")]
    public CheatPanelView cheatPanel;

    [Header("Pop-ups")]
    public GameObject reactionPanel;
    public Button acceptBtn, rerollBtn, continueBtn;

    public event Action AcceptClicked, RerollClicked, ContinueClicked;
    public event Action<ICard> CardClicked;

    readonly List<ICard> lastCombatCards = new List<ICard>();
    BattleContext lastCtx;
    Coroutine gmHide;

    static void SetText(TMP_Text t, string s) { if (t != null) t.text = s; }
    static void SetActive(GameObject g, bool on) { if (g != null && g.activeSelf != on) g.SetActive(on); }

    void Awake() {
        if (statusView == null) statusView = GetComponentInChildren<BattleStatusView>(true);
        if (statusView == null) statusView = FindFirstObjectByType<BattleStatusView>();
        if (clashPresenter == null) clashPresenter = GetComponentInChildren<ClashPresenter>(true);
        if (clashPresenter == null) clashPresenter = FindFirstObjectByType<ClashPresenter>();
        if (statusView == null || clashPresenter == null)
            Debug.LogError("BattleHud: add BattleStatusView and ClashPresenter next to it (run Paperboard God > Migrate BattleHud).", this);

        if (acceptBtn != null) acceptBtn.onClick.AddListener(() => AcceptClicked?.Invoke());
        if (rerollBtn != null) rerollBtn.onClick.AddListener(() => RerollClicked?.Invoke());
        if (continueBtn != null) continueBtn.onClick.AddListener(() => ContinueClicked?.Invoke());
        if (hand != null) hand.CardClicked += card => CardClicked?.Invoke(card);
        if (cheatPanel != null) cheatPanel.CardClicked += card => CardClicked?.Invoke(card);
        SetActive(reactionPanel, false);
        if (continueBtn != null) continueBtn.gameObject.SetActive(false);
        SetActive(gmPanel, false);
    }

    // Works whichever input system the project uses (Player Settings > Active Input Handling).
    static bool EscapePressed() {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    void Update() {
        if (EscapePressed()) {
            if (cheatPanel != null && cheatPanel.IsOpen) cheatPanel.Close();
            else if (statusView != null && statusView.HasSelection) statusView.ClearSelection();
        }
    }

    public void Initialize(IEnumerable<ICard> coreCards, IEnumerable<ICard> cheats = null) {
        if (hand != null) hand.Build(coreCards);
        if (cheatPanel != null) {
            cheatPanel.gameObject.SetActive(false);   // keep whatever opens it outside this object
            cheatPanel.Build(cheats);
        }
    }

    // ---- Hand ----
    // Hand = core Attack/Defend + the combat-card deck's hand + the hero's skills.
    public void RefreshCombatHand(CardDeck deck, IEnumerable<CardData> coreCards, IEnumerable<ICard> skills = null) {
        if (deck == null) return;
        var cards = deck.BuildVisibleCards(coreCards);
        if (skills != null) cards.AddRange(skills);
        bool changed = cards.Count != lastCombatCards.Count;
        if (!changed) for (int i = 0; i < cards.Count; i++) if (!ReferenceEquals(cards[i], lastCombatCards[i])) { changed = true; break; }
        if (changed) {
            lastCombatCards.Clear(); lastCombatCards.AddRange(cards);
            if (hand != null) hand.Build(cards);
        }
        if (lastCtx != null && hand != null) hand.Refresh(lastCtx);
    }

    public void SetTurnOrder(List<Unit> order) { if (statusView != null) statusView.SetTurnOrder(order); }

    public void Refresh(BattleContext ctx) {
        lastCtx = ctx;
        if (statusView != null) statusView.Refresh(ctx);
        if (ctx.State == BattleState.HeroTurn && clashPresenter != null) clashPresenter.ClearDice();
        if (hand != null) hand.Refresh(ctx);
        if (cheatPanel != null) cheatPanel.Refresh(ctx);
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

    public IEnumerator Tumble(int diceCount) => clashPresenter.Tumble(diceCount);
    public void ShowRoll(RollResult roll) => clashPresenter.ShowRoll(roll);   // null-safe inside ClashPresenter
    public IEnumerator TumbleEnemy(int diceCount) => clashPresenter.TumbleEnemy(diceCount);
    public void ShowEnemyRoll(int[] dice, int keptIndex) => clashPresenter.ShowEnemyRoll(dice, keptIndex);
    public IEnumerator TumbleClash(int heroDiceCount, int enemyDiceCount) => clashPresenter.TumbleClash(heroDiceCount, enemyDiceCount);
    public IEnumerator ClashApproach() => clashPresenter.ClashApproach();
    public IEnumerator ClashOutcome(bool heroWon, bool enemyWon) => clashPresenter.ClashOutcome(heroWon, enemyWon);

    public void ShowReactionPanel(bool visible) => SetActive(reactionPanel, visible);
    public void ShowContinue(bool visible) { if (continueBtn != null) continueBtn.gameObject.SetActive(visible); }
}
