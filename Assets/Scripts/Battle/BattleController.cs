using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleController : MonoBehaviour {
    public BattleHud hud;
    public EnemyDiceMode enemyDice = EnemyDiceMode.Real;
    GameManager gm; BattleContext ctx; BattleInput input; EncounterOutcome outcome;
    readonly Dictionary<Unit, ITurnHandler> handlers = new Dictionary<Unit, ITurnHandler>();
    List<Unit> order; CardDeck cardDeck; readonly List<ICard> skillCards = new List<ICard>();
    readonly RoundFlow round = new RoundFlow();

    void Start() {
        gm = GameManager.Instance;
        if (gm.Fate == null) gm.NewRun();
        if (gm.Progress.Phase == GamePhase.Normal && (gm.Run == null || gm.Run.Current == null)) {
            Debug.LogError("No campaign node is selected. Return to ExplorationMap.");
            gm.LoadScene("ExplorationMap"); return;
        }
        var data = EncounterSelector.Pick(gm);
        if (data == null) { hud.Log("No enemy found. Check GameConfig enemy lists."); return; }
        ctx = new BattleContext(gm, hud, data);
        input = new BattleInput(ctx); outcome = new EncounterOutcome(input);
        var registry = gm.Config.cheats;
        cardDeck = new CardDeck(gm.Config.combatDeck, gm.Config.combatHandSize);
        var coreCards = new List<ICard>();
        foreach (var card in gm.Config.hand) if (card is AttackCardData || card is DefendCardData) coreCards.Add(card);
        var cheatCards = new List<ICard>();
        if (registry != null) foreach (var cheat in registry.cheats) if (cheat != null) cheatCards.Add(cheat);
        foreach (var skill in ctx.Hero.Class.skills) if (skill != null) skillCards.Add(skill);
        hud.Initialize(coreCards, cheatCards); WireInput(); BuildHandlers(registry);
        ctx.Changed += RefreshHud; gm.Fate.Changed += RefreshHud; StartCoroutine(BattleLoop());
    }
    void OnDestroy() { if (gm != null && gm.Fate != null) gm.Fate.Changed -= RefreshHud; }
    void WireInput() {
        hud.CardClicked += card => card.Play(ctx, input); hud.AcceptClicked += input.PressAccept;
        hud.RerollClicked += input.PressReroll; hud.ContinueClicked += input.PressContinue;
        input.CheatRequested += cheat => ctx.PlayCheat(cheat); input.ActionChosen += ctx.RaiseChanged;
    }
    void BuildHandlers(CheatRegistry registry) {
        ICheat reroll = registry != null ? registry.Get("loaded_dice") : null;
        handlers[ctx.Hero] = new PlayerTurnHandler(input, new HeroAttackResolver(input, reroll, enemyDice), cardDeck);
        handlers[ctx.Enemy] = new EnemyTurnHandler(enemyDice);
    }
    void RefreshHud() { if (hud != null && ctx != null) { hud.Refresh(ctx); hud.RefreshCombatHand(cardDeck, gm.Config.hand, skillCards); } }
    List<Unit> BuildOrder() { ctx.Log($"{ctx.Enemy.DisplayName} appears."); return new List<Unit> { ctx.Hero, ctx.Enemy }; }
    IEnumerator BattleLoop() {
        ctx.State = BattleState.Initiative; gm.Fate.Audacity = ctx.Hero.Class.startingAudacity; RefreshHud();
        order = BuildOrder(); hud.SetTurnOrder(order); yield return new WaitForSeconds(0.8f);
        while (true) {
            ctx.Round++; round.Begin(ctx);
            yield return handlers[ctx.Hero].Run(ctx);
            if (ctx.Enemy.IsDead) { yield return outcome.Victory(ctx); yield break; }
            yield return handlers[ctx.Enemy].Run(ctx);
            if (ctx.Hero.IsDead) { yield return outcome.Defeat(ctx); yield break; }
            round.End(ctx); RefreshHud();
        }
    }
}
