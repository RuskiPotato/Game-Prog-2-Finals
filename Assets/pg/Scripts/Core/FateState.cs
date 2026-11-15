using System;
using UnityEngine;

public enum FateTier { Neutral, Ire, Irritated, Furious, Wrathful }

public class FateState {
    public int Fate, Suspicion, Morale = 100, Audacity = 3, Gold = 200;
    public bool FateLocked;      // true during the tutorial
    public bool SoloMode = true; // solo has no Morale effects
    public event Action Changed;

    // Fate event tunables (v0.3 doc values)
    public int SuccessDelta = 6, LargeSuccessDelta = 12, FumbleDelta = -10, FailDelta = -4,
               EnemyDefeatedDelta = 7, HeroKODelta = -15, DefiantCritDelta = -25;

    // Suspicion decay tunables (applied when an encounter ends)
    public int NoCheatFightDecay = 10;     // fight with no cheating
    public int CleanCheatFightDecay = 5;   // cheated, but never failed a Suspicion check that encounter

    // Bargain with Fate: 2 gold per point of current Fate
    public int BargainGoldPerFate = 2;
    public int BargainCost => Fate * BargainGoldPerFate;

    static readonly int[] MaliceByTier = { 0, 1, 2, 3, 4 };  // Neutral, Ire, Irritated, Furious, Wrathful

    public static FateTier TierOf(int fate) =>
        fate < 20 ? FateTier.Neutral :
        fate <= 45 ? FateTier.Ire :
        fate <= 69 ? FateTier.Irritated :
        fate <= 89 ? FateTier.Furious : FateTier.Wrathful;

    public int MaliceDice => MaliceByTier[(int)TierOf(Fate)];
    public int TotalDice => MaliceDice + 1;
    public string TierName => TierOf(Fate).ToString();

    // Morale (group play only). Wavering is 30-49 so it does not overlap Steady.
    public int MoraleModifier => SoloMode ? 0 : Morale >= 61 ? 1 : Morale >= 50 ? 0 : Morale >= 30 ? -1 : -2;
    public int CheatCostBump => (!SoloMode && Morale < 30) ? 1 : 0;

    public void AddFate(int d) { if (FateLocked) return; Fate = Mathf.Clamp(Fate + d, 0, 100); Changed?.Invoke(); }
    public void AddSuspicion(int d) { Suspicion = Mathf.Clamp(Suspicion + d, 0, 100); Changed?.Invoke(); }
    public void AddMorale(int d) { if (SoloMode) return; Morale = Mathf.Clamp(Morale + d, 0, 100); Changed?.Invoke(); }
    public void Notify() => Changed?.Invoke();

    public void ApplyRollOutcome(bool success, int margin, bool fumble) {
        if (fumble) AddFate(FumbleDelta);
        else if (!success) AddFate(FailDelta);
        else AddFate(margin >= 10 ? LargeSuccessDelta : SuccessDelta);
    }

    // Fair d20 + Cunning/5 vs DC. Fate never touches this roll. Suspicious tier (60+) keeps the lower of two dice.
    public bool SuspicionCheck(int cunning) {
        int dc = 8 + Suspicion / 10;
        int roll = DiceService.D20();
        if (Suspicion >= 60) roll = Mathf.Min(roll, DiceService.D20());
        bool pass = roll + cunning / Balance.CunningDivisor >= dc;
        AddSuspicion(pass ? 3 : 15);
        return pass;
    }

    public void EndEncounter(bool cheated, bool failedACheck) {
        if (!cheated) AddSuspicion(-NoCheatFightDecay);
        else if (!failedACheck) AddSuspicion(-CleanCheatFightDecay);
        // failed at least one check: no decay
    }

    public void SetPhase(GamePhase p) {
        switch (p) {
            case GamePhase.Tutorial:       Fate = 0;  FateLocked = true;  break;
            case GamePhase.FirstEncounter: Fate = 70; FateLocked = false; break;
            case GamePhase.Normal:                    FateLocked = false; break;
        }
        Changed?.Invoke();
    }
}
