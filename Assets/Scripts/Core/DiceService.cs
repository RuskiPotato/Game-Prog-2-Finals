using System;

public readonly struct RollResult {
    public readonly int[] Dice;
    public readonly int Lowest;
    public readonly bool Fumble;
    public readonly int LowestIndex;
    public RollResult(int[] dice, int lowest, bool fumble, int lowestIndex = 0) {
        Dice = dice; Lowest = lowest; Fumble = fumble; LowestIndex = lowestIndex;
    }
}

// The ONLY place combat dice are generated, always from the Combat stream.
public static class DiceService {
    static Random R => Rng.Get(RngStream.Combat);

    public static void Seed(int s) => Rng.Seed(s);   // reseeds every stream
    public static int D20() => R.Next(1, 21);
    public static int Die(int sides) => R.Next(1, sides + 1);

    // Enemies roll with Destiny's favor: n dice, keep the HIGHEST.
    public static int RollHighest(int n, int sides = 20) {
        int best = 0;
        for (int i = 0; i < n; i++) { int v = R.Next(1, sides + 1); if (v > best) best = v; }
        return best;
    }

    // Enemy roll with the dice exposed (for the real-roll clash): n dice, keep the HIGHEST. Combat stream.
    public static int RollHighestDice(int n, out int[] dice, out int highestIndex, int sides = 20) {
        dice = new int[n];
        int best = 0; highestIndex = 0;
        for (int i = 0; i < n; i++) {
            int v = R.Next(1, sides + 1);
            dice[i] = v;
            if (v > best) { best = v; highestIndex = i; }
        }
        return best;
    }

    // Roll n dice, keep the lowest. No LINQ: min and fumble found in one pass.
    public static RollResult RollMalice(int n, int sides = 20) {
        var dice = new int[n];
        int lowest = int.MaxValue;
        int lowestIndex = 0;
        bool fumble = false;
        for (int i = 0; i < n; i++) {
            int v = R.Next(1, sides + 1);
            dice[i] = v;
            if (v < lowest) { lowest = v; lowestIndex = i; }
            if (v == 1) fumble = true;
        }
        return new RollResult(dice, lowest, fumble, lowestIndex);
    }

    // Scripted loss: one die is guaranteed below minNeeded, so the check fails.
    public static RollResult RollForcedMiss(int n, int minNeeded, int sides = 20) {
        var dice = new int[n];
        int lowest = int.MaxValue;
        int lowestIndex = 0;
        bool fumble = false;
        int rigged = R.Next(0, n);
        for (int i = 0; i < n; i++) {
            int v = (i == rigged) ? R.Next(1, Math.Max(2, minNeeded)) : R.Next(1, sides + 1);
            dice[i] = v;
            if (v < lowest) { lowest = v; lowestIndex = i; }
            if (v == 1) fumble = true;
        }
        return new RollResult(dice, lowest, fumble, lowestIndex);
    }
}
