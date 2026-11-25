
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Everything that reads battle state and shows numbers: Fate diamond + gold, Suspicion/Morale bars, hero/enemy profiles,
// HP tags, status lines, passives on selection, the turn-order row, the odds line and both clash cards.
// BattleHud calls Refresh(ctx); this class never touches input or animation.
public class BattleStatusView : MonoBehaviour
{
    [Header("Fate")]
    public GameObject fatePanel;
    public DiamondMeter fateMeter;
    public TMP_Text fateValueText;
    public TMP_Text tierText;
    public TMP_Text maliceText;
    public TMP_Text goldText;

    [Header("Suspicion")]
    public GameObject suspicionPanel;
    public Slider suspicionBar;
    public TMP_Text suspicionTierText;
    public TMP_Text suspicionDetailText;

    [Header("Morale")]
    public GameObject moralePanel;
    public Slider moraleBar;
    public TMP_Text moraleTierText;
    public TMP_Text moraleDetailText;

    [Header("Under-unit HP")]
    public UnitHpTag heroHpTag, enemyHpTag;

    [Header("HeroPanel")]
    public GameObject heroProfilePanel;
    public TMP_Text heroText;
    public Slider heroHpBar;
    public AudacityPips audacityPips;
    public TMP_Text audacityText;
    public bool heroProfileAlwaysVisible = false;

    public TMP_Text enemyText;
    public Slider enemyHpBar;

    [Header("Status lines (optional)")]
    public TMP_Text heroStatusText;
    public TMP_Text enemyStatusText;

    [Header("Odds / turn order")]
    public TMP_Text oddsText;
    public TMP_Text turnOrderText;
    public string activeColorHex = "FFD35A";
    public string turnOrderSeparator = "  vs  ";
    public bool showEnemyLandChance = false;

    [Header("Clash cards")]
    public GameObject heroActionPanel;
    public TMP_Text heroActionNameText;
    public TMP_Text heroActionDescriptionText;

    public GameObject enemyActionPanel;
    public TMP_Text enemyActionNameText;
    public TMP_Text enemyActionDescriptionText;

    [Header("Passives")]
    public UnitSelectable heroSelectable, enemySelectable;
    public PassivesPanel heroPassives, enemyPassives;

    enum Selected { None, Hero, Enemy }

    Selected selected = Selected.None;
    BattleContext lastCtx;
    List<Unit> turnOrder = new List<Unit>();

    public bool HasSelection => selected != Selected.None;

    static void SetText(TMP_Text t, string s)
    {
        if (t != null) t.text = s;
    }

    static void SetActive(GameObject g, bool on)
    {
        if (g != null && g.activeSelf != on)
            g.SetActive(on);
    }

    void Awake()
    {
        if (heroSelectable != null)
            heroSelectable.Clicked += _ => Select(Selected.Hero);

        if (enemySelectable != null)
            enemySelectable.Clicked += _ => Select(Selected.Enemy);

        if (heroPassives != null)
            heroPassives.Hide();

        if (enemyPassives != null)
            enemyPassives.Hide();

        SetActive(heroProfilePanel, heroProfileAlwaysVisible);
    }

    public void SetTurnOrder(List<Unit> order)
    {
        turnOrder = order ?? new List<Unit>();

        if (lastCtx != null)
            RefreshTurnOrder(lastCtx);
    }

    // Clicking the selected unit again, or pressing Esc, clears the selection.
    void Select(Selected which)
    {
        selected = selected == which ? Selected.None : which;

        if (lastCtx != null)
            RefreshSelection(lastCtx);
    }

    public void ClearSelection() => Select(selected);

