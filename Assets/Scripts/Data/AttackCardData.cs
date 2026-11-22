using UnityEngine;

[CreateAssetMenu(menuName = "Paperboard God/Cards/Attack", fileName = "AttackCard")]
public class AttackCardData : CardData {
    public override string Id => "attack";
    public override string Title => "Attack";
    public override CardKind Kind => CardKind.Action;

    public override bool CanPlay(BattleContext ctx) => ctx.CanAct;

    public override string CostText(BattleContext ctx) {
        if (ctx.Scripted) return "";                                 
        float p = Odds.HitChance(ctx.Enemy.DefenseDc, ctx.RollMod, ctx.CurrentDice) * 100f;
        return $"{p:0.0}% to hit";
    }

    public override string DetailText(BattleContext ctx) =>
        ctx.Scripted ? "Roll the dice and hope."
                     : $"d20 {ctx.RollMod:+0;-0} vs DC {ctx.Enemy.DefenseDc}\n{ctx.CurrentDice} dice, keep lowest\nMiss: their attack lands";

    public override void Play(BattleContext ctx, BattleInput input) => input.PressAttack();
}
