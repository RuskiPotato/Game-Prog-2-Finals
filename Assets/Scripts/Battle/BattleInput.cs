using System;

// Turns button presses into state the turn handlers can wait on.
public class BattleInput {
    readonly BattleContext ctx;

    public PlayerAction ChosenAction;
    public CombatCardInstance ChosenCombatCard;
    public SkillData ChosenSkill;
    public Reaction ChosenReaction;
    public bool AcceptingReaction;
    public bool ContinuePressed;

    public event Action<CheatData> CheatRequested;
    public event Action ActionChosen;

    public BattleInput(BattleContext ctx) { this.ctx = ctx; }

    public void PressAttack() => Choose(PlayerAction.Attack);
    public void PressDefend() => Choose(PlayerAction.Defend);
    public void PressSkill(SkillData skill) {
        if (skill == null || !ctx.CanAct || ChosenAction != PlayerAction.None || !ctx.Cooldowns.IsReady(skill)) return;
        ChosenSkill = skill;
        Choose(PlayerAction.Skill);
    }
    public void PressCombatCard(CombatCardData card) {
        if (card != null) PressCombatCard(new CombatCardInstance(card));
    }
    public void PressCombatCard(CombatCardInstance card) {
        if (card == null || !ctx.CanAct || ChosenAction != PlayerAction.None) return;
        ChosenCombatCard = card;
        Choose(card.Data.action == CombatCardAction.Attack ? PlayerAction.Attack : PlayerAction.Defend);
    }

    void Choose(PlayerAction a) {
        if (!ctx.CanAct || ChosenAction != PlayerAction.None) return;
        ChosenAction = a;
        ctx.CanAct = false;
        ActionChosen?.Invoke();
    }

    public void PressCheat(CheatData cheat) {
        if (!ctx.CanAct || ChosenAction != PlayerAction.None) return;
        CheatRequested?.Invoke(cheat);
    }

    public void PressAccept() { if (AcceptingReaction) ChosenReaction = Reaction.Accept; }
    public void PressReroll() { if (AcceptingReaction) ChosenReaction = Reaction.Reroll; }
    public void PressContinue() { ContinuePressed = true; }
}
