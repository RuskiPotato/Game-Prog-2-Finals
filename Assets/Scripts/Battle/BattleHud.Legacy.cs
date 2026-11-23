using TMPro;
using UnityEngine;
using UnityEngine.UI;

// TEMPORARY. These are the old BattleHud fields, kept (hidden) only so Paperboard God > Migrate BattleHud can copy your
// Inspector assignments into BattleStatusView / ClashPresenter. After migrating, delete this file and Editor/BattleHudMigrator.cs.
public partial class BattleHud {
    [HideInInspector] public GameObject fatePanel;
    [HideInInspector] public DiamondMeter fateMeter;
    [HideInInspector] public TMP_Text fateValueText;
    [HideInInspector] public TMP_Text tierText;
    [HideInInspector] public TMP_Text maliceText;
    [HideInInspector] public TMP_Text goldText;
    [HideInInspector] public GameObject suspicionPanel;
    [HideInInspector] public Slider suspicionBar;
    [HideInInspector] public TMP_Text suspicionTierText;
    [HideInInspector] public TMP_Text suspicionDetailText;
    [HideInInspector] public GameObject moralePanel;
    [HideInInspector] public Slider moraleBar;
    [HideInInspector] public TMP_Text moraleTierText;
    [HideInInspector] public TMP_Text moraleDetailText;
    [HideInInspector] public UnitHpTag heroHpTag;
    [HideInInspector] public UnitHpTag enemyHpTag;
    [HideInInspector] public GameObject heroProfilePanel;
    [HideInInspector] public TMP_Text heroText;
    [HideInInspector] public Slider heroHpBar;
    [HideInInspector] public AudacityPips audacityPips;
    [HideInInspector] public TMP_Text audacityText;
    [HideInInspector] public TMP_Text enemyText;
    [HideInInspector] public Slider enemyHpBar;
    [HideInInspector] public TMP_Text oddsText;
    [HideInInspector] public TMP_Text turnOrderText;
    [HideInInspector] public GameObject heroActionPanel;
    [HideInInspector] public TMP_Text heroActionText;
    [HideInInspector] public GameObject enemyActionPanel;
    [HideInInspector] public TMP_Text enemyActionText;
    [HideInInspector] public UnitSelectable heroSelectable;
    [HideInInspector] public UnitSelectable enemySelectable;
    [HideInInspector] public PassivesPanel heroPassives;
    [HideInInspector] public PassivesPanel enemyPassives;
    [HideInInspector] public DiceTray diceTray;
    [HideInInspector] public DiceTray enemyDiceTray;
    [HideInInspector] public RectTransform heroCombatant;
    [HideInInspector] public RectTransform enemyCombatant;
    [HideInInspector] public bool heroProfileAlwaysVisible;
    [HideInInspector] public float clashMovePixels = 58f;
    [HideInInspector] public float clashMoveDuration = 0.28f;
    [HideInInspector] public float clashResultDuration = 0.95f;
    [HideInInspector] public float clashWinnerPushPixels = 82f;
    [HideInInspector] public float clashLoserPushPixels = 80f;
}