    public void Refresh(BattleContext ctx)
    {
        lastCtx = ctx;

        var fate = ctx.Fate;
        bool revealed = ctx.Progress.FateBarRevealed;

        SetActive(fatePanel, revealed);

        if (fateMeter != null)
            fateMeter.SetValue(fate.Fate / 100f);

        SetText(fateValueText, fate.Fate.ToString());
        SetText(tierText, fate.TierName);
        SetText(maliceText,
            $"Malice dice: {fate.MaliceDice}  (rolling {ctx.CurrentDice})" +
            (ctx.HasFortuneDie ? " + Fortune Die" : ""));
        SetText(goldText, $"Gold: {fate.Gold}");

        SetActive(suspicionPanel, revealed);

        if (suspicionBar != null)
            suspicionBar.value = fate.Suspicion / 100f;

        SetText(suspicionTierText, SuspicionInfo.TierOf(fate.Suspicion).ToString());
        SetText(suspicionDetailText,
            $"{fate.Suspicion}/100  -  check DC {SuspicionInfo.CheckDc(fate.Suspicion)}");

        SetActive(moralePanel, revealed && !fate.SoloMode);

        if (moraleBar != null)
            moraleBar.value = fate.Morale / 100f;

        SetText(moraleTierText, MoraleName(fate.Morale));
        SetText(moraleDetailText,
            $"{fate.Morale}/100  -  rolls {fate.MoraleModifier:+0;-0;+0}");

        if (oddsText != null)
        {
            oddsText.gameObject.SetActive(!ctx.Scripted);

            float p = Odds.HitChance(
                ctx.Enemy.DefenseDc,
                ctx.RollMod,
                ctx.CurrentDice,
                ctx.HasFortuneDie) * 100f;

            oddsText.text =
                $"Hit chance: {p:0.0}%  (need {ctx.Enemy.DefenseDc - ctx.RollMod}+ on die)";
        }

        SetText(heroText,
            $"{ctx.Hero.DisplayName}  HP {ctx.Hero.Hp}/{ctx.Hero.MaxHp}");

        if (heroHpBar != null)
        {
            heroHpBar.value = ctx.Hero.MaxHp > 0
                ? (float)ctx.Hero.Hp / ctx.Hero.MaxHp
                : 0f;
        }

        SetText(audacityText, $"Audacity: {fate.Audacity}");

        if (audacityPips != null)
            audacityPips.Set(fate.Audacity);

        SetText(enemyText,
            ctx.Scripted
                ? ctx.Enemy.DisplayName
                : $"{ctx.Enemy.DisplayName}  HP {ctx.Enemy.Hp}/{ctx.Enemy.MaxHp}");

        if (enemyHpBar != null)
        {
            enemyHpBar.gameObject.SetActive(!ctx.Scripted);

            enemyHpBar.value = ctx.Enemy.MaxHp > 0
                ? (float)ctx.Enemy.Hp / ctx.Enemy.MaxHp
                : 0f;
        }

        if (heroHpTag != null)
            heroHpTag.Set(ctx.Hero.DisplayName, ctx.Hero.Hp, ctx.Hero.MaxHp, true);

        if (enemyHpTag != null)
            enemyHpTag.Set(ctx.Enemy.DisplayName, ctx.Enemy.Hp, ctx.Enemy.MaxHp, !ctx.Scripted);

        SetText(heroStatusText, StatusLine(ctx.Hero));
        SetText(enemyStatusText, StatusLine(ctx.Enemy));

        RefreshClashCards(ctx);
        RefreshSelection(ctx);
        RefreshTurnOrder(ctx);
    }

    static string StatusLine(Unit u)
    {
        string s = u.Statuses.Describe();

        if (u.Shield > 0)
            s = s.Length > 0
                ? $"Shield {u.Shield}  |  {s}"
                : $"Shield {u.Shield}";

        return s;
    }

    static string MoraleName(int morale) =>
        morale >= 61 ? "Confident" :
        morale >= 50 ? "Steady" :
        morale >= 30 ? "Wavering" : "Breaking";

    // ---- Clash cards ----

    void RefreshClashCards(BattleContext ctx)
    {
        SetActive(enemyActionPanel, ctx.Enemy.Attacks);

        if (enemyActionNameText != null)
            enemyActionNameText.text = EnemyCardName(ctx);

        if (enemyActionDescriptionText != null)
            enemyActionDescriptionText.text = EnemyCardAction(ctx);

        bool committed = ctx.ChosenAction != PlayerAction.None;

        SetActive(heroActionPanel, committed);

        if (heroActionNameText != null)
            heroActionNameText.text = HeroCardName(ctx);

        if (heroActionDescriptionText != null)
            heroActionDescriptionText.text = HeroCardAction(ctx);
    }

    string EnemyCardName(BattleContext ctx)
    {
        return ctx.Enemy.CurrentMoveName;
    }

    string EnemyCardAction(BattleContext ctx)
    {
        var enemy = ctx.Enemy;
        var move = enemy.CurrentMove;

        if (ctx.Scripted)
            return "";

        if (move != null && move.moveType == EnemyMoveType.Defend)
        {
            string g = move.guardAmount > 0
                ? $"Guard {move.guardAmount}"
                : "Guarding";

            if (move.damageReductionPercent > 0)
                g += $"  -{move.damageReductionPercent}% damage";

            return g + "\nNo attack this round";
        }

        if (move != null && move.moveType == EnemyMoveType.Skill)
            return "No attack this round" + EffectLines(move);

        var d = enemy.Data;

        int die = move != null ? move.damageDie : d.damageDie;
        int bonus = move != null ? move.damageBonus : d.damageBonus;

        string text =
            $"d{die}{bonus:+0;-0;+0} damage\n" +
            $"{1 + ctx.Fate.MaliceDice} dice, keep highest";

        if (showEnemyLandChance)
        {
            text +=
                $"\n{Odds.EnemyHitChance(ctx.Hero.DefenseDc, enemy.AttackModifier, 1 + ctx.Fate.MaliceDice) * 100f:0.0}% to land if you miss";
        }

        return text + EffectLines(move);
    }

