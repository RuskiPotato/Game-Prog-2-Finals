using System;
using System.Text.RegularExpressions;
using UnityEngine;

public enum CombatCardAction { Attack, Defend }
public enum CombatCardEffect { None, RollModifier, ExtraPlayerDie, DamageBonus, DefendDamageReduction, AudacityBonus }

// How a card calculates its damage contribution. Dice formulas are rolled as soon as the card is played.
public enum CardDamageCalculation { None, Fixed, DiceFormula, PlayerAttackRoll }
// EnemyAttackRoll uses the enemy's final d20 total (die result + attack modifier).
public enum CardDefenseCalculation { None, Fixed, DiceFormula, EnemyAttackRoll }

// Collectible combat cards are data-driven modifiers routed through the normal battle resolvers.
[CreateAssetMenu(menuName = "Paperboard God/Cards/Combat Card", fileName = "CombatCard")]
public class CombatCardData : CardData {
    public string id = "combat_card";
    public string displayName = "Combat Card";
    [TextArea] public string description;
    public CombatCardAction action = CombatCardAction.Attack;

    [Header("Legacy Effect (optional)")]
    public CombatCardEffect effect = CombatCardEffect.None;
    public int effectValue = 1;

    [Header("Damage")]
    public CardDamageCalculation damageCalculation = CardDamageCalculation.None;
    [Min(0)] public int fixedDamage;
    [Tooltip("Dice notation such as 1d8 + 2. Rolled once immediately when this card is played.")]
    public string damageDiceFormula = "1d8 + 2";

    [Header("Defense")]
    public CardDefenseCalculation defenseCalculation = CardDefenseCalculation.None;
    [Min(0)] public int fixedDefense;
    [Tooltip("Dice notation such as 1d6 + 1. Rolled once immediately when this card is played.")]
    public string defenseDiceFormula = "1d6 + 1";

    public bool exhaustOnPlay;

    public override string Id => id;
    public override string Title => displayName;
    public override CardKind Kind => CardKind.Action;
    public override bool CanPlay(BattleContext ctx) => ctx.CanAct;
    public override string CostText(BattleContext ctx) => action == CombatCardAction.Defend ? "DEFEND" : "ATTACK";
    public override string DetailText(BattleContext ctx) => description;
    public override void Play(BattleContext ctx, BattleInput input) => input.PressCombatCard(this);

    // Called once after the player commits the card. Independent formulas roll here;
    // roll-derived values are resolved later in the same clash when their source roll exists.
    public void Apply(BattleContext ctx) {
        switch (effect) {
            case CombatCardEffect.RollModifier: ctx.AddCardRollModifier(effectValue); break;
            case CombatCardEffect.ExtraPlayerDie: ctx.AddCardExtraDice(effectValue); break;
            case CombatCardEffect.DamageBonus: ctx.AddCardDamageBonus(effectValue); break;
            case CombatCardEffect.DefendDamageReduction: ctx.AddCardDefendReduction(effectValue); break;
            case CombatCardEffect.AudacityBonus: ctx.AddCardAudacity(effectValue); break;
        }

        switch (damageCalculation) {
            case CardDamageCalculation.Fixed:
                ctx.AddCardDamageBonus(Mathf.Max(0, fixedDamage));
                break;
            case CardDamageCalculation.DiceFormula:
                ctx.AddCardDamageBonus(RollFormula(damageDiceFormula, "damage", this));
                break;
            case CardDamageCalculation.PlayerAttackRoll:
                ctx.UsePlayerAttackRollForDamage();
                break;
        }

        switch (defenseCalculation) {
            case CardDefenseCalculation.Fixed:
                ctx.AddCardDefendReduction(Mathf.Max(0, fixedDefense));
                break;
            case CardDefenseCalculation.DiceFormula:
                ctx.AddCardDefendReduction(RollFormula(defenseDiceFormula, "defense", this));
                break;
            case CardDefenseCalculation.EnemyAttackRoll:
                ctx.UseEnemyAttackRollForDefense();
                break;
        }
    }

    static readonly Regex FormulaPattern = new Regex(@"^\s*(\d*)d(\d+)\s*([+-]\s*\d+)?\s*$", RegexOptions.IgnoreCase);

    static int RollFormula(string formula, string label, UnityEngine.Object source) {
        if (string.IsNullOrWhiteSpace(formula)) {
            Debug.LogError($"{source.name}: {label} dice formula is empty.", source);
            return 0;
        }
        var match = FormulaPattern.Match(formula);
        if (!match.Success) {
            Debug.LogError($"{source.name}: invalid {label} dice formula '{formula}'. Use notation such as 1d8 + 2.", source);
            return 0;
        }

        int count = string.IsNullOrEmpty(match.Groups[1].Value) ? 1 : int.Parse(match.Groups[1].Value);
        int sides = int.Parse(match.Groups[2].Value);
        int modifier = string.IsNullOrWhiteSpace(match.Groups[3].Value) ? 0 : int.Parse(match.Groups[3].Value.Replace(" ", ""));
        if (count < 1 || count > 100 || sides < 2 || sides > 1000) {
            Debug.LogError($"{source.name}: {label} dice formula must use 1-100 dice with 2-1000 sides.", source);
            return 0;
        }

        int total = modifier;
        for (int i = 0; i < count; i++) total += DiceService.Die(sides);
        return Mathf.Max(0, total);
    }
}
