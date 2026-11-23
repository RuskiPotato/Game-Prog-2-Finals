using UnityEngine;

// Base class for anything that fights. HeroUnit and EnemyUnit override the abstract parts.
public abstract class Unit : IDamageable {
    public int MaxHp { get; protected set; }
    public int Hp { get; protected set; }
    public int Shield { get; private set; }
    public StatusTracker Statuses { get; } = new StatusTracker();
    public bool IsDead => Hp <= 0;

    public abstract string DisplayName { get; }
    public abstract int BaseAgi { get; }
    public abstract int BaseDefenseDc { get; }   // DC an attacker must meet, before statuses
    public abstract int RollDamage();

    // Statuses flow through the same three numbers everything else already reads.
    public int Agi => BaseAgi + StatusAgiMod;
    public virtual int DefenseDc => BaseDefenseDc + StatusDefenseMod;

    public int StatusRollMod => Statuses.Total(EnemyStatusEffectType.AttackUp)
                              - Statuses.Total(EnemyStatusEffectType.AttackDown)
                              - Statuses.Total(EnemyStatusEffectType.AccuracyDown);
    public int StatusDefenseMod => Statuses.Total(EnemyStatusEffectType.DefenseUp)
                                 - Statuses.Total(EnemyStatusEffectType.DefenseDown);
    public int StatusAgiMod => Statuses.Total(EnemyStatusEffectType.AgilityUp)
                             - Statuses.Total(EnemyStatusEffectType.AgilityDown);
    public virtual int StatusDamageBonus => Statuses.Total(EnemyStatusEffectType.StrengthUp)
                                          + Statuses.Total(EnemyStatusEffectType.MagicUp)
                                          - Statuses.Total(EnemyStatusEffectType.StrengthDown)
                                          - Statuses.Total(EnemyStatusEffectType.MagicDown)
                                          - Statuses.Total(EnemyStatusEffectType.Weakness);

    // Raw damage (IDamageable): the shield soaks first.
    public virtual void TakeDamage(int amount) {
        amount = Mathf.Max(0, amount);
        int absorbed = Mathf.Min(Shield, amount);
        Shield -= absorbed;
        Hp = Mathf.Max(0, Hp - (amount - absorbed));
    }

    // A hit from the other side: Vulnerable / Damage Reduction / % reduction first, then the shield, then HP. Returns damage after statuses.
    public int ReceiveHit(int raw) {
        int dmg = raw + Statuses.Total(EnemyStatusEffectType.Vulnerable)
                      - Statuses.Total(EnemyStatusEffectType.DamageReduction);
        int pct = Mathf.Clamp(Statuses.Total(EnemyStatusEffectType.DamageReductionPercent), 0, 90);
        dmg = Mathf.Max(0, (Mathf.Max(0, dmg) * (100 - pct) + 99) / 100);
        TakeDamage(dmg);
        return dmg;
    }

    public void AddShield(int amount) => Shield += Mathf.Max(0, amount);
    public virtual void Heal(int amount) => Hp = Mathf.Min(MaxHp, Hp + Mathf.Max(0, amount));
    public void FullHeal() => Hp = MaxHp;
    public int RollInitiative() => DiceService.D20() + Agi;

    public void ClearBattleState() { Shield = 0; Statuses.Clear(); }

    // End of round: Regeneration heals, then every status loses a turn.
    public int EndRound() {
        int regen = IsDead ? 0 : Mathf.Max(0, Statuses.Total(EnemyStatusEffectType.Regeneration));
        if (regen > 0) Heal(regen);
        Statuses.Tick();
        return regen;
    }
}
