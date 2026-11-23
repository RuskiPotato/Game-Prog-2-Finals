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
    public override int BaseAgi => agi;
    public override int BaseDefenseDc => Balance.HeroDefenseDc(Agi);   // Agi already includes Agility statuses
    public override int RollDamage() => Mathf.Max(0, DiceService.Die(Class.weaponDie) + AttackStatValue + StatusDamageBonus);

    // Only the attack stat's up/down statuses matter to this hero; Weakness always does.
    public override int StatusDamageBonus => Class.attackStat == AttackStat.Strength
        ? Statuses.Total(EnemyStatusEffectType.StrengthUp) - Statuses.Total(EnemyStatusEffectType.StrengthDown) - Statuses.Total(EnemyStatusEffectType.Weakness)
        : Statuses.Total(EnemyStatusEffectType.MagicUp) - Statuses.Total(EnemyStatusEffectType.MagicDown) - Statuses.Total(EnemyStatusEffectType.Weakness);
    public int StatusAttackModifier => StatusRollMod;

    public int AttackStatValue => Class.attackStat == AttackStat.Strength ? Str : Mag;
    public int AttackModifier => Balance.AttackMod(AttackStatValue) + StatusRollMod;

    public int CheatCost(string cheatId, int baseCost) {
        int cost = baseCost;
        foreach (var m in Class.cheatCostMods) if (m.cheatId == cheatId) cost += m.delta;
        return Mathf.Max(0, cost);
    }
}
