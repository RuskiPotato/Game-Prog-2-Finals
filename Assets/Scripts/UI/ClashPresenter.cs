
using System.Collections;
using TMPro;
using UnityEngine;

// The clash on screen: both dice trays, the two combatants sliding together,
// and the floating CLASH WON/LOST/TIED text.
// BattleHud forwards the IBattleView dice/clash calls here.
public class ClashPresenter : MonoBehaviour
{
    [Header("Dice")]
    public DiceTray diceTray;
    public DiceTray enemyDiceTray;

    [Header("Clash presentation")]
    public RectTransform heroCombatant;
    public RectTransform enemyCombatant;
    public float clashMovePixels = 58f;
    public float clashMoveDuration = 0.28f;
    public float clashResultDuration = 0.95f;
    public float clashWinnerPushPixels = 82f;
    public float clashLoserPushPixels = 80f;

    Vector2 heroHome, enemyHome;
    bool clashPositionsCached;
    Canvas rootCanvas;

    void Awake()
    {
        if (heroCombatant == null)
        {
            var go = GameObject.Find("HeroImage");
            if (go != null)
                heroCombatant = go.GetComponent<RectTransform>();
        }

        if (enemyCombatant == null)
        {
            var go = GameObject.Find("EnemyImage");
            if (go != null)
                enemyCombatant = go.GetComponent<RectTransform>();
        }

        rootCanvas = GetComponentInParent<Canvas>();

        if (rootCanvas == null)
            rootCanvas = FindFirstObjectByType<Canvas>();

        CacheClashPositions();
    }

    public void ClearDice()
    {
        if (diceTray != null)
            diceTray.Clear();

        if (enemyDiceTray != null)
            enemyDiceTray.Clear();
    }

    public IEnumerator Tumble(int diceCount, int fortuneIndex = -1)
    {
        if (diceTray == null)
            yield break;

        yield return diceTray.Tumble(diceCount, fortuneIndex: fortuneIndex);
    }

    public void ShowRoll(RollResult roll)
    {
        if (diceTray != null)
            diceTray.ShowRoll(roll);
    }

    public IEnumerator TumbleEnemy(int diceCount)
    {
        if (enemyDiceTray == null)
            yield break;

        yield return enemyDiceTray.Tumble(diceCount);
    }

    public void ShowEnemyRoll(int[] dice, int keptIndex)
    {
        if (enemyDiceTray != null)
            enemyDiceTray.ShowValues(dice, keptIndex);
    }

    public IEnumerator TumbleClash(
        int heroDiceCount,
        int enemyDiceCount,
        int heroFortuneIndex = -1)
    {
        Coroutine hero = diceTray != null
            ? StartCoroutine(diceTray.Tumble(heroDiceCount, 0.72f, 0.045f, heroFortuneIndex))
            : null;

        Coroutine enemy = enemyDiceTray != null
            ? StartCoroutine(enemyDiceTray.Tumble(enemyDiceCount, 0.72f, 0.045f))
            : null;

        if (hero != null)
            yield return hero;

        if (enemy != null)
            yield return enemy;
    }

    void CacheClashPositions()
    {
        if (heroCombatant != null)
            heroHome = heroCombatant.anchoredPosition;

        if (enemyCombatant != null)
            enemyHome = enemyCombatant.anchoredPosition;

        clashPositionsCached =
            heroCombatant != null &&
            enemyCombatant != null;
    }

    public IEnumerator ClashApproach()
    {
        // Re-read in case layout groups moved the units since Awake.
        CacheClashPositions();

        if (!clashPositionsCached)
            yield break;

        yield return MoveCombatants(
            heroHome + Vector2.right * clashMovePixels,
            enemyHome + Vector2.left * clashMovePixels,
            clashMoveDuration
        );
    }

    IEnumerator MoveCombatants(
        Vector2 heroTarget,
        Vector2 enemyTarget,
        float duration)
    {
        if (!clashPositionsCached)
            yield break;

        Vector2 h = heroCombatant.anchoredPosition;
        Vector2 e = enemyCombatant.anchoredPosition;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            float eased = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(t / duration)
            );

