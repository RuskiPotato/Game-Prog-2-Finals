using System.Collections;
using UnityEngine;

// Resolves a hero attack and, in Real mode, the opposed clash.
public class HeroAttackResolver
{
    readonly BattleInput input;
    readonly ICheat rerollCheat;
    readonly EnemyDiceMode enemyDiceMode;

    public HeroAttackResolver(
        BattleInput input,
        ICheat rerollCheat,
        EnemyDiceMode enemyDiceMode = EnemyDiceMode.Real)
    {
        this.input = input;
        this.rerollCheat = rerollCheat;
        this.enemyDiceMode = enemyDiceMode;
    }

    static RollResult Roll(
        BattleContext ctx,
        int diceCount,
        int minNeeded)
    {
        if (ctx.Scripted)
            return DiceService.RollForcedMiss(diceCount, minNeeded);

        return ctx.CardBonusDice > 0
            ? DiceService.RollMaliceWithBonusDice(
                diceCount,
                ctx.CardBonusDice)
            : DiceService.RollMalice(diceCount);
    }

    bool CanReroll(BattleContext ctx)
    {
        return rerollCheat != null
            && ctx.Progress.IsUnlocked(rerollCheat.Id)
            && rerollCheat.CanUse(ctx)
            && ctx.Fate.Audacity >= CheatRunner.AudacityCost(rerollCheat, ctx);
    }

