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


public class FortuneDieBalanceTests {
    [TestCase(2, 0.5443947f, 0.4295725f)]
    [TestCase(3, 0.4117002f, 0.2996783f)]
    [TestCase(4, 0.3395920f, 0.2333532f)]
    [TestCase(5, 0.2977080f, 0.1949619f)]
    public void Fortune_die_creates_the_expected_harsh_curve(
        int dice,
        float regularEnemyChance,
        float ogreChance) {
        Assert.AreEqual(
            regularEnemyChance,
            Odds.OpposedWinChance(4, dice, 1, dice, true),
            0.0001f
        );
        Assert.AreEqual(
            ogreChance,
            Odds.OpposedWinChance(4, dice, 3, dice, true),
            0.0001f
        );
    }

    [Test]
    public void Fortune_roll_keeps_the_higher_of_lowest_malice_and_fortune() {
        DiceService.Seed(12345);
        RollResult result = DiceService.RollMaliceWithFortune(4);

        Assert.AreEqual(5, result.Dice.Length);
        Assert.AreEqual(4, result.FortuneIndex);
        Assert.AreEqual(
            System.Math.Max(result.Lowest, result.Dice[result.FortuneIndex]),
            result.KeptValue
        );
        Assert.AreEqual(result.KeptValue, result.Dice[result.KeptIndex]);
    }

    [Test]
    public void Fortune_die_improves_non_opposed_hit_chance() {
        float withoutFortune = Odds.HitChance(12, 4, 3);
        float withFortune = Odds.HitChance(12, 4, 3, true);
        Assert.Greater(withFortune, withoutFortune);
    }
}
