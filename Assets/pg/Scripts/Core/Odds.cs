using UnityEngine;

public static class Odds {
    // P(min of n d20 + mod >= dc) = ((21 - k) / 20)^n, where k = dc - mod
    public static float HitChance(int dc, int mod, int n) {
        int k = dc - mod;
        if (k <= 1) return 1f;
        if (k > 20) return 0f;
        return Mathf.Pow((21 - k) / 20f, n);
    }
}
