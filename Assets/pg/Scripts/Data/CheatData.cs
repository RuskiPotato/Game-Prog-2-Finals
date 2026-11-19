using UnityEngine;

// Abstract cheat card. Each cheat overrides Apply (and sometimes CanUse). Also an ICheat for CheatRunner.
public abstract class CheatData : CardData, ICheat {
    public string id;
    public string displayName;
    [TextArea] public string description;
    public int audacityCost = 1;
    public bool needsSuspicionCheck = true;

    // ICard (via CardData) and ICheat share these
    public override string Id => id;
    public string DisplayName => displayName;
    public int AudacityCost => audacityCost;
    public bool NeedsSuspicionCheck => needsSuspicionCheck;

    public override string Title => displayName;
    public override CardKind Kind => CardKind.Cheat;

    public virtual bool CanUse(ICheatHost host) => true;
    public abstract void Apply(ICheatHost host);

    public virtual string CostLabel(ICheatHost host) {
        int cost = CheatRunner.AudacityCost(this, host);
        int dc = 8 + host.Fate.Suspicion / 10;
        return needsSuspicionCheck ? $"{cost} AUD, Susp DC {dc}" : $"{cost} AUD";
    }

    // ICard
    public override bool IsAvailable(BattleContext ctx) => ctx.Progress.IsUnlocked(id);
    public override bool CanPlay(BattleContext ctx) => ctx.CanAct && CanUse(ctx);
    public override string CostText(BattleContext ctx) => CostLabel(ctx);
    public override string DetailText(BattleContext ctx) => description;
    public override void Play(BattleContext ctx, BattleInput input) => input.PressCheat(this);
}
