using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One die on screen. Regular dice and Malice dice can look different: give the prefab (or a second prefab)
// a Malice sprite and tint below. Anything left empty keeps the regular look.
public class Dice : MonoBehaviour {
    public Image background;
    public TMP_Text label;
    public Color normal = Color.white;
    public Color lowest = new Color(0.7f, 0.3f, 1f);        // highlight for the kept die

    [Header("Malice dice (the extra dice Destiny adds) - all optional")]
    public Sprite normalSprite;                              // empty = keep whatever sprite the prefab already has
    public Sprite maliceSprite;                              // the different image for Malice dice
    public Color maliceNormal = Color.white;
    public Color maliceLowest = new Color(0.7f, 0.3f, 1f);

    [Header("Fortune Die (optional distinct look)")]
    public Sprite fortuneSprite;
    public Color fortuneNormal = new Color(0.35f, 0.85f, 0.55f);
    public Color fortuneKept = new Color(0.15f, 1f, 0.4f);

    Sprite prefabSprite;
    bool captured;

    public void Show(int value, bool isLowest) => Show(value, isLowest, false);

    public void Show(int value, bool isKept, bool isMalice) =>
        Show(value, isKept, isMalice, false);

    public void Show(int value, bool isKept, bool isMalice, bool isFortune) {
        gameObject.SetActive(true);
        if (!captured) { prefabSprite = background.sprite; captured = true; }

        label.text = value.ToString();
        if (isFortune) {
            background.sprite = fortuneSprite != null
                ? fortuneSprite
                : normalSprite != null ? normalSprite : prefabSprite;
            background.color = isKept ? fortuneKept : fortuneNormal;
        }
        else if (isMalice) {
            if (maliceSprite != null) background.sprite = maliceSprite;
            else background.sprite = normalSprite != null ? normalSprite : prefabSprite;
            background.color = isKept ? maliceLowest : maliceNormal;
        } else {
            background.sprite = normalSprite != null ? normalSprite : prefabSprite;
            background.color = isKept ? lowest : normal;
        }
    }

    public void Hide() => gameObject.SetActive(false);
}
