using System.Collections;
using UnityEngine;

// A row of dice. The first die is the hero's base die, later dice are Malice dice,
// and the optional Fortune Die is displayed separately with its own style.
public class DiceTray : MonoBehaviour {
    public Dice diePrefab;
    public int maxDice = 6;
    public bool extrasAreMalice = true;
    Dice[] dice;
    Vector3[] baseScales;

    void Awake() {
        // Fate can produce five Malice dice plus the hero's Fortune Die.
        maxDice = Mathf.Max(maxDice, 6);
        dice = new Dice[maxDice];
        baseScales = new Vector3[maxDice];
        for (int i = 0; i < maxDice; i++) {
            dice[i] = Instantiate(diePrefab, transform);
            baseScales[i] = dice[i].transform.localScale;
            dice[i].Hide();
        }
    }

    bool IsMalice(int i, int fortuneIndex = -1) =>
        extrasAreMalice && i >= 1 && i != fortuneIndex;

    // Random Stream
    public IEnumerator Tumble(
        int count,
        float seconds = 0.6f,
        float step = 0.06f,
        int fortuneIndex = -1) {
        for (float t = 0; t < seconds; t += step) {
            for (int i = 0; i < dice.Length; i++) {
                if (i < count) {
                    bool isFortune = i == fortuneIndex;
                    dice[i].Show(
                        Random.Range(1, 21),
                        false,
                        IsMalice(i, fortuneIndex),
                        isFortune
                    );
                    float pulse = 1f + Mathf.Sin((t / seconds) * Mathf.PI * 8f + i * 0.7f) * 0.10f;
                    dice[i].transform.localScale = baseScales[i] * pulse;
                } else dice[i].Hide();
            }
            yield return new WaitForSeconds(step);
        }
    }

    public void ShowRoll(RollResult r) {
        for (int i = 0; i < dice.Length; i++) {
            if (i < r.Dice.Length) {
                bool kept = i == r.KeptIndex;
                bool isFortune = i == r.FortuneIndex;
                dice[i].Show(r.Dice[i], kept, IsMalice(i, r.FortuneIndex), isFortune);
                dice[i].transform.localScale = baseScales[i] * (kept ? 1.18f : 0.82f);
            } else dice[i].Hide();
        }
    }

    // Show fixed values (used for the enemy's clash dice). keptIndex gets the highlight colour.
    public void ShowValues(int[] values, int keptIndex) {
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
