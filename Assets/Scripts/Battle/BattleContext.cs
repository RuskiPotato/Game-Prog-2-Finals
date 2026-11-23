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
    public int CardRollModifier { get; private set; }
    public int CardExtraDice { get; private set; }
    public int CardDamageBonus { get; private set; }
    public int CardDefendReduction { get; private set; }
    bool usePlayerAttackRollForDamage;
    bool useEnemyAttackRollForDefense;
    public bool CheatedThisFight, FailedThisFight;
    public SkillData ActiveSkill;                 // the skill the hero committed to this round (null otherwise)
    public SkillCooldowns Cooldowns { get; } = new SkillCooldowns();

    // Clash state for the current round
    public PlayerAction ChosenAction;      // what the hero committed to this round
    public string ChosenCardTitle;
    public bool HeroLanded;                // the hero's attack succeeded: the enemy's attack is cancelled
    // Enemy's real d20 roll is generated before the clash reveal and consumed by EnemyTurnHandler.
    public int[] EnemyClashDice;
    public int EnemyClashKeptIndex = -1;
    public int EnemyClashBest;
    public bool EnemyClashRollPrepared;

    public int RemovedMaliceDice { get; private set; }
    public bool SleightArmed { get; private set; }
    public event Action Changed;

    public FateState Fate => Game.Fate;
    public ProgressState Progress => Game.Progress;
    public bool Scripted => Progress.Phase == GamePhase.FirstEncounter;
    // Hero.AttackModifier already includes the hero's Attack/Accuracy statuses.
    public int RollMod => Hero.AttackModifier + Fate.MoraleModifier + CardRollModifier;
    // Cards/cheats that "add a die" cancel a Malice die instead (an extra Malice-style die could only hurt: keep-lowest).
    public int CurrentDice => Math.Max(1, Fate.TotalDice - RemovedMaliceDice - CardExtraDice);
    // True when this round's committed action rolls to hit (Attack, or an Attack-kind skill).
    public bool ActionRollsToHit => ChosenAction == PlayerAction.Attack
        || (ChosenAction == PlayerAction.Skill && ActiveSkill != null && ActiveSkill.kind == SkillKind.Attack);

    public BattleContext(GameManager game, IBattleView view, EnemyData enemy) {
        Game = game; View = view; Hero = game.Hero; Enemy = new EnemyUnit(enemy);
        Hero.ClearBattleState();                  // the hero outlives fights: no leftover shield or statuses
    }

    public void RaiseChanged() => Changed?.Invoke();
    public void ResetTurnModifiers() {
        RemovedMaliceDice = 0;
        CardRollModifier = CardExtraDice = CardDamageBonus = CardDefendReduction = 0;
        ActiveSkill = null;
        usePlayerAttackRollForDamage = false;
        useEnemyAttackRollForDefense = false;
        SleightArmed = false;
        EnemyClashDice = null;
        EnemyClashKeptIndex = -1;
        EnemyClashBest = 0;
        EnemyClashRollPrepared = false;
    }

    public void AddCardRollModifier(int value) { CardRollModifier += value; Changed?.Invoke(); }
    public void AddCardExtraDice(int value) { CardExtraDice += value; Changed?.Invoke(); }
    public void AddCardDamageBonus(int value) { CardDamageBonus += value; Changed?.Invoke(); }
    public void AddCardDefendReduction(int value) { CardDefendReduction += value; Changed?.Invoke(); }
    public void UsePlayerAttackRollForDamage() { usePlayerAttackRollForDamage = true; }
    public void UseEnemyAttackRollForDefense() { useEnemyAttackRollForDefense = true; }
    public void ResolvePlayerAttackRollDamage(int finalAttackTotal) {
        if (!usePlayerAttackRollForDamage) return;
        CardDamageBonus += Math.Max(0, finalAttackTotal);
        usePlayerAttackRollForDamage = false;
        Changed?.Invoke();
    }
    public bool NeedsEnemyAttackRollForDefense => useEnemyAttackRollForDefense;
    public void ResolveEnemyAttackRollDefense(int finalEnemyAttackTotal) {
        if (!useEnemyAttackRollForDefense) return;
        CardDefendReduction += Math.Max(0, finalEnemyAttackTotal);
        useEnemyAttackRollForDefense = false;
        Changed?.Invoke();
    }
    public void AddCardAudacity(int value) { Fate.Audacity = Math.Min(5, Fate.Audacity + Math.Max(0, value)); Changed?.Invoke(); }

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
