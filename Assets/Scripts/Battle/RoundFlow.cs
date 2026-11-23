using UnityEngine;

// What happens at the edges of a round, kept out of BattleController.
public class RoundFlow {
    // The enemy picks its move first (weighted random), so the clash card can show it and Defend moves take effect before you act.
    public void Begin(BattleContext ctx) {
        ctx.Enemy.ChooseMove();
        EnemyMoveRunner.OnRoundStart(ctx);
        ctx.RaiseChanged();
    }

    public void End(BattleContext ctx) {
        ctx.State = BattleState.RoundEnd;
        ctx.Cooldowns.Tick();
        int heroRegen = ctx.Hero.EndRound();
        int enemyRegen = ctx.Enemy.EndRound();
        if (heroRegen > 0) ctx.Log($"{ctx.Hero.DisplayName} regenerates {heroRegen}.");
        if (enemyRegen > 0) ctx.Log($"{ctx.Enemy.DisplayName} regenerates {enemyRegen}.");
        ctx.Fate.Audacity = Mathf.Min(5, ctx.Fate.Audacity + 1);
        ctx.RaiseChanged();
    }
}
