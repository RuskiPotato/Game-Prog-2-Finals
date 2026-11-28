using UnityEngine;
using UnityEngine.UI;

// Shows a unit's battlefield sprite. Lives on the same object as the unit's Image (UI) or SpriteRenderer.
// BattleStatusView adds this automatically next to the hero/enemy UnitSelectable and calls Set() every refresh:
// heroes get ClassData.BattleSprite, enemies get EnemyData.sprite.
// Set(null) restores whatever sprite the scene object had before, so a missing assignment never blanks the unit.
[DisallowMultipleComponent]
public class UnitSpriteView : MonoBehaviour
{
    public Image image;
    public SpriteRenderer spriteRenderer;
    public bool preserveAspect = true;
    [Tooltip("Resize the Image to the sprite's pixel size (UI Image only).")]
    public bool useNativeSize = false;

    Sprite fallback;
    Sprite current;
    bool captured;

    void Awake() => Capture();

    void Capture()
    {
        if (captured)
            return;

        if (image == null && spriteRenderer == null)
        {
            image = GetComponent<Image>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (image != null)
            fallback = image.sprite;
        else if (spriteRenderer != null)
            fallback = spriteRenderer.sprite;

        captured = true;
    }

    public void Set(Sprite sprite)
    {
        Capture();

        Sprite target = sprite != null ? sprite : fallback;
        if (target == current)
            return;

        current = target;

        if (image != null)
        {
            image.sprite = target;
            image.preserveAspect = preserveAspect;
            if (useNativeSize && target != null)
                image.SetNativeSize();
        }
        else if (spriteRenderer != null)
        {
            spriteRenderer.sprite = target;
        }
    }
}
