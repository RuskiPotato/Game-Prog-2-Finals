using System;

public readonly struct RollResult
{
    public readonly int[] Dice;
    // Lowest Malice die, retained for compatibility with existing tools/tests.
    public readonly int Lowest;
    public readonly bool Fumble;
    public readonly int LowestIndex;
    // The die actually used for the final total. This may be the Fortune Die.
    public readonly int KeptIndex;
    public readonly int KeptValue;
    public readonly int FortuneIndex;

    public RollResult(int[] dice, int lowest, bool fumble, int lowestIndex = 0)
    {
        Dice = dice;
        Lowest = lowest;
        Fumble = fumble;
        LowestIndex = lowestIndex;
        KeptIndex = lowestIndex;
        KeptValue = lowest;
        FortuneIndex = -1;
    }

    public RollResult(
        int[] dice,
        int lowest,
        bool fumble,
        int lowestIndex,
        int keptIndex,
        int keptValue,
        int fortuneIndex)
    {
        Dice = dice;
        Lowest = lowest;
        Fumble = fumble;
        LowestIndex = lowestIndex;
        KeptIndex = keptIndex;
        KeptValue = keptValue;
        FortuneIndex = fortuneIndex;
    }
}

// The ONLY place combat dice are generated, always from the Combat stream.
public static class DiceService
{
    static Random R => Rng.Get(RngStream.Combat);

    public static void Seed(int seed) => Rng.Seed(seed);

    public static int D20() => R.Next(1, 21);

    public static int Die(int sides) =>
        R.Next(1, Math.Max(2, sides + 1));

    // Enemies keep the highest result.
    public static int RollHighest(int n, int sides = 20)
    {
        n = Math.Max(1, n);
        sides = Math.Max(1, sides);

        int best = 0;

        for (int i = 0; i < n; i++)
        {
            int value = R.Next(1, sides + 1);

            if (value > best)
                best = value;
        }

        return best;
    }

    public static int RollHighestDice(
        int n,
        out int[] dice,
        out int highestIndex,
        int sides = 20)
    {
        n = Math.Max(1, n);
        sides = Math.Max(1, sides);

        dice = new int[n];

        int best = 0;
        highestIndex = 0;

        for (int i = 0; i < n; i++)
        {
            int value = R.Next(1, sides + 1);
            dice[i] = value;

            if (value > best)
            {
                best = value;
                highestIndex = i;
            }
        }

        return best;
    }

    // Hero keeps the lowest result.
    public static RollResult RollMalice(int n, int sides = 20)
    {
        n = Math.Max(1, n);
        sides = Math.Max(1, sides);

        var dice = new int[n];

        int lowest = int.MaxValue;
        int lowestIndex = 0;
        bool fumble = false;

        for (int i = 0; i < n; i++)
        {
            int value = R.Next(1, sides + 1);

            dice[i] = value;

            if (value < lowest)
            {
                lowest = value;
                lowestIndex = i;
            }

            if (value == 1)
                fumble = true;
        }

        return new RollResult(dice, lowest, fumble, lowestIndex);
    }

    // The hero keeps the lowest Malice die, then may use one separate Fortune Die
    // if it rolled higher. The Fortune Die is deliberately not another Malice die.
    public static RollResult RollMaliceWithFortune(int n, int sides = 20)
    {
        n = Math.Max(1, n);
        sides = Math.Max(1, sides);

        RollResult malice = RollMalice(n, sides);
        int fortune = R.Next(1, sides + 1);
        int[] dice = new int[n + 1];
        Array.Copy(malice.Dice, dice, n);
        int fortuneIndex = n;
        dice[fortuneIndex] = fortune;

        bool fortuneKept = fortune >= malice.Lowest;
        int keptIndex = fortuneKept ? fortuneIndex : malice.LowestIndex;
        int keptValue = Math.Max(malice.Lowest, fortune);
        bool fumble = malice.Fumble || fortune == 1;

        return new RollResult(
            dice,
            malice.Lowest,
            fumble,
            malice.LowestIndex,
            keptIndex,
            keptValue,
            fortuneIndex
        );
    }

    // Scripted loss: one Malice die is guaranteed to remain below minNeeded.
    // Scripted encounters intentionally do not receive the Fortune Die.
    public static RollResult RollForcedMiss(
        int n,
        int minNeeded,
        int sides = 20)
    {
        n = Math.Max(1, n);
        sides = Math.Max(1, sides);

        var dice = new int[n];

        int lowest = int.MaxValue;
        int lowestIndex = 0;
        bool fumble = false;

        int riggedIndex = R.Next(0, n);

        // Keep the forced value below the required number,
        // while never allowing it to exceed the die's sides.
        int cap = Math.Min(
            sides + 1,
            Math.Max(2, minNeeded)
        );

        for (int i = 0; i < n; i++)
        {
            int value = i == riggedIndex
                ? R.Next(1, cap)
                : R.Next(1, sides + 1);

            dice[i] = value;

            if (value < lowest)
            {
                lowest = value;
                lowestIndex = i;
            }

            if (value == 1)
                fumble = true;
        }

        return new RollResult(dice, lowest, fumble, lowestIndex);
    }
}