    static string EffectLines(EnemyMoveData move)
    {
        if (move == null || move.effects == null || move.effects.Count == 0)
            return "";

        var sb = new StringBuilder();

        foreach (var e in move.effects)
        {
            if (e == null) continue;

            sb.Append('\n');

            switch (e.effectType)
            {
                case EnemyMoveEffectType.ApplyStatus:
                    sb.Append($"{StatusTracker.NameOf(e.statusEffect)} {e.amount} ({e.durationTurns}t)");
                    break;

                case EnemyMoveEffectType.DealDamage:
                    sb.Append($"+{e.amount} damage");
                    break;

                case EnemyMoveEffectType.Heal:
                    sb.Append($"Heal {e.amount}");
                    break;

                case EnemyMoveEffectType.GainGuard:
                    sb.Append($"Guard {e.amount}");
                    break;

                case EnemyMoveEffectType.RemoveStatus:
                    sb.Append($"Cleanse {StatusTracker.NameOf(e.statusEffect)}");
                    break;

                default:
                    sb.Append($"Audacity {e.amount:+0;-0}");
                    break;
            }

            if (e.chancePercent < 100)
                sb.Append($" ({e.chancePercent}%)");
        }

        return sb.ToString();
    }

    string HeroCardName(BattleContext ctx)
    {
        return ctx.ChosenCardTitle ?? ctx.ChosenAction.ToString();
    }

    string HeroCardAction(BattleContext ctx)
    {
        if (ctx.ChosenAction == PlayerAction.Defend)
            return "Halve the hit";

        if (ctx.ActionRollsToHit)
        {
            if (ctx.Scripted)
                return "";

            string text =
                $"{Odds.HitChance(ctx.Enemy.DefenseDc, ctx.RollMod, ctx.CurrentDice, ctx.HasFortuneDie) * 100f:0.0}% to hit";

            if (ctx.ActiveSkill != null)
                text += "\n" + ctx.ActiveSkill.Summary();

            return text;
        }

        return ctx.ActiveSkill != null
            ? ctx.ActiveSkill.Summary()
            : "";
    }

    // ---- Selection / passives ----

    void RefreshSelection(BattleContext ctx)
    {
        SetActive(
            heroProfilePanel,
            heroProfileAlwaysVisible || selected == Selected.Hero);

        if (heroSelectable != null)
            heroSelectable.SetSelected(selected == Selected.Hero);

        if (enemySelectable != null)
            enemySelectable.SetSelected(selected == Selected.Enemy);

        if (selected == Selected.Hero && heroPassives != null)
        {
            var lines = new List<string>(ctx.Hero.Class.passives);

            if (ctx.Progress.FateBarRevealed && ctx.Fate.MaliceDice > 0)
                lines.Insert(0,
                    $"Jinxed: Destiny adds {ctx.Fate.MaliceDice} Malice dice (keep lowest)");

            heroPassives.Show(ctx.Hero.DisplayName, lines);
        }
        else if (heroPassives != null)
        {
            heroPassives.Hide();
        }

        if (selected == Selected.Enemy && enemyPassives != null)
            enemyPassives.Show(ctx.Enemy.DisplayName, ctx.Enemy.Data.passives);
        else if (enemyPassives != null)
            enemyPassives.Hide();
    }

    // ---- Turn order ----

    void RefreshTurnOrder(BattleContext ctx)
    {
        if (turnOrderText == null) return;

        if (turnOrder.Count == 0)
        {
            turnOrderText.text = "";
            return;
        }

        Unit active = null;

        switch (ctx.State)
        {
            case BattleState.HeroTurn:
            case BattleState.Roll:
            case BattleState.Resolve:
                active = ctx.Hero;
                break;

            case BattleState.EnemyTurn:
                active = ctx.Enemy;
                break;
        }

        var sb = new StringBuilder();

        if (ctx.Round > 0)
            sb.Append("Round ").Append(ctx.Round).Append("   ");

        for (int i = 0; i < turnOrder.Count; i++)
        {
            if (i > 0)
                sb.Append(turnOrderSeparator);

            var u = turnOrder[i];

            string name = u.IsDead
                ? $"<s>{u.DisplayName}</s>"
                : u.DisplayName;

            if (u == active)
            {
                sb.Append("<b><color=#")
                    .Append(activeColorHex)
                    .Append('>')
                    .Append(name)
                    .Append("</color></b>");
            }
            else
            {
                sb.Append(name);
            }
        }

        turnOrderText.text = sb.ToString();
    }
}
