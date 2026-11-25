
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public partial class BattleHud : MonoBehaviour, IBattleView
{
    [Header("Parts")]
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
    public Button acceptBtn;
    public Button rerollBtn;
    public Button continueBtn;

    public event Action AcceptClicked;
    public event Action RerollClicked;
    public event Action ContinueClicked;
    public event Action<ICard> CardClicked;

    readonly List<ICard> lastCombatCards = new List<ICard>();

    BattleContext lastCtx;
    Coroutine gmHide;

    static void SetText(TMP_Text t, string s)
    {
        if (t != null)
            t.text = s;
    }

    static void SetActive(GameObject g, bool on)
    {
        if (g != null && g.activeSelf != on)
            g.SetActive(on);
    }

    void Awake()
    {
        if (statusView == null)
            statusView = GetComponentInChildren<BattleStatusView>(true);

        if (statusView == null)
            statusView = FindFirstObjectByType<BattleStatusView>();

        if (clashPresenter == null)
            clashPresenter = GetComponentInChildren<ClashPresenter>(true);

        if (clashPresenter == null)
            clashPresenter = FindFirstObjectByType<ClashPresenter>();

        if (statusView == null || clashPresenter == null)
        {
            Debug.LogError(
                "BattleHud: add BattleStatusView and ClashPresenter next to it (run Paperboard God > Migrate BattleHud).",
                this
            );
        }

        if (acceptBtn != null)
            acceptBtn.onClick.AddListener(() => AcceptClicked?.Invoke());

        if (rerollBtn != null)
            rerollBtn.onClick.AddListener(() => RerollClicked?.Invoke());

        if (continueBtn != null)
            continueBtn.onClick.AddListener(() => ContinueClicked?.Invoke());

        if (hand != null)
            hand.CardClicked += card => CardClicked?.Invoke(card);

        if (cheatPanel != null)
            cheatPanel.CardClicked += card => CardClicked?.Invoke(card);

        SetActive(reactionPanel, false);

        if (continueBtn != null)
            continueBtn.gameObject.SetActive(false);

        SetActive(gmPanel, false);
    }

    // Works with whichever input system the project uses.
    static bool EscapePressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Keyboard.current != null &&
               Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    void Update()
    {
        if (!EscapePressed())
            return;

        if (cheatPanel != null && cheatPanel.IsOpen)
        {
            cheatPanel.Close();
        }
        else if (statusView != null && statusView.HasSelection)
        {
            statusView.ClearSelection();
        }
    }

    public void Initialize(
        IEnumerable<ICard> coreCards,
        IEnumerable<ICard> cheats = null)
    {
        if (hand != null)
            hand.Build(coreCards);

        if (cheatPanel != null)
        {
            // Keep the cheat panel separate from the combat hand.
            cheatPanel.gameObject.SetActive(false);
            cheatPanel.Build(cheats);
        }
    }

    // --------------------------------------------------
    // Hand
    // --------------------------------------------------

    // Hand = core Attack/Defend + combat-card deck + hero skills.
    public void RefreshCombatHand(
        CardDeck deck,
        IEnumerable<CardData> coreCards,
        IEnumerable<ICard> skills = null)
    {
        if (deck == null)
            return;

        var cards = deck.BuildVisibleCards(coreCards);

        if (skills != null)
            cards.AddRange(skills);

        bool changed = cards.Count != lastCombatCards.Count;

        if (!changed)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (!ReferenceEquals(cards[i], lastCombatCards[i]))
                {
                    changed = true;
                    break;
                }
            }
        }

        if (changed)
        {
            lastCombatCards.Clear();
            lastCombatCards.AddRange(cards);

            if (hand != null)
                hand.Build(cards);
        }

        if (lastCtx != null && hand != null)
            hand.Refresh(lastCtx);
    }

    public void SetTurnOrder(List<Unit> order)
    {
        if (statusView != null)
            statusView.SetTurnOrder(order);
    }

    public void Refresh(BattleContext ctx)
    {
        lastCtx = ctx;

        if (statusView != null)
            statusView.Refresh(ctx);

        if (ctx.State == BattleState.HeroTurn &&
            clashPresenter != null)
        {
            clashPresenter.ClearDice();
        }

        if (hand != null)
            hand.Refresh(ctx);

        if (cheatPanel != null)
            cheatPanel.Refresh(ctx);
    }

    // --------------------------------------------------
    // IBattleView
    // --------------------------------------------------

    public void Log(string message)
    {
        SetText(logText, message);
    }

    public void SetGm(string line)
    {
        SetText(gmText, line);

        SetActive(
            gmPanel,
            !string.IsNullOrEmpty(line)
        );

        if (gmHide != null)
        {
            StopCoroutine(gmHide);
            gmHide = null;
        }

        if (gmAutoHideSeconds > 0f &&
            !string.IsNullOrEmpty(line))
        {
            gmHide = StartCoroutine(HideGmLater());
        }
    }

    IEnumerator HideGmLater()
    {
        yield return new WaitForSeconds(gmAutoHideSeconds);

        SetActive(gmPanel, false);
        gmHide = null;
    }

    // --------------------------------------------------
    // Dice presentation
    // --------------------------------------------------

    public IEnumerator Tumble(int diceCount, int fortuneIndex = -1)
    {
        if (clashPresenter == null)
            yield break;

        yield return clashPresenter.Tumble(diceCount, fortuneIndex);
    }

    public void ShowRoll(RollResult roll)
    {
        if (clashPresenter != null)
            clashPresenter.ShowRoll(roll);
    }

    public IEnumerator TumbleEnemy(int diceCount)
    {
        if (clashPresenter == null)
            yield break;

        yield return clashPresenter.TumbleEnemy(diceCount);
    }

    public void ShowEnemyRoll(int[] dice, int keptIndex)
    {
        if (clashPresenter != null)
            clashPresenter.ShowEnemyRoll(dice, keptIndex);
    }

    public IEnumerator TumbleClash(
        int heroDiceCount,
        int enemyDiceCount,
        int heroFortuneIndex = -1)
    {
        if (clashPresenter == null)
            yield break;

        yield return clashPresenter.TumbleClash(
            heroDiceCount,
            enemyDiceCount,
            heroFortuneIndex
        );
    }

  
    // Clash presentation


    public IEnumerator ClashApproach()
    {
        if (clashPresenter == null)
            yield break;

        yield return clashPresenter.ClashApproach();
    }

    // Original method retained for existing callers.
    public IEnumerator ClashOutcome(
        bool heroWon,
        bool enemyWon)
    {
        if (clashPresenter == null)
            yield break;

        yield return clashPresenter.ClashOutcome(
            heroWon,
            enemyWon
        );
    }

    // New method supports a tied clash.
    public IEnumerator ClashOutcome(
        bool heroWon,
        bool enemyWon,
        bool tied)
    {
        if (clashPresenter == null)
            yield break;

        yield return clashPresenter.ClashOutcome(
            heroWon,
            enemyWon,
            tied
        );
    }

    // Pop-ups

    public void ShowReactionPanel(bool visible)
    {
        SetActive(reactionPanel, visible);
    }

    public void ShowContinue(bool visible)
    {
        if (continueBtn != null)
            continueBtn.gameObject.SetActive(visible);
    }
}
