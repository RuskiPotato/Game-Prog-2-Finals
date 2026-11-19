using System.Collections;
using UnityEngine;

// Resolves one hero attack: roll, optional Loaded Dice reroll window, Fate change, damage.
public class HeroAttackResolver {
    readonly BattleInput input;
    readonly ICheat rerollCheat;          // Loaded Dice, used after a miss

    public HeroAttackResolver(BattleInput input, ICheat rerollCheat) {
        this.input = input;
        this.rerollCheat = rerollCheat;
    }

    static RollResult Roll(BattleContext ctx, int n, int needed) =>
        ctx.Scripted ? DiceService.RollForcedMiss(n, needed) : DiceService.RollMalice(n);

    bool CanReroll(BattleContext ctx) =>
        rerollCheat != null
        && ctx.Progress.IsUnlocked(rerollCheat.Id)
        && rerollCheat.CanUse(ctx)
        && ctx.Fate.Audacity >= CheatRunner.AudacityCost(rerollCheat, ctx);

    public IEnumerator Run(BattleContext ctx) {
        ctx.State = BattleState.Roll;
        var fate = ctx.Fate;
        var enemy = ctx.Enemy;
        int mod = ctx.RollMod;
        int dc = enemy.DefenseDc;
        int needed = dc - mod;
        int n = ctx.CurrentDice;
        bool boosted = ctx.RemovedMaliceDice > 0 || ctx.SleightArmed;

        var roll = Roll(ctx, n, needed);
        if (ctx.SleightArmed) {                       // reroll, keep the better result
            var second = Roll(ctx, n, needed);
            if (second.Lowest > roll.Lowest) roll = second;
        }
        yield return ctx.View.Tumble(n);
        ctx.View.ShowRoll(roll);

        ctx.State = BattleState.Resolve;
        int total = roll.Lowest + mod;
        bool success = total >= dc;

        // Reaction window: Loaded Dice after the roll cancels a miss and rerolls once
        if (!success && CanReroll(ctx)) {
            input.ChosenReaction = Reaction.None;
            input.AcceptingReaction = true;
            ctx.View.ShowReactionPanel(true);
            ctx.Log($"Miss ({total} vs {dc}). Accept it, or cancel it with Loaded Dice?");
            yield return new WaitUntil(() => input.ChosenReaction != Reaction.None);
            input.AcceptingReaction = false;
            ctx.View.ShowReactionPanel(false);

            if (input.ChosenReaction == Reaction.Reroll && ctx.PayCheat(rerollCheat)) {
                boosted = true;
                roll = Roll(ctx, n, needed);
                yield return ctx.View.Tumble(n);
                ctx.View.ShowRoll(roll);
                total = roll.Lowest + mod;
                success = total >= dc;
            }
        }

        bool defiant = success && boosted && total - dc >= 10;
        if (defiant) fate.AddFate(fate.DefiantCritDelta);
        else fate.ApplyRollOutcome(success, total - dc, roll.Fumble);

        if (success) {
            int dmg = ctx.Hero.RollDamage();
            if (defiant) dmg *= 2;
            enemy.TakeDamage(dmg);
            ctx.Log(defiant ? $"DEFIANT CRIT! {total} vs {dc}. {dmg} damage."
                            : $"Hit! {total} vs {dc}. {dmg} damage.");
            if (enemy.IsDead) fate.AddFate(fate.EnemyDefeatedDelta);
        } else {
            ctx.Log(roll.Fumble ? $"FUMBLE! ({total} vs {dc})" : $"Miss. {total} vs {dc}");
            var taunts = ctx.Game.Config.taunts;
            if (ctx.Progress.Phase != GamePhase.Tutorial && taunts != null && taunts.Length > 0)
                ctx.View.SetGm(taunts[Random.Range(0, taunts.Length)]);
        }
        ctx.RaiseChanged();
        yield return new WaitForSeconds(0.6f);
    }
}
