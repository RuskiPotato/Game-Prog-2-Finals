using UnityEngine;

[CreateAssetMenu(menuName = "Paperboard God/Cheats/Bargain with Fate", fileName = "BargainWithFate")]
public class BargainCheat : CheatData {
    void Reset() {
        id = "bargain_fate"; displayName = "Bargain with Fate"; audacityCost = 0; needsSuspicionCheck = false;
        description = "Pay gold to reset Fate to 0.";
    }

    public override bool CanUse(ICheatHost host) => host.Fate.Gold >= host.Fate.BargainCost;

    public override void Apply(ICheatHost host) {
        var f = host.Fate;
        int cost = f.BargainCost;
        f.Gold -= cost;
        f.Fate = 0;
        f.Notify();
        host.Log($"Bargained with Fate for {cost} gold.");
    }

    public override string CostLabel(ICheatHost host) => $"{host.Fate.BargainCost} gold, no check";
}
