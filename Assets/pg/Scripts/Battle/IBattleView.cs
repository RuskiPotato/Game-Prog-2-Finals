using System.Collections;

// Everything battle logic needs from the screen. BattleHud implements it; tests could fake it.
public interface IBattleView {
    void Log(string message);
    void SetGm(string line);
    IEnumerator Tumble(int diceCount);
    void ShowRoll(RollResult roll);
    void ShowReactionPanel(bool visible);
    void ShowContinue(bool visible);
}
