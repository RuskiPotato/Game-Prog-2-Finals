using UnityEngine;
using UnityEngine.SceneManagement;

// Orchestrates the run and survives scene changes. Rules stay in plain classes (FateState, RunState,
// MapGenerator...) so they stay testable; this only holds them and moves between scenes.
public class GameManager : Singleton<GameManager> {
    // Cheats unlocked by the first encounter (ids match the CheatData assets)
    public static readonly string[] FirstUnlocks = { "loaded_dice", "bargain_fate", "sleight_of_hand" };

    public GameConfig Config { get; private set; }
    public FateState Fate;
    public ProgressState Progress;
    public HeroUnit Hero;
    public RunState Run;
    public bool SoloMode = true;

    // Creates itself before any scene loads, so pressing Play in any scene works.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap() {
        if (Instance != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    protected override void OnSingletonAwake() {
        Config = Resources.Load<GameConfig>("GameConfig");   // Assets/Resources/GameConfig.asset
        if (Config == null)
            Debug.LogError("GameConfig not found. Run Paperboard God > Create Default Assets.");
    }

    // prologue = false skips the tutorial and scripted loss (dev shortcut until MetaState exists)
    public void NewRun(int seed = 0, bool solo = true, bool prologue = true) {
        if (seed == 0) seed = System.Environment.TickCount;
        Rng.Seed(seed);

        SoloMode = solo;
        Progress = new ProgressState();
        Fate = new FateState { SoloMode = solo };

        if (prologue) {
            Fate.SetPhase(Progress.Phase);
        } else {
            Progress.Phase = GamePhase.Normal;
            foreach (var id in FirstUnlocks) Progress.Unlock(id);
            Fate.SetPhase(GamePhase.Normal);
            Fate.Fate = Config.run.startingFate;
        }

        Hero = new HeroUnit(Config.classes.Default);
        Fate.Audacity = Hero.Class.startingAudacity;
        Run = new RunState(seed, MapGenerator.Generate(Config.run), Config.run.savestatesPerRun);
    }

    public void LoadScene(string sceneName) => SceneManager.LoadScene(sceneName);
}
