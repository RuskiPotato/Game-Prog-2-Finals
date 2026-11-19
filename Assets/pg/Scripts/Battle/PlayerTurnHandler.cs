using System.Collections;
using UnityEngine;

public class PlayerTurnHandler : ITurnHandler {
    readonly BattleInput input;
    readonly HeroAttackResolver attack;

    public PlayerTurnHandler(BattleInput input, HeroAttackResolver attack) {
        this.input = input;
        this.attack = attack;
    }

    public IEnumerator Run(BattleContext ctx) {
        ctx.State = BattleState.HeroTurn;
        ctx.ResetTurnModifiers();
        input.ChosenAction = PlayerAction.None;
        ctx.CanAct = true;
        ctx.Log("Your turn.");
        ctx.RaiseChanged();

        yield return new WaitUntil(() => input.ChosenAction != PlayerAction.None);

        if (input.ChosenAction == PlayerAction.Attack) {
            yield return attack.Run(ctx);
        } else {
            ctx.Defending = true;
            ctx.Fate.Audacity = Mathf.Min(5, ctx.Fate.Audacity + 1);
            ctx.Log("You brace. +1 Audacity.");
            ctx.RaiseChanged();
            yield return new WaitForSeconds(0.5f);
        }
    }
}
