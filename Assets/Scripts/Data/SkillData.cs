using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum SkillKind {
    Attack,     // rolls to hit like a normal attack (clash); damage may multi-hit and be multiplied; effects apply on a hit
    Support     // no roll: effects apply immediately (heal, shield, buff, debuff). The enemy's attack still comes.
}

public enum SkillEffectType { Heal, Shield, ApplyStatus, RemoveStatus, ModifyAudacity }
public enum SkillEffectTarget { Self, Enemy }

[Serializable]
public class SkillEffect {
    public SkillEffectType effectType = SkillEffectType.Shield;
    public SkillEffectTarget target = SkillEffectTarget.Self;
    [Tooltip("Used by ApplyStatus / RemoveStatus.")]
    public EnemyStatusEffectType statusEffect;
    [Tooltip("Heal/Shield amount, status strength, or Audacity change.")]
    public int amount = 0;
    [Min(1)] public int durationTurns = 1;
    [Range(0, 100)] public int chancePercent = 100;
}

// A hero skill. It is a card, so it sits in the hand beside Attack/Defend and the combat cards.
// All numbers are left for you to fill in the Inspector.
[CreateAssetMenu(menuName = "Paperboard God/Skill", fileName = "NewSkill")]
public class SkillData : CardData {
    public string id;
    public string displayName;
    [TextArea] public string description;
    [Min(0)] public int cooldown = 3;     // skills use cooldowns, never mana. 0 = no cooldown

    [Header("Kind")]
    public SkillKind kind = SkillKind.Attack;

    [Header("Attack skills (ignored for Support)")]
    [Min(1)] public int hits = 1;                 // damage rolls per successful hit roll (multi-hit)
    [Min(0f)] public float damageMultiplier = 1f; // applied to each hit's damage
    public int flatDamage = 0;                    // added to each hit before the multiplier
    public int attackRollModifier = 0;            // added to the hit roll while this skill is used

    [Header("Effects")]
    [Tooltip("Attack skills: applied when the skill hits. Support skills: applied immediately.")]
    public List<SkillEffect> effects = new List<SkillEffect>();

    public override string Id => id;
    public override string Title => displayName;
    public override CardKind Kind => CardKind.Action;
    public override bool CanPlay(BattleContext ctx) => ctx.CanAct && ctx.Cooldowns.IsReady(this);
    public override string CostText(BattleContext ctx) {
        int cd = ctx.Cooldowns.Remaining(this);
        return cd > 0 ? $"CD {cd}" : (cooldown > 0 ? $"SKILL  CD {cooldown}" : "SKILL");
    }
    public override string DetailText(BattleContext ctx) => string.IsNullOrEmpty(description) ? Summary() : description;
    public override void Play(BattleContext ctx, BattleInput input) => input.PressSkill(this);

    // Total damage for one successful hit roll: `hits` separate weapon rolls, each (roll + card bonus + flat) x multiplier.
    public int RollDamage(HeroUnit hero, int cardBonus) {
        int total = 0;
        for (int i = 0; i < Mathf.Max(1, hits); i++) {
            int one = hero.RollDamage() + cardBonus + flatDamage;
            total += Mathf.Max(0, Mathf.RoundToInt(one * damageMultiplier));
        }
        return total;
    }

    // Auto text for the card / clash panel.
    public string Summary() {
        var sb = new StringBuilder();
        if (kind == SkillKind.Attack) {
            sb.Append(hits > 1 ? $"{hits} hits" : "1 hit");
            if (!Mathf.Approximately(damageMultiplier, 1f)) sb.Append($" x{damageMultiplier:0.##} dmg");
        } else sb.Append("Support");
        foreach (var e in effects) {
            if (e == null) continue;
            sb.Append(", ").Append(Describe(e));
        }
        return sb.ToString();
    }

    public static string Describe(SkillEffect e) {
        string who = e.target == SkillEffectTarget.Self ? "self" : "enemy";
        switch (e.effectType) {
            case SkillEffectType.Heal: return $"heal {e.amount}";
            case SkillEffectType.Shield: return $"shield {e.amount}";
            case SkillEffectType.ApplyStatus: return $"{StatusTracker.NameOf(e.statusEffect)} {e.amount} on {who} ({e.durationTurns}t)";
            case SkillEffectType.RemoveStatus: return $"remove {StatusTracker.NameOf(e.statusEffect)} from {who}";
            default: return $"Audacity {e.amount:+0;-0}";
        }
    }
}
