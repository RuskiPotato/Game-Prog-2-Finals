
using UnityEngine;

// All scaling rules live here so balance changes are easy to adjust.
public static class Balance
{
    public const int CunningDivisor = 5;

    public static int AttackMod(int stat) => stat;

    public static int HeroDefenseDc(int agi) => 7 + agi;

    // A clash is won by the higher total. A tie is not a win.
    public static bool WinsClash(
        int heroTotal,
        int enemyTotal,
        bool opposed,
        int defenseDc)
    {
        return opposed
            ? heroTotal > enemyTotal
            : heroTotal >= defenseDc;
    }

    // Determine the damage tier of a successful attack.
    public static RollBand EvaluateBand(
        int total,
        int defenseDc,
        bool wonClash,
        bool fumble)
    {
        if (!wonClash || fumble)
            return RollBand.Miss;

        if (total < defenseDc)
            return RollBand.Glancing;

        if (total >= defenseDc + 10)
            return RollBand.Crushing;

        return RollBand.Standard;
    }

    // Apply damage scaling for the attack tier.
    public static int ApplyBandDamage(
        int rawDamage,
        RollBand band,
        bool boosted)
    {
        switch (band)
        {
            case RollBand.Glancing:
                return Mathf.Max(1, rawDamage / 2);

            case RollBand.Crushing:
                return rawDamage * (boosted ? 2 : 1);

            case RollBand.Standard:
                return rawDamage;

            default:
                return 0;
        }
    }

    public static int ScriptedLossDamage(
        int maxHp,
        int currentHp,
        int rounds,
        int round,
        bool defending)
    {
        if (round >= rounds)
            return currentHp;

        int dmg = Mathf.CeilToInt(maxHp / (float)rounds);

        if (defending)
            dmg = (dmg + 1) / 2;

        return Mathf.Min(dmg, currentHp - 1);
    }
}
