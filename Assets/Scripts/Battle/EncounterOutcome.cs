using System.Collections;
using UnityEngine;

// What happens when a fight ends: prologue steps, the reveal, run progression, run end.
public class EncounterOutcome {
    readonly BattleInput input;

    public EncounterOutcome(BattleInput input) { this.input = input; }

    IEnumerator WaitContinue(BattleContext ctx, string message) {
        ctx.Log(message);
        input.ContinuePressed = false;
        ctx.View.ShowContinue(true);
        yield return new WaitUntil(() => input.ContinuePressed);
        ctx.View.ShowContinue(false);
    }

    public IEnumerator Victory(BattleContext ctx) {
        var gm = ctx.Game;
        var fate = ctx.Fate;
        ctx.State = BattleState.Victory;
        ctx.RaiseChanged();
        fate.EndEncounter(ctx.CheatedThisFight, ctx.FailedThisFight);

        if (ctx.Progress.Phase == GamePhase.Tutorial) {
            yield return WaitContinue(ctx, "Dummy defeated.");
            ctx.Progress.Phase = GamePhase.FirstEncounter;
            fate.SetPhase(GamePhase.FirstEncounter);        
            gm.LoadScene("Battle");
            yield break;
        }
        if (ctx.Scripted) { yield return Defeat(ctx); yield break; }   // should be impossible

        int reward = ctx.Enemy.Data.goldReward;
        fate.Gold += reward;
        var run = gm.Run;
        int act = run.Current.act;
        run.CompleteCurrent();
        string extra = "";
        if (!run.Finished && run.Current.act != act) {
            fate.Suspicion = 0;
            fate.Notify();
            extra = " New act: Suspicion resets.";
        }
        yield return WaitContinue(ctx, $"{ctx.Enemy.DisplayName} defeated. +{reward} gold.{extra}");

        if (run.Finished) {
            yield return WaitContinue(ctx, "Run complete! Destiny's table is yours.");
            gm.LoadScene("MainMenu");
        } else {
            gm.LoadScene("Battle");
        }
    }

    public IEnumerator Defeat(BattleContext ctx) {
        var gm = ctx.Game;
        var fate = ctx.Fate;
        ctx.State = BattleState.Defeat;
        ctx.RaiseChanged();

        if (ctx.Scripted) {
            ctx.View.SetGm("Destiny: \"Is that really the best you can do?\"");
            yield return WaitContinue(ctx, "Table Wipe.");

            ctx.Progress.Phase = GamePhase.Normal;            // reveal, new cheats
            foreach (var id in GameManager.FirstUnlocks) ctx.Progress.Unlock(id);
            fate.SetPhase(GamePhase.Normal);
            fate.Fate = Mathf.Clamp(gm.Config.run.startingFate, 0, 100);
            fate.Notify();
            ctx.Hero.FullHeal();
            ctx.RaiseChanged();
            yield return WaitContinue(ctx, "The Fate bar appears. Destiny is rigging every roll. Time to cheat back. "); 

            gm.LoadScene("Battle");
        } else {
            yield return WaitContinue(ctx, "Table Wipe. The run is over.");
            gm.LoadScene("MainMenu");
        }
    }
}
