using UnityEngine;

// Decides which enemy this fight uses, and skips map nodes that don't have a screen yet.
public static class EncounterSelector {
    public static EnemyData Pick(GameManager gm) {
        var cfg = gm.Config;
        var progress = gm.Progress;
        if (progress.Phase == GamePhase.Tutorial) return cfg.dummy;
        if (progress.Phase == GamePhase.FirstEncounter) return cfg.scriptedBrute;
        if (gm.Run.Finished) return null;

        var node = gm.Run.Current;
        if (node.type == NodeType.Boss && cfg.bosses.Count > 0)
            return cfg.bosses[Mathf.Min(node.act, cfg.bosses.Count - 1)];
        var list = (node.type == NodeType.Elite && cfg.elites.Count > 0) ? cfg.elites : cfg.fights;
        if (list.Count == 0) return null;
        return list[Rng.Get(RngStream.Events).Next(list.Count)];
    }

    // Placeholder until the map screen exists: skip Shop / Snack Break / Rules Dispute nodes.
    public static void SkipNonCombatNodes(GameManager gm) {
        var run = gm.Run;
        while (!run.Finished && !run.Current.IsCombat) {
            var n = run.Current;
            if (n.type == NodeType.SnackBreak) {
                gm.Hero.Heal(gm.Config.snackHeal);
                gm.Fate.AddSuspicion(-10);
            }
            Debug.Log($"[placeholder] skipped {n.type} node (act {n.act + 1}, node {n.index + 1})");
            run.CompleteCurrent();
        }
    }
}
