using NUnit.Framework;
using UnityEngine;

public class SkillTests {
    class Dummy : Unit {
        public Dummy(int hp) { MaxHp = Hp = hp; }
        public override string DisplayName => "Dummy";
        public override int BaseAgi => 0;
        public override int BaseDefenseDc => 10;
        public override int RollDamage() => 0;
    }

    [Test]
    public void Status_expires_after_its_turns_and_refreshes_instead_of_stacking() {
        var t = new StatusTracker();
        t.Add(EnemyStatusEffectType.AttackDown, 2, 2);
        t.Add(EnemyStatusEffectType.AttackDown, 2, 3);
        Assert.AreEqual(2, t.Total(EnemyStatusEffectType.AttackDown));
        t.Tick(); t.Tick();
        Assert.AreEqual(2, t.Total(EnemyStatusEffectType.AttackDown));
        t.Tick();
        Assert.AreEqual(0, t.Total(EnemyStatusEffectType.AttackDown));
    }

    [Test]
    public void Shield_soaks_damage_before_hp() {
        var u = new Dummy(10);
        u.AddShield(4);
        u.TakeDamage(6);
        Assert.AreEqual(0, u.Shield);
        Assert.AreEqual(8, u.Hp);
    }

    [Test]
    public void Defense_and_roll_statuses_change_the_numbers_units_expose() {
        var u = new Dummy(10);
        u.Statuses.Add(EnemyStatusEffectType.DefenseDown, 3, 2);
        u.Statuses.Add(EnemyStatusEffectType.AttackUp, 2, 2);
        Assert.AreEqual(7, u.DefenseDc);
        Assert.AreEqual(2, u.StatusRollMod);
    }

    [Test]
    public void ReceiveHit_applies_vulnerable_reduction_and_percent_guard() {
        var u = new Dummy(50);
        u.Statuses.Add(EnemyStatusEffectType.Vulnerable, 2, 1);
        Assert.AreEqual(12, u.ReceiveHit(10));
        u.Statuses.Clear();
        u.Statuses.Add(EnemyStatusEffectType.DamageReductionPercent, 50, 1);
        Assert.AreEqual(5, u.ReceiveHit(10));
        Assert.AreEqual(50 - 12 - 5, u.Hp);
    }

    [Test]
    public void Regeneration_heals_at_round_end() {
        var u = new Dummy(10);
        u.TakeDamage(6);
        u.Statuses.Add(EnemyStatusEffectType.Regeneration, 2, 2);
        Assert.AreEqual(2, u.EndRound());
        Assert.AreEqual(6, u.Hp);
    }

    [Test]
    public void Cooldown_n_means_ready_again_n_rounds_later() {
        var skill = ScriptableObject.CreateInstance<SkillData>();
        skill.cooldown = 2;
        var cd = new SkillCooldowns();
        cd.Start(skill);
        Assert.IsFalse(cd.IsReady(skill));
        cd.Tick();
        Assert.IsFalse(cd.IsReady(skill));
        cd.Tick();
        Assert.IsTrue(cd.IsReady(skill));
        Object.DestroyImmediate(skill);
    }

    [Test]
    public void Statuses_have_readable_names() {
        Assert.AreEqual("Attack Down", StatusTracker.NameOf(EnemyStatusEffectType.AttackDown));
        Assert.AreEqual("Damage Reduction Percent", StatusTracker.NameOf(EnemyStatusEffectType.DamageReductionPercent));
    }
}
