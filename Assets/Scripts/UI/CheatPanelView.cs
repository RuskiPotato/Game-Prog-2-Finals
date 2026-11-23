using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CheatPanelView : MonoBehaviour {
    [Header("Panel")]
    public GameObject panel;                
    public RectTransform gridRoot;          
    public CardView cardPrefab;              

    [Header("Buttons")]
    public Button openButton;              
    public Button closeButton;              
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
