using NUnit.Framework;
using UnityEngine;

public class MapGeneratorTests {
    RunConfig MakeConfig() => ScriptableObject.CreateInstance<RunConfig>();   // defaults: 3 acts, 5 nodes

    [Test]
    public void Every_act_follows_the_rules_for_1000_seeds() {
        var cfg = MakeConfig();
        for (int seed = 1; seed <= 1000; seed++) {
            Rng.Seed(seed);
            var map = MapGenerator.Generate(cfg);
            Assert.AreEqual(cfg.acts * cfg.nodesPerAct, map.Count, $"seed {seed}");

            for (int act = 0; act < cfg.acts; act++) {
                int combat = 0, shops = 0, elites = 0, snacks = 0;
                for (int i = 0; i < cfg.nodesPerAct; i++) {
                    var n = map[act * cfg.nodesPerAct + i];
                    Assert.AreEqual(act, n.act); Assert.AreEqual(i, n.index);
                    if (n.type == NodeType.Fight || n.type == NodeType.Elite) combat++;
                    if (n.type == NodeType.Shop) shops++;
                    if (n.type == NodeType.Elite) elites++;
                    if (n.type == NodeType.SnackBreak) snacks++;
                }
                Assert.AreEqual(NodeType.Fight, map[act * cfg.nodesPerAct].type, $"seed {seed} act {act} first node");
                Assert.AreEqual(NodeType.Boss, map[act * cfg.nodesPerAct + cfg.nodesPerAct - 1].type, $"seed {seed} act {act} last node");
                Assert.GreaterOrEqual(combat, cfg.minCombatPerAct, $"seed {seed} act {act} combat");
                Assert.AreEqual(cfg.shopsPerAct, shops, $"seed {seed} act {act} shops");
                Assert.LessOrEqual(elites, cfg.maxElitePerAct);
                Assert.LessOrEqual(snacks, cfg.maxSnackPerAct);
            }
        }
    }

    [Test]
    public void Same_seed_gives_same_map() {
        var cfg = MakeConfig();
        Rng.Seed(1234); var a = MapGenerator.Generate(cfg);
        Rng.Seed(1234); var b = MapGenerator.Generate(cfg);
        for (int i = 0; i < a.Count; i++) Assert.AreEqual(a[i].type, b[i].type);
    }

    [Test]
    public void Map_stream_does_not_depend_on_combat_rolls() {
        var cfg = MakeConfig();
        Rng.Seed(99); var a = MapGenerator.Generate(cfg);
        Rng.Seed(99); for (int i = 0; i < 50; i++) DiceService.D20();   // burn combat dice first
        var b = MapGenerator.Generate(cfg);
        for (int i = 0; i < a.Count; i++) Assert.AreEqual(a[i].type, b[i].type);
    }
}
