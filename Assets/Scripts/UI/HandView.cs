using System;
using System.Collections.Generic;
using UnityEngine;

// Lays the hand out as a fan. Hovered card lifts, straightens and grows; its neighbours step aside.
public class HandView : MonoBehaviour {
    public RectTransform handRoot;         // empty RectTransform anchored bottom-centre, pivot (0.5, 0)
    public CardView cardPrefab;

    [Header("Fan")]
    public float spacing = 170f;
    public float fanStepDegrees = 6f;
    public float archDrop = 8f;
    public float restY = -110f;            // negative: resting cards peek up from the bottom edge
    public float hoverY = 40f;
    public float hoverScale = 1.25f;
    public float pushApart = 60f;
    public float smooth = 14f;

    public event Action<ICard> CardClicked;

    readonly List<ICard> order = new List<ICard>();
    readonly Dictionary<ICard, CardView> views = new Dictionary<ICard, CardView>();
    readonly List<CardView> visible = new List<CardView>();
    CardView hovered, lastHovered;

    public void Clear() {
        foreach (var view in views.Values) if (view != null) Destroy(view.gameObject);
        order.Clear(); views.Clear(); visible.Clear(); hovered = lastHovered = null;
    }

    public void Build(IEnumerable<ICard> cards) {
        Clear();
        foreach (var card in cards) {
            if (card == null) continue;
            var view = Instantiate(cardPrefab, handRoot);
            var captured = card;
            view.Clicked += () => CardClicked?.Invoke(captured);
            view.HoverChanged += isOver => {
                if (isOver) hovered = view;
                else if (hovered == view) hovered = null;
            };
            order.Add(card);
            views[card] = view;
        }
    }

    public void Refresh(BattleContext ctx) {
        visible.Clear();
        foreach (var card in order) {
            var view = views[card];
            bool shown = card.IsAvailable(ctx);
            view.gameObject.SetActive(shown);
            if (!shown) {
                if (hovered == view) hovered = null;
                continue;
            }
            view.Set(card.Title, card.CostText(ctx), card.DetailText(ctx), card.Art, card.Kind, card.CanPlay(ctx));
            visible.Add(view);
        }
    }

    void Update() {
        int n = visible.Count;
        if (n == 0) return;

        if (hovered != lastHovered) {                       // keep draw order stable, hovered card on top
            for (int i = 0; i < n; i++) visible[i].transform.SetSiblingIndex(i);
            if (hovered != null && hovered.gameObject.activeSelf) hovered.transform.SetAsLastSibling();
            lastHovered = hovered;
        }

        int hoveredIndex = hovered != null ? visible.IndexOf(hovered) : -1;
        float k = 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime);

        for (int i = 0; i < n; i++) {
            float offset = i - (n - 1) * 0.5f;
            bool isHover = i == hoveredIndex;

            float x = offset * spacing;
            float y = restY - archDrop * offset * offset;
            float rot = -offset * fanStepDegrees;
            float scale = 1f;

            if (hoveredIndex >= 0) {
                if (isHover) { y = hoverY; rot = 0f; scale = hoverScale; }
                else x += (i < hoveredIndex ? -1f : 1f) * pushApart;
            }
            visible[i].Animate(new Vector2(x, y), rot, scale, k);
        }
    }
}
