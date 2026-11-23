using UnityEngine;

// Small shared helpers for skill effects and enemy-move effects.
public static class EffectOps {
    public static bool Passes(int chancePercent) =>
        chancePercent >= 100 || (chancePercent > 0 && Random.Range(0, 100) < chancePercent);

    public static void ChangeAudacity(BattleContext ctx, int delta) {
        ctx.Fate.Audacity = Mathf.Clamp(ctx.Fate.Audacity + delta, 0, 5);
    }
}
