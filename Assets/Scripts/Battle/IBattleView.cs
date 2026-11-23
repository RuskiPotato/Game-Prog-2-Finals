using System.Collections;

// Everything battle logic needs from the screen. BattleHud implements it; tests could fake it.
public interface IBattleView {
    void Log(string message);
    void SetGm(string line);
    IEnumerator Tumble(int diceCount);
    void ShowRoll(RollResult roll);
    IEnumerator TumbleEnemy(int diceCount);                  // clash: the enemy's dice are visual only
    void ShowEnemyRoll(int[] dice, int keptIndex);
    IEnumerator TumbleClash(int heroDiceCount, int enemyDiceCount);
    IEnumerator ClashApproach();
    IEnumerator ClashOutcome(bool heroWon, bool enemyWon);
    void ShowReactionPanel(bool visible);
    void ShowContinue(bool visible);
}
