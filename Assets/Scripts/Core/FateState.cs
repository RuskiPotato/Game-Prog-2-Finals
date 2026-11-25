
using System;
using UnityEngine;

public enum FateTier
{
    Neutral,
    Ire,
    Irritated,
    Furious,
    Wrathful
}

public class FateState
{
    public int Fate, Suspicion, Morale = 100, Audacity = 3, Gold = 200;
    public bool FateLocked;
    public bool SoloMode = true;

    public event Action Changed;

    // Fate event tunables
    public int SuccessDelta = 3;
    public int LargeSuccessDelta = 6;
    public int FumbleDelta = -6;
    public int FailDelta = -3;
    public int EnemyDefeatedDelta = 4;
    public int HeroKODelta = -10;
    public int DefiantCritDelta = -15;

    // Suspicion decay tunables
    public int NoCheatFightDecay = 10;
    public int CleanCheatFightDecay = 5;

    // Bargain with Fate
    public int BargainGoldPerFate = 2;
    public int BargainCost => Fate * BargainGoldPerFate;

    static readonly int[] MaliceByTier = { 0, 1, 2, 3, 4 };

    public static FateTier TierOf(int fate) =>
        fate < 20 ? FateTier.Neutral :
        fate <= 45 ? FateTier.Ire :
        fate <= 69 ? FateTier.Irritated :
        fate <= 89 ? FateTier.Furious :
        FateTier.Wrathful;

    public int MaliceDice => MaliceByTier[(int)TierOf(Fate)];
    public int TotalDice => MaliceDice + 1;
    public string TierName => TierOf(Fate).ToString();

    // Morale
    public int MoraleModifier =>
        SoloMode ? 0 :
        Morale >= 61 ? 1 :
        Morale >= 50 ? 0 :
        Morale >= 30 ? -1 : -2;

    public int CheatCostBump =>
        (!SoloMode && Morale < 30) ? 1 : 0;

    public void AddFate(int d)
    {
        if (FateLocked) return;

        Fate = Mathf.Clamp(Fate + d, 0, 100);
        Changed?.Invoke();
    }

    public void AddSuspicion(int d)
    {
        Suspicion = Mathf.Clamp(Suspicion + d, 0, 100);
        Changed?.Invoke();
    }

    public void AddMorale(int d)
    {
        if (SoloMode) return;

        Morale = Mathf.Clamp(Morale + d, 0, 100);
        Changed?.Invoke();
    }

    public void Notify() => Changed?.Invoke();

    // Original roll outcome method. Preserved for existing systems.
    public void ApplyRollOutcome(bool success, int margin, bool fumble)
    {
        if (fumble)
            AddFate(FumbleDelta);
        else if (!success)
            AddFate(FailDelta);
        else
            AddFate(margin >= 10 ? LargeSuccessDelta : SuccessDelta);
    }

    // New roll-tier outcome method.
    public void ApplyRollOutcome(RollBand band)
    {
        switch (band)
        {
            case RollBand.Miss:
                AddFate(FailDelta);
                break;

            case RollBand.Glancing:
            case RollBand.Standard:
                AddFate(SuccessDelta);
                break;

            case RollBand.Crushing:
                AddFate(LargeSuccessDelta);
                break;
        }
    }

    // Suspicion check
    public bool SuspicionCheck(int cunning)
    {
        int dc = 8 + Suspicion / 10;
        int roll = DiceService.D20();

        if (Suspicion >= 60)
            roll = Mathf.Min(roll, DiceService.D20());

        bool pass =
            roll + cunning / Balance.CunningDivisor >= dc;

        AddSuspicion(pass ? 3 : 15);

        return pass;
    }

    public void EndEncounter(bool cheated, bool failedACheck)
    {
        if (!cheated)
            AddSuspicion(-NoCheatFightDecay);
        else if (!failedACheck)
            AddSuspicion(-CleanCheatFightDecay);
    }

    public void SetPhase(GamePhase p)
    {
        switch (p)
        {
            case GamePhase.Tutorial:
                Fate = 0;
                FateLocked = true;
                break;

            case GamePhase.FirstEncounter:
                Fate = 70;
                FateLocked = false;
                break;

            case GamePhase.Normal:
                FateLocked = false;
                break;
        }

        Changed?.Invoke();
    }
}
