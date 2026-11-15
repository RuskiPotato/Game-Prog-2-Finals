using NUnit.Framework;

public class BalanceTests {
    [Test]
    public void Hero_defense_is_seven_plus_agi() {
        Assert.AreEqual(12, Balance.HeroDefenseDc(5));
    }

    [Test]
    public void Scripted_loss_kills_exactly_on_the_final_round_even_when_defending() {
        foreach (bool defending in new[] { false, true }) {
            int hp = 30;
            for (int round = 1; round <= 4; round++) {
                hp -= Balance.ScriptedLossDamage(30, hp, 4, round, defending);
                if (round < 4) Assert.Greater(hp, 0, $"died early on round {round} (defending={defending})");
            }
            Assert.AreEqual(0, hp, $"defending={defending}");
        }
    }

    [Test]
    public void RollHighest_stays_in_range() {
        for (int i = 0; i < 500; i++) {
            int v = DiceService.RollHighest(3);
            Assert.GreaterOrEqual(v, 1);
            Assert.LessOrEqual(v, 20);
        }
    }
}
