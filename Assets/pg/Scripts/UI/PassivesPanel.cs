using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

// Side panel listing a unit's passives. Slides in from its own side (set Hidden/Shown anchored X in the inspector)
// or simply toggles active if slideSpeed is 0. Put one on the left (hero) and one on the right (enemy).
public class PassivesPanel : MonoBehaviour {
    public TMP_Text titleText, bodyText;
    public RectTransform slide;             // optional: the panel body to move
    public float hiddenX = -320f, shownX = 0f;
    public float slideSpeed = 1800f;        // px/sec; 0 = no slide

    bool wantShown;

    public void Show(string title, IList<string> lines) {
        if (titleText != null) titleText.text = title;
        if (bodyText != null) {
            var sb = new StringBuilder();
            if (lines != null)
                for (int i = 0; i < lines.Count; i++) {
                    if (i > 0) sb.Append('\n');
                    sb.Append("• ").Append(lines[i]);
                }
            if (sb.Length == 0) sb.Append("No passives");
            bodyText.text = sb.ToString();
        }
        wantShown = true;
        if (slideSpeed <= 0f) gameObject.SetActive(true);
        else if (slide != null) gameObject.SetActive(true);
    }

    public void Hide() {
        wantShown = false;
        if (slideSpeed <= 0f || slide == null) gameObject.SetActive(false);
    }

    void Update() {
        if (slide == null || slideSpeed <= 0f) return;
        var p = slide.anchoredPosition;
        p.x = Mathf.MoveTowards(p.x, wantShown ? shownX : hiddenX, slideSpeed * Time.unscaledDeltaTime);
        slide.anchoredPosition = p;
    }
}
