using UnityEngine;
using UnityEngine.UI;

// Put on a Button and type the scene name. Loads through GameManager.
[RequireComponent(typeof(Button))]
public class SceneButton : MonoBehaviour {
    public string sceneName = "MainMenu";

    void Start() {
        GetComponent<Button>().onClick.AddListener(() => GameManager.Instance.LoadScene(sceneName));
    }
}
