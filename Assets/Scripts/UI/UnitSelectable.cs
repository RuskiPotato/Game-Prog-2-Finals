using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Put on the Hero and Enemy portrait/sprite objects (needs a Graphic with Raycast Target on, e.g. an Image).
// Clicking raises Clicked; BattleHud decides what to show. 'highlight' is an optional outline object.
public class UnitSelectable : MonoBehaviour, IPointerClickHandler {
    public GameObject highlight;
    public event Action<UnitSelectable> Clicked;

    public void OnPointerClick(PointerEventData e) => Clicked?.Invoke(this);
    public void SetSelected(bool on) { if (highlight != null) highlight.SetActive(on); }
}
