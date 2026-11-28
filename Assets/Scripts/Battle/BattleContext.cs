using System;

// Shared state for one fight, and the ICheatHost that cheats act on.
public class BattleContext : ICheatHost
{
    public GameManager Game { get; }
    public IBattleView View { get; }
    public HeroUnit Hero { get; }
    public EnemyUnit Enemy { get; }

    public BattleState State;
    public int Round;
    public bool CanAct;
    public bool Defending;

    public int CardRollModifier { get; private set; }
    public int CardBonusDice { get; private set; }
    public int CardDamageBonus { get; private set; }
    public int CardDefendReduction { get; private set; }

    bool usePlayerAttackRollForDamage;
    bool useEnemyAttackRollForDefense;

    public bool CheatedThisFight, FailedThisFight;

    // Existing skill system — preserved.
    public SkillData ActiveSkill;
    public SkillCooldowns Cooldowns { get; } = new SkillCooldowns();

    // Current action.
    public PlayerAction ChosenAction;
    public string ChosenCardTitle;
    public UnityEngine.Sprite ChosenCardArt;     // art of the card/skill the hero committed this turn

    // Opposed clash state.
    public bool HeroLanded;
    public bool ClashTie;
    public int HeroClashTotal;
    public int EnemyClashTotal;

    // Enemy's real roll, prepared before the clash reveal.
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

    // Hero.AttackModifier already includes attack/accuracy statuses.
    public int RollMod =>
        Hero.AttackModifier + Fate.MoraleModifier + CardRollModifier;

    // Preserve the original die calculation from your project.
    public int CurrentDice =>
        Math.Max(1, Fate.TotalDice - RemovedMaliceDice);

    // Fortune is a free counter-roll while Destiny has at least one Malice die.
    // It is disabled during the deliberately scripted first encounter.
    public bool HasFortuneDie => Fate.MaliceDice > 0 && !Scripted;

    public bool ActionRollsToHit =>
        ChosenAction == PlayerAction.Attack ||
        (ChosenAction == PlayerAction.Skill &&
         ActiveSkill != null &&
         ActiveSkill.kind == SkillKind.Attack);

    public BattleContext(GameManager game, IBattleView view, EnemyData enemy)
    {
        Game = game;
        View = view;
        Hero = game.Hero;
        Enemy = new EnemyUnit(enemy);

        // Preserve the original battle-state cleanup.
        Hero.ClearBattleState();
    }

    public void RaiseChanged() => Changed?.Invoke();

    public void ResetTurnModifiers()
    {
        RemovedMaliceDice = 0;

        CardRollModifier = 0;
        CardBonusDice = 0;
        CardDamageBonus = 0;
        CardDefendReduction = 0;

        ActiveSkill = null;

        usePlayerAttackRollForDamage = false;
        useEnemyAttackRollForDefense = false;

        SleightArmed = false;

        HeroLanded = false;
        ClashTie = false;
        HeroClashTotal = 0;
        EnemyClashTotal = 0;

        EnemyClashDice = null;
        EnemyClashKeptIndex = -1;
        EnemyClashBest = 0;
        EnemyClashRollPrepared = false;
    }

    public void AddCardRollModifier(int value)
    {
        CardRollModifier += value;
        Changed?.Invoke();
    }

    // Bonus card dice are rolled separately from the Malice pool.
    // The Malice pool keeps its lowest die; the bonus pool can replace it
    // only when a bonus die rolls higher.
    public void AddCardBonusDice(int value)
    {
        CardBonusDice = Math.Max(0, CardBonusDice + value);
        Changed?.Invoke();
    }

    public void AddCardDamageBonus(int value)
    {
        CardDamageBonus += value;
        Changed?.Invoke();
    }

    public void AddCardDefendReduction(int value)
    {
        CardDefendReduction += value;
        Changed?.Invoke();
    }

    public void UsePlayerAttackRollForDamage()
    {
        usePlayerAttackRollForDamage = true;
    }

    public void UseEnemyAttackRollForDefense()
    {
        useEnemyAttackRollForDefense = true;
    }

    public void ResolvePlayerAttackRollDamage(int finalAttackTotal)
    {
        if (!usePlayerAttackRollForDamage) return;

        CardDamageBonus += Math.Max(0, finalAttackTotal);
        usePlayerAttackRollForDamage = false;

        Changed?.Invoke();
    }

    public bool NeedsEnemyAttackRollForDefense =>
        useEnemyAttackRollForDefense;

    public void ResolveEnemyAttackRollDefense(int finalEnemyAttackTotal)
    {
        if (!useEnemyAttackRollForDefense) return;

        CardDefendReduction += Math.Max(0, finalEnemyAttackTotal);
        useEnemyAttackRollForDefense = false;

        Changed?.Invoke();
    }

    public void AddCardAudacity(int value)
    {
        Fate.Audacity = Math.Min(
            5,
            Fate.Audacity + Math.Max(0, value)
        );

        Changed?.Invoke();
    }

    // ICheatHost
    public void RemoveMaliceDice(int count)
    {
        RemovedMaliceDice = count;
        Changed?.Invoke();
    }

    public void ArmSleightOfHand()
    {
        SleightArmed = true;
        Changed?.Invoke();
    }

    public void Log(string message) => View.Log(message);

    public bool PlayCheat(ICheat cheat)
    {
        var result = CheatRunner.TryPlay(cheat, this);
        Track(result);
        Changed?.Invoke();

        return result.Played;
    }

    public bool PayCheat(ICheat cheat)
    {
        var result = CheatRunner.TryPay(cheat, this);
        Track(result);
        Changed?.Invoke();

        return result.Played;
    }

    void Track(CheatResult result)
    {
        if (result.SuspicionChecked)
            CheatedThisFight = true;

        if (result.Noticed)
            FailedThisFight = true;
    }
}