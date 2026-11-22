using UnityEngine;

// All scaling rules live here so balance changes are one-line edits.
public static class Balance {
    public const int CunningDivisor = 5;          // Suspicion bonus = Cunning / 5

    // Option A: the stat IS the modifier.
    public static int AttackMod(int stat) => stat;

    // Placeholder: the DC an enemy must meet to hit a hero.
    public static int HeroDefenseDc(int agi) => 7 + agi;


    public static int ScriptedLossDamage(int maxHp, int currentHp, int rounds, int round, bool defending) {
        if (round >= rounds) return currentHp;                       // final round: guaranteed knockout
        int dmg = Mathf.CeilToInt(maxHp / (float)rounds);
        if (defending) dmg = (dmg + 1) / 2;
        return Mathf.Min(dmg, currentHp - 1);                        // never kill early
    }
}
