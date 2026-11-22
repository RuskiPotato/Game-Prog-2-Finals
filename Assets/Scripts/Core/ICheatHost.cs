// What a cheat is allowed to touch.
public interface ICheatHost {
    FateState Fate { get; }
    HeroUnit Hero { get; }
    int RemovedMaliceDice { get; }
    bool SleightArmed { get; }
    void RemoveMaliceDice(int count);
    void ArmSleightOfHand();
    void Log(string message);
}
