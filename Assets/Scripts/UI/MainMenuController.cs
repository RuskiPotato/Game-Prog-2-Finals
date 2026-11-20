using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour {
    public Button newGameBtn, diceLabBtn, quitBtn;
    public Toggle skipPrologueToggle;      // optional dev toggle: skips tutorial and scripted loss

    void Start() {
        var gm = GameManager.Instance;
        newGameBtn.onClick.AddListener(() => {
            bool prologue = skipPrologueToggle == null || !skipPrologueToggle.isOn;
            gm.NewRun(prologue: prologue);
            gm.LoadScene("Battle");
        });
        diceLabBtn.onClick.AddListener(() => gm.LoadScene("DiceLab"));
        quitBtn.onClick.AddListener(Application.Quit);
    }
}
