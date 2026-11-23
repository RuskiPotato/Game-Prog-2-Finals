// Applies a SkillData's effect list. Heal / Shield / Status / Audacity all go through Unit.
public static class SkillEffectRunner {
    public static void Apply(BattleContext ctx, SkillData skill) {
        if (skill == null || skill.effects == null) return;
        foreach (var e in skill.effects) {
            if (e == null || !EffectOps.Passes(e.chancePercent)) continue;
            Unit target = e.target == SkillEffectTarget.Self ? (Unit)ctx.Hero : ctx.Enemy;
            switch (e.effectType) {
                case SkillEffectType.Heal:
                    target.Heal(e.amount);
                    ctx.Log($"{target.DisplayName} heals {e.amount}.");
                    break;
                case SkillEffectType.Shield:
                    target.AddShield(e.amount);
                    ctx.Log($"{target.DisplayName} gains {e.amount} shield.");
                    break;
                case SkillEffectType.ApplyStatus:
                    target.Statuses.Add(e.statusEffect, e.amount, e.durationTurns);   // hero turn: duration 1 = this round
                    ctx.Log($"{target.DisplayName}: {StatusTracker.NameOf(e.statusEffect)} {e.amount} ({e.durationTurns}t).");
                    break;
                case SkillEffectType.RemoveStatus:
                    target.Statuses.Remove(e.statusEffect);
                    break;
                case SkillEffectType.ModifyAudacity:
                    EffectOps.ChangeAudacity(ctx, e.amount);
                    break;
            }
        }
        ctx.RaiseChanged();
    }
}
