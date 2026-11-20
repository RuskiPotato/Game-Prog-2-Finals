using System;
using System.Collections.Generic;

public static class MapGenerator {
    // Builds the whole run's map from the Map stream (seed it first with Rng.Seed).
    public static List<MapNode> Generate(RunConfig cfg) {
        var rng = Rng.Get(RngStream.Map);
        var map = new List<MapNode>();
        for (int act = 0; act < cfg.acts; act++) {
            var types = BuildAct(cfg, rng);
            for (int i = 0; i < types.Count; i++)
                map.Add(new MapNode { act = act, index = i, type = types[i] });
        }
        return map;
    }

    static List<NodeType> BuildAct(RunConfig cfg, Random rng) {
        int middle = Math.Max(0, cfg.nodesPerAct - 2);      // first = Fight, last = Boss
        var mid = new List<NodeType>();

        for (int i = 0; i < cfg.shopsPerAct && mid.Count < middle; i++) mid.Add(NodeType.Shop);

        int needCombat = Math.Max(0, cfg.minCombatPerAct - 1);   // the opening Fight is the first
        for (int i = 0; i < needCombat && mid.Count < middle; i++) mid.Add(NodeType.Fight);

        int elites = 0, snacks = 0;
        while (mid.Count < middle) {
            var t = WeightedPick(cfg, rng, elites < cfg.maxElitePerAct, snacks < cfg.maxSnackPerAct);
            if (t == NodeType.Elite) elites++;
            if (t == NodeType.SnackBreak) snacks++;
            mid.Add(t);
        }

        Shuffle(mid, rng);

        var result = new List<NodeType> { NodeType.Fight };
        result.AddRange(mid);
        result.Add(NodeType.Boss);
        return result;
    }

    static NodeType WeightedPick(RunConfig c, Random rng, bool allowElite, bool allowSnack) {
        int wF = c.fightWeight;
        int wE = allowElite ? c.eliteWeight : 0;
        int wS = allowSnack ? c.snackWeight : 0;
        int wD = c.disputeWeight;
        int total = wF + wE + wS + wD;
        if (total <= 0) return NodeType.Fight;
        int roll = rng.Next(total);
        if (roll < wF) return NodeType.Fight;  roll -= wF;
        if (roll < wE) return NodeType.Elite;  roll -= wE;
        if (roll < wS) return NodeType.SnackBreak;
        return NodeType.RulesDispute;
    }

    static void Shuffle<T>(List<T> list, Random rng) {
        for (int i = list.Count - 1; i > 0; i--) {
            int j = rng.Next(i + 1);
            T tmp = list[i]; list[i] = list[j]; list[j] = tmp;
        }
    }
}
