// What a cheat is allowed to touch. The battle (and tests) implement this, so cheats never depend on a scene.
public interface ICheatHost {
    FateState Fate { get; }
    HeroUnit Hero { get; }
    int RemovedMaliceDice { get; }
    bool SleightArmed { get; }
    void RemoveMaliceDice(int count);
    void ArmSleightOfHand();
    void Log(string message);
}
