using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Dice sandbox. Cheat rules come from CheatRunner and the CheatRegistry (same code as the Battle scene);
// this script only owns the sandbox state and its debug buttons.
public class DiceLabController : MonoBehaviour, ICheatHost {
    [Header("Targets")] public int dc = 12;
    public int tutorialDc = 6;
    [Header("Mode")] public bool soloMode = true;
    [Header("Scripted first encounter")] public int scriptedLossRolls = 4;

    [Header("UI")]
    public GameObject fatePanel;        // holds FateBar, TierText, MaliceText
    public GameObject moraleRoot;       // holds MoraleBar
    public Slider fateBar, suspicionBar, moraleBar;      // Min 0, Max 1
    public TMP_Text tierText, maliceText, oddsText, audacityText, goldText, logText;
    public DiceTray diceTray;
    public Button rollBtn, loadedBtn, sleightBtn, bargainBtn, fateUpBtn, fateDownBtn,
                  finishTutorialBtn, forceEndBtn;
    public Button endEncounterBtn;      // optional: applies the per-encounter Suspicion decay

    FateState s;
    ProgressState progress;
    HeroUnit hero;
    CheatRegistry cheats;
    int removedDice, firstRolls;
    bool sleightArmed, rolling, cheatedThisEncounter, failedThisEncounter;

    int CurrentDc => progress.Phase == GamePhase.Tutorial ? tutorialDc : dc;
    int RollMod => hero.AttackModifier + s.MoraleModifier;
    int CurrentDice => Mathf.Max(1, s.TotalDice - removedDice);

    // ---- ICheatHost ----
    public FateState Fate => s;
    public HeroUnit Hero => hero;
    public int RemovedMaliceDice => removedDice;
    public bool SleightArmed => sleightArmed;
    public void RemoveMaliceDice(int count) { removedDice = count; Refresh(); }
    public void ArmSleightOfHand() { sleightArmed = true; Refresh(); }
    public void Log(string m) => logText.text = m;

    void Start() {
        var cfg = GameManager.Instance != null ? GameManager.Instance.Config : null;
        if (cfg == null || cfg.classes == null || cfg.classes.Default == null || cfg.cheats == null) {
            Debug.LogError("DiceLab: GameConfig is missing, or its Classes (with a Default Class) or Cheats slot is empty.");
            enabled = false;
            return;
        }
        cheats = cfg.cheats;
        hero = new HeroUnit(cfg.classes.Default);

        progress = new ProgressState();
        s = new FateState { SoloMode = soloMode };
        s.Audacity = hero.Class.startingAudacity;
        s.Changed += Refresh;

        rollBtn.onClick.AddListener(() => StartCoroutine(DoRoll()));
        loadedBtn.onClick.AddListener(() => PlayCheat("loaded_dice"));
        sleightBtn.onClick.AddListener(() => PlayCheat("sleight_of_hand"));
        bargainBtn.onClick.AddListener(() => PlayCheat("bargain_fate"));
        fateUpBtn.onClick.AddListener(() => s.AddFate(10));
        fateDownBtn.onClick.AddListener(() => s.AddFate(-10));
        finishTutorialBtn.onClick.AddListener(() => {
            progress.Phase = GamePhase.FirstEncounter; ApplyPhase();
            Log("First encounter. Good luck.");
        });
        forceEndBtn.onClick.AddListener(OnFirstEncounterEnd);
        if (endEncounterBtn != null) endEncounterBtn.onClick.AddListener(EndEncounter);

        ApplyPhase();
        Log("Tutorial: hit the dummy.");
    }

    void ApplyPhase() {
        s.SetPhase(progress.Phase);
        fatePanel.SetActive(progress.FateBarRevealed);
        moraleRoot.SetActive(!soloMode && progress.Phase == GamePhase.Normal);
        finishTutorialBtn.gameObject.SetActive(progress.Phase == GamePhase.Tutorial);
        forceEndBtn.gameObject.SetActive(progress.Phase == GamePhase.FirstEncounter);
        Refresh();
    }

