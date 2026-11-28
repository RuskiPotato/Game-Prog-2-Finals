using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager> {
    public static readonly string[] FirstUnlocks = { "loaded_dice", "bargain_fate", "sleight_of_hand" };
    public static readonly string[] ActTwoUnlocks = { "read_your_fate", "gaslight", "flashback", "reset_encounter", "bribery" };

    public GameConfig Config { get; private set; }
    public FateState Fate;
    public ProgressState Progress;
    public HeroUnit Hero;
    public RunState Run;
    public bool SoloMode = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap() {
        if (Instance != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    protected override void OnSingletonAwake() {
        Config = Resources.Load<GameConfig>("GameConfig");
        if (Config == null) Debug.LogError("GameConfig not found. Run Paperboard God > Create Default Assets.");
    }

    public void NewRun(int seed = 0, bool solo = true, bool prologue = true) {
        if (seed == 0) seed = System.Environment.TickCount;
        Rng.Seed(seed);
        SoloMode = solo;
        Progress = new ProgressState();
        Fate = new FateState { SoloMode = solo };
        if (prologue) Fate.SetPhase(Progress.Phase);
        else {
            Progress.Phase = GamePhase.Normal;
            foreach (var id in FirstUnlocks) Progress.Unlock(id);
            Fate.SetPhase(GamePhase.Normal);
            Fate.Fate = Config.run.startingFate;
        }
        Hero = new HeroUnit(Config.classes.Default);
        Fate.Audacity = Hero.Class.startingAudacity;
        Run = new RunState(seed, MapGenerator.Generate(Config.run), Config.run.savestatesPerRun);
    }

    public void BeginCampaignAfterPrologue() {
        Progress.Phase = GamePhase.Normal;
        foreach (var id in FirstUnlocks) Progress.Unlock(id);
        Fate.SetPhase(GamePhase.Normal);
        Fate.Fate = Config.run.startingFate;
        Fate.Notify();
    }

    public void EnterAct(int act) {
        if (act == 1) foreach (var id in ActTwoUnlocks) Progress.Unlock(id);
        Fate.Suspicion = 0;
        Fate.Notify();
    }

    public void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);
}
