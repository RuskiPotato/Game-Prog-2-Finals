using System;

// Shared state for one fight, and the ICheatHost that cheats act on.
public class BattleContext : ICheatHost {
    public GameManager Game { get; }
    public IBattleView View { get; }
    public HeroUnit Hero { get; }
    public EnemyUnit Enemy { get; }

    public BattleState State;
    public int Round;
    public bool CanAct;
    public bool Defending;
    public bool CheatedThisFight, FailedThisFight;

    public int RemovedMaliceDice { get; private set; }
    public bool SleightArmed { get; private set; }
    public event Action Changed;

    public FateState Fate => Game.Fate;
    public ProgressState Progress => Game.Progress;
    public bool Scripted => Progress.Phase == GamePhase.FirstEncounter;
    public int RollMod => Hero.AttackModifier + Fate.MoraleModifier;
    public int CurrentDice => Math.Max(1, Fate.TotalDice - RemovedMaliceDice);

    public BattleContext(GameManager game, IBattleView view, EnemyData enemy) {
        Game = game; View = view; Hero = game.Hero; Enemy = new EnemyUnit(enemy);
    }

    public void RaiseChanged() => Changed?.Invoke();
    public void ResetTurnModifiers() { RemovedMaliceDice = 0; SleightArmed = false; }

    // ICheatHost
    public void RemoveMaliceDice(int count) { RemovedMaliceDice = count; Changed?.Invoke(); }
    public void ArmSleightOfHand() { SleightArmed = true; Changed?.Invoke(); }
    public void Log(string message) => View.Log(message);

    public bool PlayCheat(ICheat cheat) {
        var r = CheatRunner.TryPlay(cheat, this);
        Track(r);
        Changed?.Invoke();
        return r.Played;
    }

    public bool PayCheat(ICheat cheat) {          // cost and Suspicion check only (used for the reroll)
        var r = CheatRunner.TryPay(cheat, this);
        Track(r);
        Changed?.Invoke();
        return r.Played;
    }

    void Track(CheatResult r) {
        if (r.SuspicionChecked) CheatedThisFight = true;
        if (r.Noticed) FailedThisFight = true;
    }
}
