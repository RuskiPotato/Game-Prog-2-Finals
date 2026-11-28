using System.Collections;
using UnityEngine;

public class EncounterOutcome {
    readonly BattleInput input;
    public EncounterOutcome(BattleInput input) { this.input = input; }
    IEnumerator WaitContinue(BattleContext ctx, string message) {
        ctx.Log(message); input.ContinuePressed = false; ctx.View.ShowContinue(true);
        yield return new WaitUntil(() => input.ContinuePressed); ctx.View.ShowContinue(false);
    }

    public IEnumerator Victory(BattleContext ctx) {
        var gm = ctx.Game; var fate = ctx.Fate;
        ctx.State = BattleState.Victory; ctx.RaiseChanged(); fate.EndEncounter(ctx.CheatedThisFight, ctx.FailedThisFight);
        if (ctx.Progress.Phase == GamePhase.Tutorial) {
            yield return WaitContinue(ctx, "Dummy defeated. Your training is complete.");
            ctx.Progress.Phase = GamePhase.FirstEncounter; fate.SetPhase(GamePhase.FirstEncounter);
            gm.LoadScene("Battle"); yield break;
        }
        if (ctx.Scripted) { yield return Defeat(ctx); yield break; }

        int reward = ctx.Enemy.Data.goldReward;
        fate.Gold += reward;
        var run = gm.Run; int oldAct = run.Current.act; bool wasBoss = run.Current.type == NodeType.Boss;
        run.CompleteCurrent();
        string extra = "";
        if (wasBoss && !run.Finished) {
            int newAct = run.CurrentAct;
            gm.EnterAct(newAct);
            extra = $" Act {newAct + 1} begins. ";
        }
        yield return WaitContinue(ctx, $"{ctx.Enemy.DisplayName} defeated. +{reward} Gold.{extra}");
        if (run.Finished) {
            yield return WaitContinue(ctx, "Campaign complete! You have reached the end of Destiny's table.");
            gm.LoadScene("MainMenu");
        } else gm.LoadScene("Campaign");
    }

    public IEnumerator Defeat(BattleContext ctx) {
        var gm = ctx.Game; var fate = ctx.Fate;
        ctx.State = BattleState.Defeat; ctx.RaiseChanged();
        if (ctx.Scripted) {
            ctx.View.SetGm("Destiny: \"Is that really the best you can do?\"");
            yield return WaitContinue(ctx, "Table Wipe.");
            gm.BeginCampaignAfterPrologue(); gm.Hero.FullHeal(); ctx.RaiseChanged();
            yield return WaitContinue(ctx, "The Fate bar appears. Destiny is rigging every roll. Time to cheat back.");
            gm.LoadScene("Campaign");
        } else {
            yield return WaitContinue(ctx, "Table Wipe. This run has ended.");
            gm.LoadScene("MainMenu");
        }
    }
}
