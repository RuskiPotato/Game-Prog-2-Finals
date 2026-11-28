using System.Collections;
using UnityEngine;

public class PlayerTurnHandler : ITurnHandler {
    readonly BattleInput input;
    readonly HeroAttackResolver attack;
    readonly CardDeck deck;

    public PlayerTurnHandler(BattleInput input, HeroAttackResolver attack, CardDeck deck) {
        this.input = input;
        this.attack = attack;
        this.deck = deck;
    }

    // Art of the permanent Attack / Defend card (the ones listed in GameConfig.hand).
    static Sprite CoreCardArt(BattleContext ctx, PlayerAction action) {
        var hand = ctx.Game != null && ctx.Game.Config != null ? ctx.Game.Config.hand : null;
        if (hand == null) return null;
        foreach (var card in hand) {
            if (card == null) continue;
            if (action == PlayerAction.Attack && card is AttackCardData) return card.Art;
            if (action == PlayerAction.Defend && card is DefendCardData) return card.Art;
        }
        return null;
    }

    public IEnumerator Run(BattleContext ctx) {
        ctx.State = BattleState.HeroTurn;
        ctx.ResetTurnModifiers();
        input.ChosenAction = PlayerAction.None;
        input.ChosenCombatCard = null;
        input.ChosenSkill = null;
        if (deck != null) deck.DrawToHand();
        ctx.ChosenAction = PlayerAction.None;
        ctx.ChosenCardTitle = null;
        ctx.ChosenCardArt = null;
        ctx.HeroLanded = false;
        ctx.CanAct = true;
        ctx.Log("Your turn.");
        ctx.RaiseChanged();

        yield return new WaitUntil(() => input.ChosenAction != PlayerAction.None);

        ctx.ChosenAction = input.ChosenAction;
        var skill = input.ChosenSkill;
        ctx.ActiveSkill = skill;
        ctx.ChosenCardTitle = skill != null ? skill.Title
            : input.ChosenCombatCard != null ? input.ChosenCombatCard.Title : input.ChosenAction.ToString();
        ctx.ChosenCardArt = skill != null ? skill.Art
            : input.ChosenCombatCard != null ? input.ChosenCombatCard.Art : CoreCardArt(ctx, input.ChosenAction);
        if (input.ChosenCombatCard != null) input.ChosenCombatCard.Data.Apply(ctx);
        if (skill != null) {
            ctx.Cooldowns.Start(skill);
            if (skill.kind == SkillKind.Attack) ctx.AddCardRollModifier(skill.attackRollModifier);
        }
        ctx.RaiseChanged();

        if (ctx.ActionRollsToHit) {
            yield return attack.Run(ctx);
        } else if (skill != null) {
            ctx.Log($"You use {skill.Title}.");
            SkillEffectRunner.Apply(ctx, skill);        // Support skill: no roll, the enemy's attack still comes
            yield return new WaitForSeconds(0.5f);
        } else {
            ctx.Defending = true;
            ctx.Fate.Audacity = Mathf.Min(5, ctx.Fate.Audacity + 1);
            ctx.Log("You brace. +1 Audacity. The enemy's attack will land for half.");
            ctx.RaiseChanged();
            yield return new WaitForSeconds(0.5f);
        }

        if (input.ChosenCombatCard != null && deck != null) {
            if (input.ChosenCombatCard.Data.exhaustOnPlay) deck.Exhaust(input.ChosenCombatCard);
            else deck.Play(input.ChosenCombatCard);
        }
        ctx.RaiseChanged();
    }
}