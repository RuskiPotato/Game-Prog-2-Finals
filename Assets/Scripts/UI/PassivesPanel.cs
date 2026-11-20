using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class PassivesPanel : MonoBehaviour {
    public TMP_Text titleText, bodyText;
    public UnityEvent onShown, onHidden;

    bool shown;

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

        // BattleHud calls Show on every refresh while a unit is selected
        if (!shown) { shown = true; onShown?.Invoke(); }
    }

    public void Hide() {
        if (shown) { shown = false; onHidden?.Invoke(); }
    }
}