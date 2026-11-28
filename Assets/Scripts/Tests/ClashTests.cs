using NUnit.Framework;

public class ClashTests {
    [Test]
    public void Enemy_fake_dice_look_like_a_win_or_a_loss() {
        for (int i = 0; i < 500; i++) {
            int[] win = EnemyFakeRoll.Make(4, true, out int wi);
            Assert.GreaterOrEqual(win[wi], 13);
            foreach (int d in win) Assert.LessOrEqual(d, win[wi]);          // kept die is the highest

            int[] lose = EnemyFakeRoll.Make(4, false, out int li);
            Assert.LessOrEqual(lose[li], 9);
            foreach (int d in lose) Assert.LessOrEqual(d, lose[li]);
        }
    }

    [Test]
    public void Fake_roll_never_touches_the_combat_seed() {
        DiceService.Seed(7); int a = DiceService.D20();
        DiceService.Seed(7); EnemyFakeRoll.Make(5, true, out _); int b = DiceService.D20();
        Assert.AreEqual(a, b);
    }
}

public class EnemyRealRollTests {
    [Test]
    public void Real_enemy_roll_keeps_the_highest_and_is_seeded() {
        DiceService.Seed(11);
        int best = DiceService.RollHighestDice(4, out int[] dice, out int idx);
        Assert.AreEqual(4, dice.Length);
        Assert.AreEqual(best, dice[idx]);
        foreach (int d in dice) Assert.LessOrEqual(d, best);

        DiceService.Seed(11);
        int again = DiceService.RollHighestDice(4, out int[] dice2, out _);
        Assert.AreEqual(best, again);
        CollectionAssert.AreEqual(dice, dice2);
    }

    // Malice dice help the enemy: more dice = a higher chance to keep a high one
    [TestCase(10, 3)]
    [TestCase(12, 0)]
    public void More_dice_never_lower_the_enemy_chance(int dc, int mod) {
        float prev = 0f;
        for (int n = 1; n <= 5; n++) {
            float p = Odds.EnemyHitChance(dc, mod, n);
            Assert.GreaterOrEqual(p, prev);
            prev = p;
        }
    }
}
