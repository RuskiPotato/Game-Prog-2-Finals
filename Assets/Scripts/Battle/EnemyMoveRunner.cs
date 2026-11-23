using UnityEngine;

// Runs the parts of an EnemyMoveData the clash itself doesn't: guard, effects, status.
//   Attack move : effects fire only if the attack lands (OnLand).
//   Defend move : guard + effects fire at the start of the round (OnRoundStart), before the hero acts.
//   Skill move  : effects fire in the enemy's phase, no attack (OnSkillPhase).
public static class EnemyMoveRunner {
    public static void OnRoundStart(BattleContext ctx) {
        var move = ctx.Enemy.CurrentMove;
        if (move == null || move.moveType != EnemyMoveType.Defend) return;
        if (move.guardAmount > 0) ctx.Enemy.AddShield(move.guardAmount);
        if (move.damageReductionPercent > 0 && move.defenseDurationTurns > 0)
            ctx.Enemy.Statuses.Add(EnemyStatusEffectType.DamageReductionPercent, move.damageReductionPercent, move.defenseDurationTurns);
        ApplyEffects(ctx, move, 0);
        ctx.Log($"{ctx.Enemy.DisplayName} uses {ctx.Enemy.CurrentMoveName}.");
    }

    public static void OnLand(BattleContext ctx) {
        var move = ctx.Enemy.CurrentMove;
        if (move != null && move.moveType == EnemyMoveType.Attack) ApplyEffects(ctx, move, 1);
    }

    public static void OnSkillPhase(BattleContext ctx) {
        var move = ctx.Enemy.CurrentMove;
        if (move == null || move.moveType != EnemyMoveType.Skill) return;
        ctx.Log($"{ctx.Enemy.DisplayName} uses {ctx.Enemy.CurrentMoveName}.");
        ApplyEffects(ctx, move, 1);
    }

    // extraTurns: effects applied during the enemy's phase get +1 so "1 turn" still covers the next hero turn.
    static void ApplyEffects(BattleContext ctx, EnemyMoveData move, int extraTurns) {
        if (move.effects == null) return;
        foreach (var e in move.effects) {
            if (e == null || !EffectOps.Passes(e.chancePercent)) continue;
            Unit target = e.target == EnemyEffectTarget.Self ? (Unit)ctx.Enemy : ctx.Hero;
            switch (e.effectType) {
                case EnemyMoveEffectType.DealDamage:
                    int dealt = target.ReceiveHit(e.amount);
                    ctx.Log($"{target.DisplayName} takes {dealt} more damage.");
                    break;
                case EnemyMoveEffectType.ApplyStatus:
                    target.Statuses.Add(e.statusEffect, e.amount, e.durationTurns + extraTurns);
                    ctx.Log($"{target.DisplayName}: {StatusTracker.NameOf(e.statusEffect)} {e.amount} ({e.durationTurns}t).");
                    break;
                case EnemyMoveEffectType.Heal:
                    target.Heal(e.amount);
                    break;
                case EnemyMoveEffectType.GainGuard:
                    target.AddShield(e.amount);
                    break;
                case EnemyMoveEffectType.RemoveStatus:
                    target.Statuses.Remove(e.statusEffect);
                    break;
                case EnemyMoveEffectType.ModifyAudacity:
                    EffectOps.ChangeAudacity(ctx, e.amount);
                    break;
            }
        }
        ctx.RaiseChanged();
    }
}
