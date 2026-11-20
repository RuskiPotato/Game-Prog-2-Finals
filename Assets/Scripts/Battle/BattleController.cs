using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Thin orchestrator: builds the pieces, wires them together, runs the round loop.
public class BattleController : MonoBehaviour {
    public BattleHud hud;
    public EnemyDiceMode enemyDice = EnemyDiceMode.Visual;   // Visual: cosmetic dice. Real: enemy rolls per the GDD

    GameManager gm;
    BattleContext ctx;
    BattleInput input;
    EncounterOutcome outcome;
    readonly Dictionary<Unit, ITurnHandler> handlers = new Dictionary<Unit, ITurnHandler>();
    List<Unit> order;

    void Start() {
        gm = GameManager.Instance;
        if (gm.Fate == null) gm.NewRun();          // pressing Play inside Battle starts a fresh run
        if (gm.Progress.Phase == GamePhase.Normal) EncounterSelector.SkipNonCombatNodes(gm);

        var data = EncounterSelector.Pick(gm);
        if (data == null) {
            hud.Log("No enemy found. Run Paperboard God > Create Default Assets.");
            return;
        }

        ctx = new BattleContext(gm, hud, data);
        input = new BattleInput(ctx);
        outcome = new EncounterOutcome(input);

        var registry = gm.Config.cheats;
        hud.Initialize(gm.Config.hand);

        WireInput();
        BuildHandlers(registry);

        ctx.Changed += RefreshHud;
        gm.Fate.Changed += RefreshHud;             // unsubscribed in OnDestroy (Fate outlives this scene)
        StartCoroutine(BattleLoop());
    }

    void OnDestroy() {
        if (gm != null && gm.Fate != null) gm.Fate.Changed -= RefreshHud;
    }

    void WireInput() {
        hud.CardClicked += card => card.Play(ctx, input);     // each card knows how to play itself
        hud.AcceptClicked += input.PressAccept;
        hud.RerollClicked += input.PressReroll;
        hud.ContinueClicked += input.PressContinue;
        input.CheatRequested += cheat => ctx.PlayCheat(cheat);
        input.ActionChosen += ctx.RaiseChanged;
    }

    void BuildHandlers(CheatRegistry registry) {
        ICheat reroll = registry != null ? registry.Get("loaded_dice") : null;
        var attack = new HeroAttackResolver(input, reroll);
        handlers[ctx.Hero] = new PlayerTurnHandler(input, attack);
        handlers[ctx.Enemy] = new EnemyTurnHandler(enemyDice);
    }

    void RefreshHud() => hud.Refresh(ctx);

    // Clash: the hero commits first and rolls; then the enemy's side of the exchange resolves.
    // Initiative is no longer used for sequencing in a 1v1 clash; the order list only feeds the turn row.
    List<Unit> BuildOrder() {
        ctx.Log($"{ctx.Enemy.DisplayName} appears.");
        return new List<Unit> { ctx.Hero, ctx.Enemy };
    }

    IEnumerator BattleLoop() {
        ctx.State = BattleState.Initiative;
        gm.Fate.Audacity = ctx.Hero.Class.startingAudacity;     // Audacity (shared by the party) resets each encounter
        RefreshHud();
        order = BuildOrder();
        hud.SetTurnOrder(order);
        yield return new WaitForSeconds(0.8f);

        while (true) {
            ctx.Round++;

            yield return handlers[ctx.Hero].Run(ctx);                   // choose + roll
            if (ctx.Enemy.IsDead) { yield return outcome.Victory(ctx); yield break; }

            yield return handlers[ctx.Enemy].Run(ctx);                  // clash resolution
            if (ctx.Hero.IsDead) { yield return outcome.Defeat(ctx); yield break; }

            ctx.State = BattleState.RoundEnd;
            gm.Fate.Audacity = Mathf.Min(5, gm.Fate.Audacity + 1);
            RefreshHud();
        }
    }
}