            heroCombatant.anchoredPosition =
                Vector2.LerpUnclamped(h, heroTarget, eased);

            enemyCombatant.anchoredPosition =
                Vector2.LerpUnclamped(e, enemyTarget, eased);

            yield return null;
        }

        heroCombatant.anchoredPosition = heroTarget;
        enemyCombatant.anchoredPosition = enemyTarget;
    }

    // Original method retained for compatibility with existing calls.
    public IEnumerator ClashOutcome(bool heroWon, bool enemyWon)
    {
        yield return ClashOutcome(heroWon, enemyWon, false);
    }

    // New overload supports ties.
    public IEnumerator ClashOutcome(
        bool heroWon,
        bool enemyWon,
        bool tied)
    {
        string label = tied
            ? "CLASH TIED!"
            : heroWon
                ? "CLASH WON!"
                : enemyWon
                    ? "CLASH LOST!"
                    : "BOTH MISSED";

        if (clashPositionsCached)
        {
            Vector2 heroTarget = heroHome;
            Vector2 enemyTarget = enemyHome;

            if (heroWon)
            {
                heroTarget +=
                    Vector2.right * clashWinnerPushPixels;

                enemyTarget +=
                    Vector2.left * clashLoserPushPixels;
            }
            else if (enemyWon)
            {
                enemyTarget +=
                    Vector2.left * clashWinnerPushPixels;

                heroTarget +=
                    Vector2.right * clashLoserPushPixels;
            }

            // On a tie, neither combatant gets pushed back.
            yield return MoveCombatants(
                heroTarget,
                enemyTarget,
                0.16f
            );

            Color resultColor = tied
                ? Color.white
                : heroWon
                    ? new Color(0.28f, 1f, 0.48f)
                    : enemyWon
                        ? new Color(1f, 0.28f, 0.25f)
                        : Color.white;

            RectTransform textTarget =
                heroWon
                    ? heroCombatant
                    : enemyWon
                        ? enemyCombatant
                        : null;

            ShowFloatingClashText(
                label,
                textTarget,
                resultColor
            );

            yield return new WaitForSeconds(
                clashResultDuration
            );

            // Return both combatants to their original positions.
            yield return MoveCombatants(
                heroHome,
                enemyHome,
                0.22f
            );
        }
        else
        {
            ShowFloatingClashText(
                label,
                null,
                Color.white
            );

            yield return new WaitForSeconds(
                clashResultDuration
            );
        }
    }

    void ShowFloatingClashText(
        string message,
        RectTransform target,
        Color color)
    {
        if (rootCanvas == null)
            return;

        var go = new GameObject(
            "FloatingClashResult",
            typeof(RectTransform),
            typeof(CanvasGroup),
            typeof(TextMeshProUGUI)
        );

        var rect = go.GetComponent<RectTransform>();

        rect.SetParent(rootCanvas.transform, false);
        rect.sizeDelta = new Vector2(440f, 72f);

        Vector2 localPoint = Vector2.zero;

        if (target != null)
        {
            Vector2 screen =
                RectTransformUtility.WorldToScreenPoint(
                    rootCanvas.worldCamera,
                    target.position
                );

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rootCanvas.transform as RectTransform,
                screen,
                rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : rootCanvas.worldCamera,
                out localPoint
            );

            localPoint += new Vector2(0f, 125f);
        }

        rect.anchoredPosition = localPoint;

        var text = go.GetComponent<TextMeshProUGUI>();

        text.text = message;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = 38f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.outlineWidth = 0.18f;
        text.outlineColor = Color.black;

        StartCoroutine(
            FadeFloatingText(
                rect,
                go.GetComponent<CanvasGroup>()
            )
        );
    }

    IEnumerator FadeFloatingText(
        RectTransform rect,
        CanvasGroup group)
    {
        float duration = clashResultDuration;

        Vector2 start = rect.anchoredPosition;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            float p = Mathf.Clamp01(t / duration);

            rect.anchoredPosition =
                start + Vector2.up * (34f * p);

            group.alpha = p < 0.7f
                ? 1f
                : 1f - (p - 0.7f) / 0.3f;

            yield return null;
        }

        Destroy(rect.gameObject);
    }
}