    public IEnumerator Run(BattleContext ctx)
    {
        ctx.State = BattleState.Roll;

        var fate = ctx.Fate;
        var enemy = ctx.Enemy;

        int mod = ctx.RollMod;
        int dc = enemy.DefenseDc;
        int heroDiceCount = ctx.CurrentDice;
        int displayedHeroDiceCount = heroDiceCount +
            (!ctx.Scripted ? ctx.CardBonusDice : 0);

        bool boosted =
            ctx.RemovedMaliceDice > 0 ||
            ctx.SleightArmed;

        // An opposed clash only happens against an enemy
        // whose currently selected move is an attack.
        bool hasEnemyAttack = enemy.AttacksThisTurn;

        bool opposed =
            hasEnemyAttack &&
            enemyDiceMode == EnemyDiceMode.Real;

        int enemyDiceCount = 0;
        int enemyTotal = 0;

        if (opposed)
        {
            enemyDiceCount = 1 + fate.MaliceDice;

            ctx.EnemyClashBest = DiceService.RollHighestDice(
                enemyDiceCount,
                out ctx.EnemyClashDice,
                out ctx.EnemyClashKeptIndex
            );

            ctx.EnemyClashRollPrepared = true;

            enemyTotal =
                ctx.EnemyClashBest + enemy.AttackModifier;

            ctx.EnemyClashTotal = enemyTotal;
        }

        // In a scripted encounter, ensure the hero cannot win.
        int scriptedCap;

        if (opposed)
        {
            scriptedCap = Mathf.Max(
                2,
                enemyTotal - mod + 1
            );
        }
        else
        {
            scriptedCap = Mathf.Max(2, dc - mod);
        }

        var roll = Roll(
            ctx,
            heroDiceCount,
            scriptedCap
        );

        if (ctx.SleightArmed)
        {
            var secondRoll = Roll(
                ctx,
                heroDiceCount,
                scriptedCap
            );

            if (secondRoll.Lowest > roll.Lowest)
                roll = secondRoll;
        }

        // Reveal the dice.
        yield return ctx.View.ClashApproach();

        if (opposed)
        {
            yield return ctx.View.TumbleClash(
                displayedHeroDiceCount,
                enemyDiceCount
            );

            ctx.View.ShowRoll(roll);

            ctx.View.ShowEnemyRoll(
                ctx.EnemyClashDice,
                ctx.EnemyClashKeptIndex
            );
        }
        else
        {
            yield return ctx.View.Tumble(displayedHeroDiceCount);
            ctx.View.ShowRoll(roll);
        }

        ctx.State = BattleState.Resolve;

        int total = roll.Lowest + mod;

        ctx.HeroClashTotal = total;

        bool wonClash;
        bool tiedClash = false;

        if (opposed)
        {
            wonClash = !ctx.Scripted &&
                Balance.WinsClash(
                    total,
                    enemyTotal,
                    true,
                    dc
                );

            tiedClash =
                !ctx.Scripted &&
                total == enemyTotal;
        }
        else
        {
            wonClash =
                !ctx.Scripted &&
                total >= dc;
        }

        // Loaded Dice can reroll a lost clash.
        // The enemy's already-revealed roll remains fixed.
        if (!wonClash && CanReroll(ctx))
        {
            input.ChosenReaction = Reaction.None;
            input.AcceptingReaction = true;

            ctx.View.ShowReactionPanel(true);

            string targetLabel = opposed
                ? $"vs Enemy {enemyTotal}"
                : $"vs DC {dc}";

            ctx.Log(
                $"Clash lost ({total} {targetLabel}). " +
                "Accept it, or reroll with Loaded Dice?"
            );

            yield return new WaitUntil(
                () => input.ChosenReaction != Reaction.None
            );

            input.AcceptingReaction = false;
            ctx.View.ShowReactionPanel(false);

            if (
                input.ChosenReaction == Reaction.Reroll &&
                ctx.PayCheat(rerollCheat)
            )
            {
                boosted = true;

                roll = Roll(
                    ctx,
                    heroDiceCount,
                    scriptedCap
                );

                yield return ctx.View.Tumble(displayedHeroDiceCount);
                ctx.View.ShowRoll(roll);

                total = roll.Lowest + mod;

                ctx.HeroClashTotal = total;

                if (opposed)
                {
                    wonClash =
                        !ctx.Scripted &&
                        Balance.WinsClash(
                            total,
                            enemyTotal,
                            true,
                            dc
                        );

                    tiedClash =
                        !ctx.Scripted &&
                        total == enemyTotal;
                }
                else
                {
                    wonClash =
                        !ctx.Scripted &&
                        total >= dc;

                    tiedClash = false;
                }
            }
        }

        // Determine the damage band using the enemy's Defense DC.
        RollBand band = Balance.EvaluateBand(
            total,
            dc,
            wonClash,
            roll.Fumble
        );

        ctx.ResolvePlayerAttackRollDamage(total);

        ctx.HeroLanded = wonClash;
        ctx.ClashTie = tiedClash;

        bool defiant =
            band == RollBand.Crushing &&
            boosted;

        if (defiant)
            fate.AddFate(fate.DefiantCritDelta);
        else
            fate.ApplyRollOutcome(band);

        // Resolve a successful attack.
        if (wonClash)
        {
            var skill = ctx.ActiveSkill;

            int rawDamage = skill != null
                ? skill.RollDamage(ctx.Hero, ctx.CardDamageBonus)
                : ctx.Hero.RollDamage() + ctx.CardDamageBonus;

            int damage = Balance.ApplyBandDamage(
                rawDamage,
                band,
                boosted
            );

            // Preserve the existing damage/status/shield pipeline.
            damage = enemy.ReceiveHit(damage);

            string comparison = opposed
                ? $"{total} vs {enemyTotal} (DC {dc})"
                : $"{total} vs DC {dc}";

            string hitCount =
                skill != null && skill.hits > 1
                    ? $" ({skill.hits} hits)"
                    : "";

            switch (band)
            {
                case RollBand.Glancing:
                    ctx.Log(
                        $"Glancing Hit! {comparison} — " +
                        $"half damage ({damage}){hitCount}."
                    );
                    break;

                case RollBand.Crushing:
                    ctx.Log(
                        defiant
                            ? $"DEFIANT CRUSH! {comparison} — {damage} damage{hitCount}!"
                            : $"Crushing Hit! {comparison} — {damage} damage{hitCount}!"
                    );
                    break;

                default:
                    ctx.Log(
                        $"Hit! {comparison} — {damage} damage{hitCount}."
                    );
                    break;
            }

            // Preserve attack-skill effects.
            if (skill != null && !enemy.IsDead)
                SkillEffectRunner.Apply(ctx, skill);

            if (enemy.IsDead)
                fate.AddFate(fate.EnemyDefeatedDelta);
        }
        else if (tiedClash)
        {
            ctx.Log(
                $"CLASH TIE! ({total} vs {enemyTotal}) — " +
                "Neither side lands a hit."
            );
        }
        else
        {
            string comparison = opposed
                ? $"{total} vs {enemyTotal}"
                : $"{total} vs DC {dc}";

            ctx.Log(
                roll.Fumble
                    ? $"FUMBLE! ({comparison})"
                    : $"Clash Lost. ({comparison})"
            );

            var taunts = ctx.Game.Config.taunts;

            if (
                ctx.Progress.Phase != GamePhase.Tutorial &&
                taunts != null &&
                taunts.Length > 0
            )
            {
                ctx.View.SetGm(
                    taunts[Random.Range(0, taunts.Length)]
                );
            }
        }

        bool enemyWinsClash =
            opposed &&
            !wonClash &&
            !tiedClash;

        // In Visual mode, a missed hero attack still lets the enemy land.
        if (enemyDiceMode == EnemyDiceMode.Visual && hasEnemyAttack)
            enemyWinsClash = !wonClash;

        yield return ctx.View.ClashOutcome(
            wonClash,
            enemyWinsClash,
            tiedClash
        );

        ctx.RaiseChanged();

        yield return new WaitForSeconds(0.35f);
    }
}