using NUnit.Framework;

public class OddsTests {
    // +4 modifier vs DC 12 (need 8+ on the die): matches the Fate tier table
    [TestCase(1, 0.6500f)]
    [TestCase(2, 0.4225f)]
    [TestCase(3, 0.2746f)]
    [TestCase(4, 0.1785f)]
    [TestCase(5, 0.1160f)]
    public void HitChance_matches_tier_table(int dice, float expected) {
        Assert.AreEqual(expected, Odds.HitChance(12, 4, dice), 0.001f);
    }

    [Test]
    public void Tier_boundaries_follow_the_doc() {
        Assert.AreEqual(FateTier.Neutral,   FateState.TierOf(19));
        Assert.AreEqual(FateTier.Ire,       FateState.TierOf(20));
        Assert.AreEqual(FateTier.Ire,       FateState.TierOf(45));
        Assert.AreEqual(FateTier.Irritated, FateState.TierOf(46));
        Assert.AreEqual(FateTier.Furious,   FateState.TierOf(70));
        Assert.AreEqual(FateTier.Wrathful,  FateState.TierOf(90));
    }

    [Test]
    public void Total_dice_per_tier_are_1_2_3_4_5() {
        var f = new FateState();
        int[] fates = { 0, 20, 46, 70, 90 };
        for (int i = 0; i < fates.Length; i++) { f.Fate = fates[i]; Assert.AreEqual(i + 1, f.TotalDice); }
    }

    [Test]
    public void Same_seed_gives_same_rolls() {
        DiceService.Seed(42); var a = DiceService.RollMalice(4);
        DiceService.Seed(42); var b = DiceService.RollMalice(4);
        CollectionAssert.AreEqual(a.Dice, b.Dice);
    }

    [Test]
    public void Forced_miss_never_succeeds() {
        for (int i = 0; i < 1000; i++) {
            var r = DiceService.RollForcedMiss(4, 8);
            Assert.Less(r.Lowest, 8);
        }
    }

    [Test]
    public void Fate_is_locked_in_tutorial() {
        var f = new FateState();
        f.SetPhase(GamePhase.Tutorial);
        f.AddFate(50);
        Assert.AreEqual(0, f.Fate);
    }

    [Test]
    public void Encounter_decay_follows_the_doc() {
        var f = new FateState { Suspicion = 40 };
        f.EndEncounter(false, false); Assert.AreEqual(30, f.Suspicion);   // no cheating: -10
        f.EndEncounter(true, false);  Assert.AreEqual(25, f.Suspicion);   // cheated, no failed check: -5
        f.EndEncounter(true, true);   Assert.AreEqual(25, f.Suspicion);   // failed a check: no decay
    }

    [Test]
    public void Bargain_costs_two_gold_per_fate() {
        var f = new FateState { Fate = 70 };
        Assert.AreEqual(140, f.BargainCost);
    }
}
