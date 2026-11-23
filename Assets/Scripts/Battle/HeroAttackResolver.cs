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

        // Roll both sides before revealing either result. The enemy roll is real, seeded,
        // and uses Destiny's existing highest-of-(1 + Malice) rule.
        var roll = Roll(ctx, n, needed);
        if (ctx.SleightArmed) {
            var second = Roll(ctx, n, needed);
            if (second.Lowest > roll.Lowest) roll = second;
        }

        bool hasEnemyAttack = enemy.AttacksThisTurn;
        int enemyCount = 0;
        if (hasEnemyAttack) {
            enemyCount = 1 + fate.MaliceDice;
            ctx.EnemyClashBest = DiceService.RollHighestDice(enemyCount,
                out ctx.EnemyClashDice, out ctx.EnemyClashKeptIndex);
            ctx.EnemyClashRollPrepared = true;
        }

        yield return ctx.View.ClashApproach();
        if (hasEnemyAttack) {
            yield return ctx.View.TumbleClash(n, enemyCount);
            ctx.View.ShowRoll(roll);
            ctx.View.ShowEnemyRoll(ctx.EnemyClashDice, ctx.EnemyClashKeptIndex);
        } else {
            yield return ctx.View.Tumble(n);
            ctx.View.ShowRoll(roll);
        }

        ctx.State = BattleState.Resolve;
        int total = roll.Lowest + mod;
        bool success = total >= dc;

        // Loaded Dice rerolls the player's dice only; the enemy's revealed result remains fixed.
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

        // Roll-derived attack damage uses the final attack total, including any Loaded Dice reroll.
        ctx.ResolvePlayerAttackRollDamage(total);
        ctx.HeroLanded = success;
        bool defiant = success && boosted && total - dc >= 10;
        if (defiant) fate.AddFate(fate.DefiantCritDelta);
        else fate.ApplyRollOutcome(success, total - dc, roll.Fumble);

        if (success) {
            var skill = ctx.ActiveSkill;
            int dmg = skill != null ? skill.RollDamage(ctx.Hero, ctx.CardDamageBonus)
                                    : ctx.Hero.RollDamage() + ctx.CardDamageBonus;
            if (defiant) dmg *= 2;
            dmg = enemy.ReceiveHit(dmg);                      // Vulnerable / Guard / shield are applied here
            string hitsText = skill != null && skill.hits > 1 ? $" ({skill.hits} hits)" : "";
            ctx.Log(defiant ? $"DEFIANT CRIT! {total} vs {dc}. {dmg} damage{hitsText}."
                            : $"Hit! {total} vs {dc}. {dmg} damage{hitsText}.");
            if (skill != null && !enemy.IsDead) SkillEffectRunner.Apply(ctx, skill);
            if (enemy.IsDead) fate.AddFate(fate.EnemyDefeatedDelta);
        } else {
            ctx.Log(roll.Fumble ? $"FUMBLE! ({total} vs {dc})" : $"Miss. {total} vs {dc}");
            var taunts = ctx.Game.Config.taunts;
            if (ctx.Progress.Phase != GamePhase.Tutorial && taunts != null && taunts.Length > 0)
                ctx.View.SetGm(taunts[Random.Range(0, taunts.Length)]);
        }

        bool enemyLands = hasEnemyAttack && !ctx.Scripted &&
            (ctx.EnemyClashBest + enemy.AttackModifier >= ctx.Hero.DefenseDc);
        if (ctx.Scripted) enemyLands = hasEnemyAttack;
        yield return ctx.View.ClashOutcome(success, enemyLands && !success);
        ctx.RaiseChanged();
        yield return new WaitForSeconds(0.35f);
    }
}
