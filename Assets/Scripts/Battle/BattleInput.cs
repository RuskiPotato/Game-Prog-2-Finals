using System;

// Turns button presses into state the turn handlers can wait on.
public class BattleInput {
    readonly BattleContext ctx;

    public PlayerAction ChosenAction;
    public Reaction ChosenReaction;
    public bool AcceptingReaction;
    public bool ContinuePressed;

    public event Action<CheatData> CheatRequested;
    public event Action ActionChosen;

    public BattleInput(BattleContext ctx) { this.ctx = ctx; }

    public void PressAttack() => Choose(PlayerAction.Attack);
    public void PressDefend() => Choose(PlayerAction.Defend);

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
