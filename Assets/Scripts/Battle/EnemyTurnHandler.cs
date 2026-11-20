using System.Collections;
using UnityEngine;

// The enemy's half of the clash. The hero has already committed (and rolled, if attacking).
//   Hero's attack succeeded  -> the enemy's attack is cancelled.
//   Hero missed or defended  -> the enemy's attack lands automatically (halved if the hero defended).
// EnemyDiceMode.Visual: enemy dice are cosmetic (outcome decided first). EnemyDiceMode.Real: the enemy rolls for real per the GDD.
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

        if (!enemy.Attacks) {
            ctx.Log($"{enemy.DisplayName} just stands there.");
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        bool landed;
        if (ctx.HeroLanded) {
            landed = false;                                   // hero won the clash: nothing to roll
            if (mode == EnemyDiceMode.Real) ctx.View.ShowEnemyRoll(new int[0], -1);
            else {
                int shown = 1 + fate.MaliceDice;
                int[] fake = EnemyFakeRoll.Make(shown, false, out int fk);
                yield return ctx.View.TumbleEnemy(shown);
                ctx.View.ShowEnemyRoll(fake, fk);
            }
        } else if (mode == EnemyDiceMode.Real) {
            // GDD: Destiny favors the enemy. 1 + Malice dice, keep the HIGHEST, add the attack mod, beat the hero's Defense DC.
            // The scripted fight is still a guaranteed hit.
            int n = 1 + fate.MaliceDice;
            int best = DiceService.RollHighestDice(n, out int[] dice, out int bestIndex);
            yield return ctx.View.TumbleEnemy(n);
            ctx.View.ShowEnemyRoll(dice, bestIndex);
            int total = best + enemy.AttackModifier;
            int def = hero.DefenseDc;
            landed = ctx.Scripted || total >= def;
            if (!landed) ctx.Log($"{enemy.DisplayName} rolls {total} vs {def}: misses.");
            else if (!ctx.Scripted) ctx.Log($"{enemy.DisplayName} rolls {total} vs {def}: lands.");
        } else {
            landed = true;                                    // visual mode: a hero miss means the enemy lands
            int shown = 1 + fate.MaliceDice;
            int[] fake = EnemyFakeRoll.Make(shown, true, out int fk);
            yield return ctx.View.TumbleEnemy(shown);
            ctx.View.ShowEnemyRoll(fake, fk);
        }

        if (!landed) {
            if (ctx.HeroLanded) ctx.Log($"{enemy.DisplayName}'s attack is beaten back.");
            ctx.Defending = false;
            ctx.RaiseChanged();
            yield return new WaitForSeconds(0.6f);
            yield break;
        }

        int dmg;
        if (ctx.Scripted) {
            dmg = Balance.ScriptedLossDamage(hero.MaxHp, hero.Hp, ctx.Game.Config.scriptedLossRounds, ctx.Round, ctx.Defending);
            ctx.Log($"{enemy.DisplayName} hits you for {dmg}.");
        } else {
            dmg = enemy.RollDamage();
            if (ctx.Defending) dmg = (dmg + 1) / 2;
            ctx.Log($"{enemy.DisplayName} lands the clash: {dmg} damage.");
        }
        ctx.Defending = false;

        hero.TakeDamage(dmg);
        if (hero.IsDead) fate.AddFate(fate.HeroKODelta);
        // Later: Flashback window goes here, before the knockout resolves
        ctx.RaiseChanged();
        yield return new WaitForSeconds(0.6f);
    }
}