    void Refresh() {
        fateBar.value = s.Fate / 100f;
        suspicionBar.value = s.Suspicion / 100f;
        moraleBar.value = s.Morale / 100f;
        tierText.text = s.TierName;
        maliceText.text = $"Malice dice: {s.MaliceDice}  (rolling {CurrentDice})";

        // Odds stay hidden during the scripted first encounter ("rigged but legible" starts at the reveal)
        oddsText.gameObject.SetActive(progress.Phase != GamePhase.FirstEncounter);
        float p = Odds.HitChance(CurrentDc, RollMod, CurrentDice) * 100f;
        oddsText.text = $"Hit chance: {p:0.0}%  (need {CurrentDc - RollMod}+ on die)";

        audacityText.text = $"Audacity: {s.Audacity}";
        goldText.text = $"Gold: {s.Gold}";
        loadedBtn.gameObject.SetActive(progress.IsUnlocked("loaded_dice"));
        sleightBtn.gameObject.SetActive(progress.IsUnlocked("sleight_of_hand"));
        bargainBtn.gameObject.SetActive(progress.IsUnlocked("bargain_fate"));
        fateUpBtn.gameObject.SetActive(progress.Phase != GamePhase.Tutorial);
        fateDownBtn.gameObject.SetActive(progress.Phase != GamePhase.Tutorial);
    }

    // All cheat rules live in CheatRunner and the CheatData assets
    void PlayCheat(string id) {
        if (rolling) return;
        var cheat = cheats.Get(id);
        if (cheat == null) { Log($"Cheat '{id}' is not in the CheatRegistry."); return; }

        var result = CheatRunner.TryPlay(cheat, this);
        if (!result.Played) return;
        if (result.SuspicionChecked) cheatedThisEncounter = true;
        if (result.Noticed) failedThisEncounter = true;
        Refresh();
    }

    RollResult OneRoll(int n, int needed, bool scripted) =>
        scripted ? DiceService.RollForcedMiss(n, needed) : DiceService.RollMalice(n);

    IEnumerator DoRoll() {
        if (rolling) yield break;
        rolling = true;

        int n = CurrentDice;
        int needed = CurrentDc - RollMod;
        bool scripted = progress.Phase == GamePhase.FirstEncounter;

        var result = OneRoll(n, needed, scripted);
        if (sleightArmed) {                      // reroll, keep the better result
            var second = OneRoll(n, needed, scripted);
            if (second.Lowest > result.Lowest) result = second;
        }

        yield return diceTray.Tumble(n);
        diceTray.ShowRoll(result);

        int total = result.Lowest + RollMod;
        bool success = total >= CurrentDc;
        s.ApplyRollOutcome(success, total - CurrentDc, result.Fumble);
        Log(result.Fumble ? $"FUMBLE! ({total} vs DC {CurrentDc})"
          : success ? $"Hit! {total} vs DC {CurrentDc}" : $"Miss. {total} vs DC {CurrentDc}");

        // end-of-round stand-in
        removedDice = 0; sleightArmed = false;
        s.Audacity = Mathf.Min(5, s.Audacity + 1);

        Refresh();
        rolling = false;

        if (scripted && ++firstRolls >= scriptedLossRolls) OnFirstEncounterEnd();
    }

    void EndEncounter() {
        s.EndEncounter(cheatedThisEncounter, failedThisEncounter);
        cheatedThisEncounter = failedThisEncounter = false;
        Log("Encounter over. Suspicion decay applied.");
    }

    void OnFirstEncounterEnd() {
        if (progress.Phase != GamePhase.FirstEncounter) return;
        Log("Party wipe. Destiny: \"Is that really the best you can do?\"");
        progress.Phase = GamePhase.Normal;
        foreach (var id in GameManager.FirstUnlocks) progress.Unlock(id);
        ApplyPhase();
    }
}
