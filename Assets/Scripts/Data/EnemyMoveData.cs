
using System.Collections.Generic;
using UnityEngine;

public enum EnemyMoveType
{
    Attack,
    Defend,
    Skill
}

public enum EnemyMoveEffectType
{
    DealDamage,
    ApplyStatus,
    Heal,
    GainGuard,
    RemoveStatus,
    ModifyAudacity
}

public enum EnemyEffectTarget
{
    Player,
    Self
}

public enum EnemyStatusEffectType
{
    StrengthDown,
    MagicDown,
    AgilityDown,
    AttackDown,
    DefenseDown,
    AccuracyDown,
    Weakness,
    Vulnerable,


    StrengthUp,
    MagicUp,
    AgilityUp,
    AttackUp,
    DefenseUp,
    DamageReduction,
    Regeneration
}

[System.Serializable]
public class EnemyMoveEffect
{
    [Header("Effect")]
    public EnemyMoveEffectType effectType;
    public EnemyEffectTarget target = EnemyEffectTarget.Player;

    [Header("Status Effect")]
    public EnemyStatusEffectType statusEffect;

    [Header("Values")]
    public int amount = 1;

    [Min(1)]
    public int durationTurns = 1;

    [Range(0, 100)]
    public int chancePercent = 100;
}

// One configurable enemy move.
// Enemies select from their assigned moves using weighted random selection.
[CreateAssetMenu(
    menuName = "Paperboard God/Enemy Move",
    fileName = "NewEnemyMove"
)]
public class EnemyMoveData : ScriptableObject
{
    [Header("Presentation")]
    public string moveName = "New Move";

    [TextArea]
    public string description;

    [Header("Move Category")]
    public EnemyMoveType moveType = EnemyMoveType.Attack;

    [Header("Weighted Selection")]
    [Min(0f)]
    public float selectionWeight = 1f;

    [Header("Attack Resolution")]
    [Min(1)]
    public int damageDie = 4;

    public int damageBonus = 1;
    public int attackModifier = 0;

    [Header("Defense")]
    [Min(0)]
    public int guardAmount = 0;

    [Range(0, 100)]
    public int damageReductionPercent = 0;

    [Min(0)]
    public int defenseDurationTurns = 1;

    [Header("Special Effects")]
    public List<EnemyMoveEffect> effects =
        new List<EnemyMoveEffect>();
}
