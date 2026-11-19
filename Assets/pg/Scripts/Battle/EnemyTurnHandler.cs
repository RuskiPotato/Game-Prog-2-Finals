using System.Collections;
using UnityEngine;

public class EnemyTurnHandler : ITurnHandler {
    public IEnumerator Run(BattleContext ctx) {
        ctx.State = BattleState.EnemyTurn;
        ctx.RaiseChanged();
        yield return new WaitForSeconds(0.4f);

        var enemy = ctx.Enemy;
        var hero = ctx.Hero;
        var fate = ctx.Fate;

        if (!enemy.Attacks) {
            ctx.Log($"{enemy.DisplayName} just stands there.");
            yield return new WaitForSeconds(0.5f);
            yield break;
        }

        bool hit;
        int dmg = 0;
        if (ctx.Scripted) {
            hit = true;
            dmg = Balance.ScriptedLossDamage(hero.MaxHp, hero.Hp, ctx.Game.Config.scriptedLossRounds, ctx.Round, ctx.Defending);
            ctx.Log($"{enemy.DisplayName} hits you for {dmg}.");
        } else {
            int dice = 1 + fate.MaliceDice;                       // Destiny's favor: extra dice, keep highest
            int total = DiceService.RollHighest(dice) + enemy.AttackModifier;
            int def = hero.DefenseDc;
            hit = total >= def;
            if (hit) {
                dmg = enemy.RollDamage();
                if (ctx.Defending) dmg = (dmg + 1) / 2;
            }
            ctx.Log(hit ? $"{enemy.DisplayName} rolls {total} vs {def}: hits for {dmg}."
                        : $"{enemy.DisplayName} rolls {total} vs {def}: misses.");
        }
        ctx.Defending = false;

        if (hit) {
            hero.TakeDamage(dmg);
            if (hero.IsDead) fate.AddFate(fate.HeroKODelta);
            // Later: Flashback window goes here, before the knockout resolves
        }
        ctx.RaiseChanged();
        yield return new WaitForSeconds(0.5f);
    }
}
