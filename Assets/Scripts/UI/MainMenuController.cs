using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour {
    public Button newGameBtn, diceLabBtn, quitBtn;
    public Toggle skipPrologueToggle;
    void Start() {
        var gm = GameManager.Instance;
        newGameBtn.onClick.AddListener(() => {
            bool prologue = skipPrologueToggle == null || !skipPrologueToggle.isOn;
            gm.NewRun(prologue: prologue);
            gm.LoadScene(prologue ? "Battle" : "ExplorationMap");
        });
        if (diceLabBtn) diceLabBtn.onClick.AddListener(() => gm.LoadScene("DiceLab"));
        if (quitBtn) quitBtn.onClick.AddListener(Application.Quit);
    }
}
