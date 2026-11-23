using System.Collections;
using UnityEngine;


public class EnemyTurnHandler : ITurnHandler {
    readonly EnemyDiceMode mode;
    public EnemyTurnHandler(EnemyDiceMode mode = EnemyDiceMode.Visual) { this.mode = mode; }

    public IEnumerator Run(BattleContext ctx) {
        ctx.State = BattleState.EnemyTurn;
        ctx.RaiseChanged();
        yield return new WaitForSeconds(0.3f);

        var enemy = ctx.Enemy;
        var hero = ctx.Hero;
        var fate = ctx.Fate;

        // The move was already chosen at round start (RoundFlow.Begin) so the clash card could show it.
        if (!enemy.Attacks) {
            ctx.Log($"{enemy.DisplayName} just stands there.");
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        if (!enemy.AttacksThisTurn) {                         // Defend / Skill move: no clash
            if (enemy.CurrentMoveType == EnemyMoveType.Skill) EnemyMoveRunner.OnSkillPhase(ctx);
            ctx.Defending = false;
            ctx.RaiseChanged();
            yield return new WaitForSeconds(0.6f);
            yield break;
        }

        bool isDefendClash = ctx.ChosenAction == PlayerAction.Defend;
        if (isDefendClash) yield return ctx.View.ClashApproach();

        bool landed;
        if (mode == EnemyDiceMode.Real) {
            // The real enemy roll was generated and revealed alongside the hero's roll.
            // It is still consumed even when the hero wins, so the clash shows both sides.
            if (!ctx.EnemyClashRollPrepared) {
                int n = 1 + fate.MaliceDice;
                ctx.EnemyClashBest = DiceService.RollHighestDice(n,
                    out ctx.EnemyClashDice, out ctx.EnemyClashKeptIndex);
                ctx.EnemyClashRollPrepared = true;
                yield return ctx.View.TumbleEnemy(n);
                ctx.View.ShowEnemyRoll(ctx.EnemyClashDice, ctx.EnemyClashKeptIndex);
            }

            int total = ctx.EnemyClashBest + enemy.AttackModifier;
            ctx.ResolveEnemyAttackRollDefense(total);
            int def = hero.DefenseDc;
            landed = ctx.Scripted || total >= def;
            if (ctx.HeroLanded) landed = false;               // player success cancels enemy attack
            if (!landed && !ctx.HeroLanded) ctx.Log($"{enemy.DisplayName} uses {enemy.CurrentMoveName} ({total} vs {def}): misses.");
            else if (landed && !ctx.Scripted) ctx.Log($"{enemy.DisplayName} uses {enemy.CurrentMoveName} ({total} vs {def}): lands.");
        } else if (ctx.HeroLanded) {
            landed = false;
            int shown = 1 + fate.MaliceDice;
            int[] fake = EnemyFakeRoll.Make(shown, false, out int fk);
            yield return ctx.View.TumbleEnemy(shown);
            ctx.View.ShowEnemyRoll(fake, fk);
        } else {
            landed = true;                                    // visual mode: a hero miss means the enemy lands
            int shown = 1 + fate.MaliceDice;
            if (ctx.NeedsEnemyAttackRollForDefense) {
                int rolled = DiceService.RollHighestDice(shown, out ctx.EnemyClashDice, out ctx.EnemyClashKeptIndex);
                ctx.ResolveEnemyAttackRollDefense(rolled + enemy.AttackModifier);
                yield return ctx.View.TumbleEnemy(shown);
                ctx.View.ShowEnemyRoll(ctx.EnemyClashDice, ctx.EnemyClashKeptIndex);
            } else {
                int[] fake = EnemyFakeRoll.Make(shown, true, out int fk);
                yield return ctx.View.TumbleEnemy(shown);
                ctx.View.ShowEnemyRoll(fake, fk);
            }
        }

        if (isDefendClash && mode == EnemyDiceMode.Real)
            yield return ctx.View.ClashOutcome(false, landed);

        if (!landed) {
            if (ctx.HeroLanded) ctx.Log($"{enemy.DisplayName}'s {enemy.CurrentMoveName} is beaten back.");
            ctx.Defending = false;
            ctx.RaiseChanged();
            yield return new WaitForSeconds(0.6f);
            yield break;
        }

        int dmg;
        if (ctx.Scripted) {
            dmg = Balance.ScriptedLossDamage(hero.MaxHp, hero.Hp, ctx.Game.Config.scriptedLossRounds, ctx.Round, ctx.Defending);
            if (ctx.Defending) dmg = Mathf.Max(0, dmg - ctx.CardDefendReduction);
        } else {
            dmg = enemy.RollDamage();
            if (ctx.Defending) {
                dmg = (dmg + 1) / 2;
                dmg = Mathf.Max(0, dmg - ctx.CardDefendReduction);
            }
        }
        ctx.Defending = false;

        dmg = hero.ReceiveHit(dmg);                          // statuses + shield, returns what got through
        ctx.Log($"{enemy.DisplayName} uses {enemy.CurrentMoveName}: {dmg} damage.");
        if (!hero.IsDead) EnemyMoveRunner.OnLand(ctx);       // on-hit effects (statuses, extra damage, Audacity)
        if (hero.IsDead) fate.AddFate(fate.HeroKODelta);
        // Later: Flashback window goes here, before the knockout resolves
        ctx.RaiseChanged();
        yield return new WaitForSeconds(0.6f);
    }
}
