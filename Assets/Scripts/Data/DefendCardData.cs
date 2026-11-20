using UnityEngine;

[CreateAssetMenu(menuName = "Paperboard God/Cards/Defend", fileName = "DefendCard")]
public class DefendCardData : CardData {
    public override string Id => "defend";
    public override string Title => "Defend";
    public override CardKind Kind => CardKind.Action;

    public override bool CanPlay(BattleContext ctx) => ctx.CanAct;
    public override string CostText(BattleContext ctx) => "+1 AUD";
    public override string DetailText(BattleContext ctx) => "Their attack lands for half.\nGain 1 Audacity.";
    public override void Play(BattleContext ctx, BattleInput input) => input.PressDefend();
}
