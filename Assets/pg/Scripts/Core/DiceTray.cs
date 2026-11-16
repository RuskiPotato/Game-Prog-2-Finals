using System.Collections;
using UnityEngine;

public class DiceTray : MonoBehaviour {
    public Dice diePrefab;
    public int maxDice = 5;
    Dice[] dice;

    void Awake() {
        dice = new Dice[maxDice];
        for (int i = 0; i < maxDice; i++) {
            dice[i] = Instantiate(diePrefab, transform);
            dice[i].Hide();
        }
    }

    // Random Stream
    public IEnumerator Tumble(int count, float seconds = 0.6f, float step = 0.06f) {
        for (float t = 0; t < seconds; t += step) {
            for (int i = 0; i < dice.Length; i++) {
                if (i < count) dice[i].Show(Random.Range(1, 21), false);
                else dice[i].Hide();
            }
            yield return new WaitForSeconds(step);
        }
    }

    public void ShowRoll(RollResult r) {
        for (int i = 0; i < dice.Length; i++) {
            if (i < r.Dice.Length) dice[i].Show(r.Dice[i], r.Dice[i] == r.Lowest);
            else dice[i].Hide();
        }
    }

    public void Clear() { foreach (var d in dice) d.Hide(); }
}