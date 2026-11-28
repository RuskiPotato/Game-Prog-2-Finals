using System.Collections;
using UnityEngine;

// A row of dice. The FIRST die is the unit's own die; every die after it is a Malice die (Destiny's extras),
// drawn with the Dice component's Malice look. Turn extrasAreMalice off to make all dice look the same.
public class DiceTray : MonoBehaviour {
    public Dice diePrefab;
    public int maxDice = 5;
    public bool extrasAreMalice = true;
    Dice[] dice;
    Vector3[] baseScales;

    void Awake() {
        EnsureCapacity(Mathf.Max(1, maxDice));
    }

    // Focused Roll can temporarily add dice beyond the tray's original
    // Inspector capacity, so grow the tray instead of hiding the kept die.
    void EnsureCapacity(int required) {
        required = Mathf.Max(1, required);

        int current = dice == null ? 0 : dice.Length;
        if (required <= current)
            return;

        System.Array.Resize(ref dice, required);
        System.Array.Resize(ref baseScales, required);

        for (int i = current; i < required; i++) {
            dice[i] = Instantiate(diePrefab, transform);
            baseScales[i] = dice[i].transform.localScale;
            dice[i].Hide();
        }
    }

    bool IsMalice(int i) => extrasAreMalice && i >= 1;

    // Random Stream
    public IEnumerator Tumble(int count, float seconds = 0.6f, float step = 0.06f) {
        EnsureCapacity(count);

        for (float t = 0; t < seconds; t += step) {
            for (int i = 0; i < dice.Length; i++) {
                if (i < count) {
                    dice[i].Show(Random.Range(1, 21), false, IsMalice(i));
                    float pulse = 1f + Mathf.Sin((t / seconds) * Mathf.PI * 8f + i * 0.7f) * 0.10f;
                    dice[i].transform.localScale = baseScales[i] * pulse;
                } else dice[i].Hide();
            }
            yield return new WaitForSeconds(step);
        }
    }

    public void ShowRoll(RollResult r) {
        EnsureCapacity(r.Dice == null ? 0 : r.Dice.Length);

        for (int i = 0; i < dice.Length; i++) {
            if (i < r.Dice.Length) {
                bool kept = i == r.LowestIndex;
                dice[i].Show(r.Dice[i], kept, IsMalice(i));
                dice[i].transform.localScale = baseScales[i] * (kept ? 1.18f : 0.82f);
            } else dice[i].Hide();
        }
    }

    // Show fixed values (used for the enemy's clash dice). keptIndex gets the highlight colour.
    public void ShowValues(int[] values, int keptIndex) {
        EnsureCapacity(values == null ? 0 : values.Length);

        for (int i = 0; i < dice.Length; i++) {
            if (i < values.Length) {
                bool kept = i == keptIndex;
                dice[i].Show(values[i], kept, IsMalice(i));
                dice[i].transform.localScale = baseScales[i] * (kept ? 1.18f : 0.82f);
            } else dice[i].Hide();
        }
    }

    public void Clear() {
        if (dice == null) return;
        for (int i = 0; i < dice.Length; i++) {
            dice[i].Hide();
            dice[i].transform.localScale = baseScales[i];
        }
    }
}
