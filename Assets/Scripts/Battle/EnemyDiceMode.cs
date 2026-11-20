// How the enemy's side of the clash is decided. Pick it on BattleController.
public enum EnemyDiceMode {
    Visual,   // the hero's roll decides everything; enemy dice are cosmetic (hero misses = enemy lands)
    Real      // GDD rule: the enemy really rolls (1 + Malice dice, keep HIGHEST, + attack mod) vs the hero's Defense DC
}
