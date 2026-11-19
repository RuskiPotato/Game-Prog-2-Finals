using UnityEngine;

[CreateAssetMenu(menuName = "Paperboard God/Cheats/Loaded Dice", fileName = "LoadedDice")]
public class LoadedDiceCheat : CheatData {
    public int diceRemoved = 2;

    void Reset() {
        id = "loaded_dice"; displayName = "Loaded Dice"; audacityCost = 1; needsSuspicionCheck = true;
        description = "Remove 2 Malice dice before a roll, or cancel a miss and reroll once.";
    }

    public override bool CanUse(ICheatHost host) => host.RemovedMaliceDice == 0;
    public override void Apply(ICheatHost host) => host.RemoveMaliceDice(diceRemoved);
}
