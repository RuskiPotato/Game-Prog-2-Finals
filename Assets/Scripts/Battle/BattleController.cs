using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Thin orchestrator: builds the pieces, wires them together, runs the round loop.
public class BattleController : MonoBehaviour {
    public BattleHud hud;
    public EnemyDiceMode enemyDice = EnemyDiceMode.Real;  

    GameManager gm;
    BattleContext ctx;
    BattleInput input;
    EncounterOutcome outcome;
    readonly Dictionary<Unit, ITurnHandler> handlers = new Dictionary<Unit, ITurnHandler>();
    List<Unit> order;
    CardDeck cardDeck;
    readonly List<ICard> skillCards = new List<ICard>();
    readonly RoundFlow round = new RoundFlow();

    void Start() {
        gm = GameManager.Instance;
        if (gm.Fate == null) gm.NewRun();          
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
        cardDeck = new CardDeck(gm.Config.combatDeck, gm.Config.combatHandSize);
        var coreCards = new List<ICard>();
        foreach (var card in gm.Config.hand)
            if (card is AttackCardData || card is DefendCardData) coreCards.Add(card);
        var cheatCards = new List<ICard>();
        if (registry != null) foreach (var cheat in registry.cheats) if (cheat != null) cheatCards.Add(cheat);
        foreach (var skill in ctx.Hero.Class.skills) if (skill != null) skillCards.Add(skill);   // hero skills sit in the hand
        hud.Initialize(coreCards, cheatCards);

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
        var attack = new HeroAttackResolver(input, reroll, enemyDice);
        handlers[ctx.Hero] = new PlayerTurnHandler(input, attack, cardDeck);
        handlers[ctx.Enemy] = new EnemyTurnHandler(enemyDice);
    }

    void RefreshHud() {
        if (hud != null && ctx != null) {
            hud.Refresh(ctx);
            hud.RefreshCombatHand(cardDeck, gm.Config.hand, skillCards);
        }
    }

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
            round.Begin(ctx);                                           // enemy picks its move (weighted), Defend moves resolve

            yield return handlers[ctx.Hero].Run(ctx);                   // choose + roll
            if (ctx.Enemy.IsDead) { yield return outcome.Victory(ctx); yield break; }

            yield return handlers[ctx.Enemy].Run(ctx);                  // clash resolution
            if (ctx.Hero.IsDead) { yield return outcome.Defeat(ctx); yield break; }

            round.End(ctx);                                             // cooldowns, regen, status ticks, +1 Audacity
            RefreshHud();
        }
    }
}
