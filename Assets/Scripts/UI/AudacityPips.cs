using UnityEngine;
using UnityEngine.UI;

// Row of circles showing Audacity. Filled = available, dim = empty.
// Put it on an object with a HorizontalLayoutGroup; assign one circle Image prefab.
// Pips are created on first use, so it is safe even if its panel starts inactive (a hidden profile panel).
public class AudacityPips : MonoBehaviour {
    public Image pipPrefab;                  // one circle Image prefab
    public Color filledColor = new Color(0.95f, 0.78f, 0.25f);
    public Color emptyColor = new Color(0.95f, 0.78f, 0.25f, 0.18f);
    public int maxAudacity = 5;

    Image[] pips;
    int lastShown = -1;

    void Awake() { Build(); }

    void Build() {
        if (pips != null) return;
        pips = new Image[maxAudacity];
        for (int i = 0; i < maxAudacity; i++) pips[i] = Instantiate(pipPrefab, transform);
    }

    public void Set(int current) {
        Build();
        current = Mathf.Clamp(current, 0, maxAudacity);
        if (current == lastShown) return;
        lastShown = current;
        for (int i = 0; i < pips.Length; i++)
            pips[i].color = i < current ? filledColor : emptyColor;
    }
}
