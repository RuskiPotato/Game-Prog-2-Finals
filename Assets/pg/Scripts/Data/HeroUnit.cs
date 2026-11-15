using UnityEngine;

public class HeroUnit {
    public ClassData Class { get; }
    public int MaxHp, Hp, Str, Mag, Agi, Cunning;

    public HeroUnit(ClassData c) {
        Class = c;
        MaxHp = Hp = c.baseStats.hp;
        Str = c.baseStats.str; Mag = c.baseStats.mag;
        Agi = c.baseStats.agi; Cunning = c.baseStats.cunning;
    }

    public int AttackStatValue => Class.attackStat == AttackStat.Strength ? Str : Mag;
    public int AttackModifier => Balance.AttackMod(AttackStatValue);

    public int CheatCost(string cheatId, int baseCost) {
        int cost = baseCost;
        foreach (var m in Class.cheatCostMods) if (m.cheatId == cheatId) cost += m.delta;
        return Mathf.Max(0, cost);
    }
}
