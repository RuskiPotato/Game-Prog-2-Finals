using UnityEngine;
using UnityEngine.UI;

public class DiamondMeter : MonoBehaviour {
    public Image back;
    public Image fill;
    public Gradient colorByValue;          
    public float easeSpeed = 6f;           
    public RectTransform[] tierNotches;   

    float target, shown;
    static Sprite generated;

    void Awake() {
        if (fill == null) return;
        if (fill.sprite == null) fill.sprite = DiamondSprite();
        if (back != null && back.sprite == null) back.sprite = DiamondSprite();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Vertical;
        fill.fillOrigin = (int)Image.OriginVertical.Bottom;
        fill.fillAmount = shown;
    }

    public void SetValue(float value01, bool instant = false) {
        target = Mathf.Clamp01(value01);
        if (instant) { shown = target; Apply(); }
    }

    void Update() {
        if (Mathf.Approximately(shown, target)) return;
        shown = Mathf.MoveTowards(shown, target, easeSpeed * Time.unscaledDeltaTime * Mathf.Max(0.1f, Mathf.Abs(target - shown)) + 0.002f);
        Apply();
    }

    void Apply() {
        if (fill == null) return;
        fill.fillAmount = shown;
        if (colorByValue != null && colorByValue.colorKeys.Length > 0) fill.color = colorByValue.Evaluate(shown);
    }

  
    static Sprite DiamondSprite() {
        if (generated != null) return generated;
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        float c = (n - 1) / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++) {
                float d = (Mathf.Abs(x - c) + Mathf.Abs(y - c)) / c;       // 1 at the edge of the diamond
                byte a = (byte)Mathf.Clamp(Mathf.RoundToInt((1f - d) * c * 255f / 1.5f), 0, 255);
                px[y * n + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(px);
        tex.Apply();
        generated = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return generated;
    }
}
