using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Dice : MonoBehaviour {
    public Image background;
    public TMP_Text label;
    public Color normal = Color.white;
    public Color lowest = new Color(0.7f, 0.3f, 1f);

    public void Show(int value, bool isLowest) {
        gameObject.SetActive(true);
        label.text = value.ToString();
        background.color = isLowest ? lowest : normal;
    }

    public void Hide() => gameObject.SetActive(false);
}