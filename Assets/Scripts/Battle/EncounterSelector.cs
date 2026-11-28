using UnityEngine;

public static class EncounterSelector {
    public static EnemyData Pick(GameManager gm) {
        var cfg = gm.Config;
        if (gm.Progress.Phase == GamePhase.Tutorial) return cfg.dummy;
        if (gm.Progress.Phase == GamePhase.FirstEncounter) return cfg.scriptedBrute;
        if (gm.Run == null || gm.Run.Current == null) return null;
        var node = gm.Run.Current;
        if (node.type == NodeType.Boss && cfg.bosses.Count > 0)
            return cfg.bosses[Mathf.Min(node.act, cfg.bosses.Count - 1)];
        var list = node.type == NodeType.Elite && cfg.elites.Count > 0 ? cfg.elites : cfg.fights;
        if (list.Count == 0) return null;
        return list[Rng.Get(RngStream.Events).Next(list.Count)];
    }
}
