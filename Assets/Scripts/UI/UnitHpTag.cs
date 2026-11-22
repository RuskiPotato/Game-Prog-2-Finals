using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The name + HP bar that sits UNDER a unit's image (always visible, unlike the profile panel).
// One per unit: later, a party just gets one tag per hero slot.
// Hierarchy: put it on an object under the unit image with a TMP text and a Slider (Min 0, Max 1, Interactable off).
public class UnitHpTag : MonoBehaviour {
    public TMP_Text label;           // "Rogue  24/30"
    public Slider bar;               // optional
    public bool showName = true;

    // showHp = false hides the numbers and bar (the enemy in the scripted fight)
    public void Set(string displayName, int hp, int maxHp, bool showHp) {
        if (label != null) {
            string name = showName ? displayName : "";
            label.text = showHp ? (showName ? $"{name}  {hp}/{maxHp}" : $"{hp}/{maxHp}") : name;
        }
        if (bar != null) {
            if (bar.gameObject.activeSelf != showHp) bar.gameObject.SetActive(showHp);
            bar.value = maxHp > 0 ? Mathf.Clamp01((float)hp / maxHp) : 0f;
        }
    }
}
