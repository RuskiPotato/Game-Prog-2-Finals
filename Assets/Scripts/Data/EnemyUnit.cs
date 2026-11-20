// Runtime wrapper around an EnemyData asset.
public class EnemyUnit : Unit {
    public EnemyData Data { get; }

    public EnemyUnit(EnemyData data) { Data = data; MaxHp = Hp = data.hp; }

    public override string DisplayName => Data.displayName;
    public override int Agi => Data.agi;
    public override int DefenseDc => Data.defenseDc;
    public override int RollDamage() => DiceService.Die(Data.damageDie) + Data.damageBonus;

    public int AttackModifier => Data.attackMod;
    public bool Attacks => Data.attacks;
}
