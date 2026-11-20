using UnityEngine;

// Base class for anything that fights. HeroUnit and EnemyUnit override the abstract parts.
public abstract class Unit : IDamageable {
    public int MaxHp { get; protected set; }
    public int Hp { get; protected set; }
    public bool IsDead => Hp <= 0;

    public abstract string DisplayName { get; }
    public abstract int Agi { get; }
    public abstract int DefenseDc { get; }       // DC an attacker must meet to hit this unit
    public abstract int RollDamage();

    public virtual void TakeDamage(int amount) => Hp = Mathf.Max(0, Hp - amount);
    public virtual void Heal(int amount) => Hp = Mathf.Min(MaxHp, Hp + amount);
    public void FullHeal() => Hp = MaxHp;
    public int RollInitiative() => DiceService.D20() + Agi;
}
