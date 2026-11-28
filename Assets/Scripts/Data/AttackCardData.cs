using UnityEngine;

[CreateAssetMenu(menuName = "Paperboard God/Cards/Attack", fileName = "AttackCard")]
public class AttackCardData : CardData
{
    public override string Id => "attack";
    public override string Title => "Attack";
    public override CardKind Kind => CardKind.Action;

    public override bool CanPlay(BattleContext ctx) => ctx.CanAct;

    public override string CostText(BattleContext ctx)
    {
        if (ctx.Scripted)
            return "";

        if (!ctx.Enemy.AttacksThisTurn)
        {
            float chance = Odds.HitChance(
                ctx.Enemy.DefenseDc,
                ctx.RollMod,
                ctx.CurrentDice
            ) * 100f;

            return $"{chance:0.0}% to hit";
        }

        float winChance = Odds.OpposedWinChance(
            ctx.RollMod,
            ctx.CurrentDice,
            ctx.Enemy.AttackModifier,
            1 + ctx.Fate.MaliceDice
        ) * 100f;

        return $"{winChance:0.0}% clash win";
    }

    public override string DetailText(BattleContext ctx)
    {
        if (ctx.Scripted)
            return "Roll the dice and hope.";

        if (!ctx.Enemy.AttacksThisTurn)
        {
            return
                $"d20 {ctx.RollMod:+0;-0} vs DC {ctx.Enemy.DefenseDc}\n" +
                $"{ctx.CurrentDice} dice, keep lowest";
        }

        return
            $"Opposed: d20 {ctx.RollMod:+0;-0} vs Enemy d20 {ctx.Enemy.AttackModifier:+0;-0}\n" +
            $"Tier DC {ctx.Enemy.DefenseDc} (<DC: ½ dmg, +10: Crushing)";
    }

    public override void Play(BattleContext ctx, BattleInput input)
    {
        input.PressAttack();
    }
}