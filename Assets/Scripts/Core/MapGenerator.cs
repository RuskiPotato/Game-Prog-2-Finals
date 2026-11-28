using System;
using System.Collections.Generic;

public static class MapGenerator {
    const int Lanes = 3;

    // Three-lane branching map. Every node connects to adjacent lanes in the next row.
    public static List<MapNode> Generate(RunConfig cfg) {
        var rng = Rng.Get(RngStream.Map);
        var map = new List<MapNode>();
        int rows = Math.Max(3, cfg.nodesPerAct);
        for (int act = 0; act < cfg.acts; act++) {
            var actNodes = new List<MapNode>();
            for (int row = 0; row < rows; row++) {
                if (row == 0 || row == rows - 1) {
                    int lane = 1;
                    actNodes.Add(new MapNode {
                        id = Id(act, row, lane), act = act, index = row, lane = lane,
                        type = row == 0 ? NodeType.Fight : NodeType.Boss
                    });
                } else {
                    for (int lane = 0; lane < Lanes; lane++) {
                        actNodes.Add(new MapNode {
                            id = Id(act, row, lane), act = act, index = row, lane = lane,
                            type = PickType(cfg, rng, row, lane)
                        });
                    }
                }
            }
            for (int row = 0; row < rows - 1; row++) {
                var from = actNodes.FindAll(n => n.index == row);
                var to = actNodes.FindAll(n => n.index == row + 1);
                foreach (var a in from)
                    foreach (var b in to)
                        if (Math.Abs(a.lane - b.lane) <= 1) a.nextNodeIds.Add(b.id);
            }
            map.AddRange(actNodes);
        }
        // Connect each Act boss to the next Act's single starting node.
        for (int act = 0; act < cfg.acts - 1; act++) {
            var boss = map.Find(n => n.act == act && n.type == NodeType.Boss);
            var nextStart = map.Find(n => n.act == act + 1 && n.index == 0);
            if (boss != null && nextStart != null) boss.nextNodeIds.Add(nextStart.id);
        }
        return map;
    }

    static string Id(int act, int row, int lane) => $"A{act + 1}_R{row + 1}_L{lane + 1}";

    static NodeType PickType(RunConfig c, Random rng, int row, int lane) {
        // Guarantee the middle row has a recovery/shop opportunity without making paths identical.
        int wFight = Math.Max(0, c.fightWeight);
        int wElite = Math.Max(0, c.eliteWeight);
        int wSnack = Math.Max(0, c.snackWeight);
        int wDispute = Math.Max(0, c.disputeWeight);
        int wShop = row == 1 && lane == 1 && c.shopsPerAct > 0 ? 3 : 1;
        int total = wFight + wElite + wSnack + wDispute + wShop;
        if (total <= 0) return NodeType.Fight;
        int roll = rng.Next(total);
        if ((roll -= wFight) < 0) return NodeType.Fight;
        if ((roll -= wElite) < 0) return NodeType.Elite;
        if ((roll -= wShop) < 0) return NodeType.Shop;
        if ((roll -= wSnack) < 0) return NodeType.SnackBreak;
        return NodeType.RulesDispute;
    }
}
