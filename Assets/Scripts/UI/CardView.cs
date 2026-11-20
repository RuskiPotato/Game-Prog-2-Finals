using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One card on screen. Pure view: HandView tells it where to be, ICard tells it what to say.
public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
    [Header("Slots")]
    public RectTransform rect;
    public TMP_Text titleText, costText, detailText;
    public Image artImage;                 // optional
    public Image frame;
    public CanvasGroup canvasGroup;

    [Header("Colours")]
    public Color actionColor = new Color(0.88f, 0.88f, 0.88f);
    public Color cheatColor = new Color(0.55f, 0.90f, 0.85f);

    public event Action Clicked;
    public event Action<bool> HoverChanged;

    Vector2 pos;
    float rot;
    float scale = 1f;
    bool posed, playable;

    public void Set(string title, string cost, string detail, Sprite art, CardKind kind, bool canPlay) {
        titleText.text = title;
        costText.text = cost;
        detailText.text = detail;
        if (artImage != null) { artImage.sprite = art; artImage.enabled = art != null; }
        frame.color = kind == CardKind.Cheat ? cheatColor : actionColor;
        playable = canPlay;
        canvasGroup.alpha = canPlay ? 1f : 0.55f;
    }

    // Smoothly moves toward a pose (k is the lerp factor for this frame)
    public void Animate(Vector2 targetPos, float targetRot, float targetScale, float k) {
        if (!posed) { pos = targetPos; rot = targetRot; scale = targetScale; posed = true; }
        else {
            pos = Vector2.Lerp(pos, targetPos, k);
            rot = Mathf.LerpAngle(rot, targetRot, k);
            scale = Mathf.Lerp(scale, targetScale, k);
        }
        rect.anchoredPosition = pos;
        rect.localRotation = Quaternion.Euler(0f, 0f, rot);
        rect.localScale = new Vector3(scale, scale, 1f);
    }

    void OnDisable() { posed = false; }         // snap into place the next time it appears

    public void OnPointerEnter(PointerEventData e) => HoverChanged?.Invoke(true);
    public void OnPointerExit(PointerEventData e) => HoverChanged?.Invoke(false);
    public void OnPointerClick(PointerEventData e) { if (playable) Clicked?.Invoke(); }
}
