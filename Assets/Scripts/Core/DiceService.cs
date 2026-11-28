using System;

public readonly struct RollResult
{
    public readonly int[] Dice;
    public readonly int Lowest;
    public readonly bool Fumble;
    public readonly int LowestIndex;

    public RollResult(int[] dice, int lowest, bool fumble, int lowestIndex = 0)
    {
        Dice = dice;
        Lowest = lowest;
        Fumble = fumble;
        LowestIndex = lowestIndex;
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

    // Focused Roll: roll the normal Malice pool (keep its lowest), then
    // roll the bonus d20(s) and keep the higher of the two results.
    // The returned Dice array contains both pools for the clash display.
    // Fumble is based on the final kept die, so a higher Focused die can
    // rescue a natural 1 from the Malice pool.
    public static RollResult RollMaliceWithBonusDice(
        int maliceDiceCount,
        int bonusDiceCount,
        int sides = 20)
    {
        maliceDiceCount = Math.Max(1, maliceDiceCount);
        bonusDiceCount = Math.Max(0, bonusDiceCount);
        sides = Math.Max(1, sides);

        var dice = new int[maliceDiceCount + bonusDiceCount];

        int maliceLowest = int.MaxValue;
        int maliceLowestIndex = 0;

        for (int i = 0; i < maliceDiceCount; i++)
        {
            int value = R.Next(1, sides + 1);
            dice[i] = value;

            if (value < maliceLowest)
            {
                maliceLowest = value;
                maliceLowestIndex = i;
            }
        }

        int keptValue = maliceLowest;
        int keptIndex = maliceLowestIndex;

        for (int i = 0; i < bonusDiceCount; i++)
        {
            int index = maliceDiceCount + i;
            int value = R.Next(1, sides + 1);
            dice[index] = value;

            if (value > keptValue)
            {
                keptValue = value;
                keptIndex = index;
            }
        }

        return new RollResult(
            dice,
            keptValue,
            keptValue == 1,
            keptIndex
        );
    }

    // Scripted loss: one die is guaranteed to roll below minNeeded.
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