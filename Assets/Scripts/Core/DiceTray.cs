using System.Collections;
using UnityEngine;

// A row of dice. The FIRST die is the unit's own die; every die after it is a Malice die (Destiny's extras),
// drawn with the Dice component's Malice look. Turn extrasAreMalice off to make all dice look the same.
public class DiceTray : MonoBehaviour {
    public Dice diePrefab;
    public int maxDice = 5;
    public bool extrasAreMalice = true;
    Dice[] dice;

    void Awake() {
        dice = new Dice[maxDice];
        for (int i = 0; i < maxDice; i++) {
            dice[i] = Instantiate(diePrefab, transform);
            dice[i].Hide();
        }
    }

    bool IsMalice(int i) => extrasAreMalice && i >= 1;

    // Random Stream
    public IEnumerator Tumble(int count, float seconds = 0.6f, float step = 0.06f) {
        for (float t = 0; t < seconds; t += step) {
            for (int i = 0; i < dice.Length; i++) {
                if (i < count) dice[i].Show(Random.Range(1, 21), false, IsMalice(i));
                else dice[i].Hide();
            }
            yield return new WaitForSeconds(step);
        }
    }

    public void ShowRoll(RollResult r) {
        for (int i = 0; i < dice.Length; i++) {
            if (i < r.Dice.Length) dice[i].Show(r.Dice[i], r.Dice[i] == r.Lowest, IsMalice(i));
            else dice[i].Hide();
        }
    }

    // Show fixed values (used for the enemy's clash dice). keptIndex gets the highlight colour.
    public void ShowValues(int[] values, int keptIndex) {
        for (int i = 0; i < dice.Length; i++) {
            if (i < values.Length) dice[i].Show(values[i], i == keptIndex, IsMalice(i));
            else dice[i].Hide();
        }
    }

    public void Clear() { if (dice == null) return; foreach (var d in dice) d.Hide(); }
}
