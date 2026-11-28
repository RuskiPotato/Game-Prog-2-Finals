
using UnityEngine;

public static class Odds
{
    // Probability that the lowest of n d20 rolls meets the DC.
    public static float HitChance(int dc, int mod, int n)
    {
        int k = dc - mod;

        if (k <= 1) return 1f;
        if (k > 20) return 0f;

        return Mathf.Pow((21 - k) / 20f, n);
    }

    // Probability that the highest of n d20 rolls meets the DC.
    public static float EnemyHitChance(int dc, int mod, int n)
    {
        int k = dc - mod;

        if (k <= 1) return 1f;
        if (k > 20) return 0f;

        return 1f - Mathf.Pow((k - 1) / 20f, n);
    }

    // Probability that the hero's lowest die total beats
    // the enemy's highest die total.
    public static float OpposedWinChance(
        int heroMod,
        int heroDice,
        int enemyMod,
        int enemyDice)
    {
        heroDice = Mathf.Max(1, heroDice);
        enemyDice = Mathf.Max(1, enemyDice);

        float winChance = 0f;

        // Enumerate possible kept hero die results.
        for (int heroRoll = 1; heroRoll <= 20; heroRoll++)
        {
            float heroProbability =
                Mathf.Pow((21f - heroRoll) / 20f, heroDice) -
                Mathf.Pow((20f - heroRoll) / 20f, heroDice);

            int heroTotal = heroRoll + heroMod;

            // Probability the enemy's highest die is lower.
            int enemyThreshold = heroTotal - enemyMod - 1;

            float enemyProbability;

            if (enemyThreshold <= 0)
                enemyProbability = 0f;
            else if (enemyThreshold >= 20)
                enemyProbability = 1f;
            else
                enemyProbability =
                    Mathf.Pow(enemyThreshold / 20f, enemyDice);

            winChance += heroProbability * enemyProbability;
        }

        return Mathf.Clamp01(winChance);
    }
}
