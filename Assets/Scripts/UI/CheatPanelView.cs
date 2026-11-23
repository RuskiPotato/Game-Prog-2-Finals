using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The cheat menu. Owns the panel, its Open / Close buttons and one CardView per cheat.
// Cards are positioned by the Grid Layout Group on GridRoot: this script never moves them,
// it only scales a card slightly on hover, so the layout group stays in charge.
//
// Put this script on an object that is ALWAYS ACTIVE (for example GamePanels or an empty CheatMenu),
// not on CheatPanel itself, because CheatPanel is hidden at the start.
public class CheatPanelView : MonoBehaviour {
    [Header("Panel")]
    public GameObject panel;                 // CheatPanel (hidden at the start)
    public RectTransform gridRoot;           // CheatContainer: the object with the Grid Layout Group
    public CardView cardPrefab;              // a CheatCard prefab with a CardView on it

    [Header("Buttons")]
    public Button openButton;                // the "Cheats" button on the bottom bar
    public Button closeButton;               // the X in the header
    public bool hideOpenButtonUntilUnlocked = true;

    [Header("Hover")]
    public float hoverScale = 1.06f;

    public event Action<ICard> CardClicked;
    public bool IsOpen { get; private set; }

    readonly List<ICard> order = new List<ICard>();
    readonly Dictionary<ICard, CardView> views = new Dictionary<ICard, CardView>();

    void Awake() {
        if (openButton != null) openButton.onClick.AddListener(Toggle);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        SetPanel(false);
    }

    // One card per cheat in the registry (called once by BattleHud.Initialize)
    public void Build(IEnumerable<ICard> cheats) {
        Clear();
        if (cheats == null) return;
        if (cardPrefab == null || gridRoot == null) {
            Debug.LogError("CheatPanelView: assign Card Prefab and Grid Root in the Inspector.", this);
            return;
        }

        foreach (var card in cheats) {
            if (card == null) continue;
            var view = Instantiate(cardPrefab, gridRoot);
            var captured = card;
            view.Clicked += () => CardClicked?.Invoke(captured);
            view.HoverChanged += over => view.transform.localScale = over ? Vector3.one * hoverScale : Vector3.one;
            order.Add(card);
            views[card] = view;
        }
    }

    void Clear() {
        foreach (var view in views.Values) if (view != null) Destroy(view.gameObject);
        order.Clear();
        views.Clear();
    }

    // Shows only unlocked cheats, dims the ones you can't play right now,
    // and closes the panel as soon as it is no longer the player's turn to act.
    public void Refresh(BattleContext ctx) {
        bool anyUnlocked = false;
        foreach (var card in order) {
            var view = views[card];
            bool unlocked = card.IsAvailable(ctx);
            anyUnlocked |= unlocked;
            view.gameObject.SetActive(unlocked);
            if (unlocked) view.Set(card.Title, card.CostText(ctx), card.DetailText(ctx), card.Art, card.Kind, card.CanPlay(ctx));
            else view.transform.localScale = Vector3.one;
        }

        if (openButton != null) {
            if (hideOpenButtonUntilUnlocked) openButton.gameObject.SetActive(anyUnlocked);
            openButton.interactable = ctx.CanAct;
        }
        if (!ctx.CanAct && IsOpen) Close();
    }

    public void Open() { if (!IsOpen) SetPanel(true); }
    public void Close() { if (IsOpen) SetPanel(false); }
    public void Toggle() { if (IsOpen) Close(); else Open(); }

    void SetPanel(bool on) {
        IsOpen = on;
        if (panel != null) panel.SetActive(on);
    }
}
