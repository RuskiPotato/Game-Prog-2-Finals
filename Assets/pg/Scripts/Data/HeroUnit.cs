using UnityEngine;

public class HeroUnit : Unit {
    public ClassData Class { get; }
    public int Str, Mag, Cunning;
    readonly int agi;

    public HeroUnit(ClassData c) {
        Class = c;
        MaxHp = Hp = c.baseStats.hp;
        Str = c.baseStats.str; Mag = c.baseStats.mag; Cunning = c.baseStats.cunning;
        agi = c.baseStats.agi;
    }

    public override string DisplayName => Class.displayName;
    public override int Agi => agi;
    public override int DefenseDc => Balance.HeroDefenseDc(Agi);
    public override int RollDamage() => DiceService.Die(Class.weaponDie) + AttackStatValue;

    public int AttackStatValue => Class.attackStat == AttackStat.Strength ? Str : Mag;
    public int AttackModifier => Balance.AttackMod(AttackStatValue);

    public int CheatCost(string cheatId, int baseCost) {
        int cost = baseCost;
        foreach (var m in Class.cheatCostMods) if (m.cheatId == cheatId) cost += m.delta;
        return Mathf.Max(0, cost);
    }
}
