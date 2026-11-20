using System;

// The enemy's dice in a clash are VISUAL ONLY. The real roll is the hero's; the exchange is decided first,
// then this makes enemy dice that look right: high when the enemy wins, low when the hero wins.
// Deliberately uses its own Random, NOT DiceService, so it never changes the seeded combat stream.
public static class EnemyFakeRoll {
    static readonly Random visual = new Random();

    // The enemy keeps its HIGHEST die. Returns the dice; keptIndex is the one to highlight.
    public static int[] Make(int count, bool enemyWins, out int keptIndex, Random rng = null) {
        var r = rng ?? visual;
        if (count < 1) count = 1;
        int kept = enemyWins ? r.Next(13, 21) : r.Next(2, 10);       // win: 13-20, lose: 2-9
        keptIndex = r.Next(0, count);
        var dice = new int[count];
        for (int i = 0; i < count; i++) dice[i] = i == keptIndex ? kept : r.Next(1, kept + 1);   // others never beat it
        return dice;
    }
}
