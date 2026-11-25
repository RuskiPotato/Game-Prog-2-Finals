
using System.Collections;
using UnityEngine;

public class EnemyTurnHandler : ITurnHandler
{
    readonly EnemyDiceMode mode;

    public EnemyTurnHandler(EnemyDiceMode mode = EnemyDiceMode.Visual)
    {
        this.mode = mode;
    }

    public IEnumerator Run(BattleContext ctx)
    {
        ctx.State = BattleState.EnemyTurn;
        ctx.RaiseChanged();

        yield return new WaitForSeconds(0.3f);

        var enemy = ctx.Enemy;
        var hero = ctx.Hero;
        var fate = ctx.Fate;

        // The move was already chosen at round start.
        // Do not call ChooseMove() again here.
        if (!enemy.Attacks)
        {
            ctx.Log($"{enemy.DisplayName} just stands there.");
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        // Defend and Skill moves do not perform attack clashes.
        if (!enemy.AttacksThisTurn)
        {
            if (enemy.CurrentMoveType == EnemyMoveType.Skill)
                EnemyMoveRunner.OnSkillPhase(ctx);

            ctx.Defending = false;
            ctx.RaiseChanged();

            yield return new WaitForSeconds(0.6f);
            yield break;
        }

        bool isDefendClash =
            ctx.ChosenAction == PlayerAction.Defend;

        if (isDefendClash)
            yield return ctx.View.ClashApproach();

        bool landed;
        bool tied = false;

        if (mode == EnemyDiceMode.Real)
        {
            // The real enemy roll may already have been prepared
            // during the hero's opposed attack roll.
            if (!ctx.EnemyClashRollPrepared)
            {
                int n = 1 + fate.MaliceDice;

                ctx.EnemyClashBest = DiceService.RollHighestDice(
                    n,
                    out ctx.EnemyClashDice,
                    out ctx.EnemyClashKeptIndex
                );

                ctx.EnemyClashRollPrepared = true;

                yield return ctx.View.TumbleEnemy(n);

                ctx.View.ShowEnemyRoll(
                    ctx.EnemyClashDice,
                    ctx.EnemyClashKeptIndex
                );
            }

            int total =
                ctx.EnemyClashBest + enemy.AttackModifier;

            ctx.EnemyClashTotal = total;

            ctx.ResolveEnemyAttackRollDefense(total);

            int def = hero.DefenseDc;

            // If the hero already won the opposed clash,
            // the enemy's attack is cancelled.
            if (ctx.HeroLanded)
            {
                landed = false;
            }
            else if (ctx.ClashTie)
            {
                landed = false;
                tied = true;
            }
            else
            {
                landed = ctx.Scripted || total >= def;
            }

            if (tied)
            {
                ctx.Log(
                    $"{enemy.DisplayName} and the hero tie in the clash. Neither lands a hit."
                );
            }
            else if (!landed && !ctx.HeroLanded)
            {
                ctx.Log(
                    $"{enemy.DisplayName} uses {enemy.CurrentMoveName} ({total} vs {def}): misses."
                );
            }
            else if (landed && !ctx.Scripted)
            {
                ctx.Log(
                    $"{enemy.DisplayName} uses {enemy.CurrentMoveName} ({total} vs {def}): lands."
                );
            }
        }
        else if (ctx.HeroLanded || ctx.ClashTie)
        {
            // Visual mode: a hero win or tie prevents enemy damage.
            landed = false;
            tied = ctx.ClashTie;

            int shown = 1 + fate.MaliceDice;

            int[] fake = EnemyFakeRoll.Make(
                shown,
                false,
                out int fk
            );

            yield return ctx.View.TumbleEnemy(shown);

            ctx.View.ShowEnemyRoll(fake, fk);
        }
        else
        {
            // Visual mode: a hero miss means the enemy lands.
            landed = true;

            int shown = 1 + fate.MaliceDice;

            if (ctx.NeedsEnemyAttackRollForDefense)
            {
                int rolled = DiceService.RollHighestDice(
                    shown,
                    out ctx.EnemyClashDice,
                    out ctx.EnemyClashKeptIndex
                );

                ctx.ResolveEnemyAttackRollDefense(
                    rolled + enemy.AttackModifier
                );

                yield return ctx.View.TumbleEnemy(shown);

                ctx.View.ShowEnemyRoll(
                    ctx.EnemyClashDice,
                    ctx.EnemyClashKeptIndex
                );
            }
            else
            {
                int[] fake = EnemyFakeRoll.Make(
                    shown,
                    true,
                    out int fk
                );

                yield return ctx.View.TumbleEnemy(shown);

                ctx.View.ShowEnemyRoll(fake, fk);
            }
        }

        // Show the result when the hero chose Defend.
        if (isDefendClash && mode == EnemyDiceMode.Real)
        {
            yield return ctx.View.ClashOutcome(
                false,
                landed
            );
        }

        if (!landed)
        {
            if (tied)
            {
                ctx.Log("Neither side deals damage.");
            }
            else if (ctx.HeroLanded)
            {
                ctx.Log(
                    $"{enemy.DisplayName}'s {enemy.CurrentMoveName} is beaten back."
                );
            }

            ctx.Defending = false;
            ctx.RaiseChanged();

            yield return new WaitForSeconds(0.6f);
            yield break;
        }

        int dmg;

        if (ctx.Scripted)
        {
            dmg = Balance.ScriptedLossDamage(
                hero.MaxHp,
                hero.Hp,
                ctx.Game.Config.scriptedLossRounds,
                ctx.Round,
                ctx.Defending
            );

            if (ctx.Defending)
            {
                dmg = Mathf.Max(
                    0,
                    dmg - ctx.CardDefendReduction
                );
            }
        }
        else
        {
            dmg = enemy.RollDamage();

            if (ctx.Defending)
            {
                dmg = (dmg + 1) / 2;

                dmg = Mathf.Max(
                    0,
                    dmg - ctx.CardDefendReduction
                );
            }
        }

        ctx.Defending = false;

        // Preserve statuses and shields.
        dmg = hero.ReceiveHit(dmg);

        ctx.Log(
            $"{enemy.DisplayName} uses {enemy.CurrentMoveName}: {dmg} damage."
        );

        if (!hero.IsDead)
            EnemyMoveRunner.OnLand(ctx);

        if (hero.IsDead)
            fate.AddFate(fate.HeroKODelta);

        ctx.RaiseChanged();

        yield return new WaitForSeconds(0.6f);
    }
}
