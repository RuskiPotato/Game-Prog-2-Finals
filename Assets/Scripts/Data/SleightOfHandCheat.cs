using UnityEngine;

[CreateAssetMenu(menuName = "Paperboard God/Cheats/Sleight of Hand", fileName = "SleightOfHand")]
public class SleightOfHandCheat : CheatData {
    void Reset() {
        id = "sleight_of_hand"; displayName = "Sleight of Hand"; audacityCost = 2; needsSuspicionCheck = true;
        description = "Roll twice and keep the better result.";
    }

    public override bool CanUse(ICheatHost host) => !host.SleightArmed;
    public override void Apply(ICheatHost host) => host.ArmSleightOfHand();
}
