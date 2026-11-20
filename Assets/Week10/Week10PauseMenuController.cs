using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class Week10PauseMenuController : MonoBehaviour
{
    // These RectTransforms act as visual position markers. The panel moves between
    // their world positions, which keeps the setup easier to adjust for different screens.
    [Header("Responsive Positions")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private RectTransform openTarget;
    [SerializeField] private RectTransform closedTarget;

    // CanvasGroups control visibility, interaction, and whether the UI receives clicks.
    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup overlay;
    [SerializeField] private CanvasGroup menuGroup;
    [SerializeField] private CanvasGroup[] buttonGroups;

    // References used for the staggered button animation and status messages.
    [Header("Animated Controls")]
    [SerializeField] private Transform[] buttonTransforms;
    [SerializeField] private Button pauseButton;
    [SerializeField] private TMP_Text statusText;

    // Main animation duration and the delay between each button's entrance.
    [Header("Timing")]
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private float stagger = 0.08f;

    // Stores the active sequence so it can be stopped before a new one begins.
    private Sequence currentTween;
    private bool isOpen;

    private void Awake()
    {
        // Reset the demo to its normal gameplay state whenever the scene starts.
        Time.timeScale = 1f;
        panel.position = closedTarget.position;
        overlay.alpha = 0f;
        overlay.blocksRaycasts = false;
        menuGroup.alpha = 0f;
        menuGroup.interactable = false;
        menuGroup.blocksRaycasts = false;
        PrepareButtons();
        SetStatus("Gameplay running. Click PAUSE or press Escape.");
    }

    private void Update()
    {
        // Escape works as an alternative to the Pause and Resume buttons.
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isOpen)
                ClosePauseMenu();
            else
                OpenPauseMenu();
        }
    }

    public void OpenPauseMenu()
    {
        // Stop any unfinished closing animation before starting the opening animation.
        isOpen = true;

        // DOTween: Immediately stops the currently stored sequence, if one exists.
        currentTween?.Kill();

        // Reset all animated objects so the entrance always starts consistently.
        PrepareButtons();

        panel.position = closedTarget.position;
        overlay.alpha = 0f;
        menuGroup.alpha = 0f;
        menuGroup.interactable = false;
        menuGroup.blocksRaycasts = true;
        overlay.blocksRaycasts = true;
        pauseButton.interactable = false;
        Time.timeScale = 0f;
        SetStatus("Gameplay paused. DOTween continues with SetUpdate(true).");

        // DOTween: Creates the master opening sequence and allows it to run while timeScale is 0.
        currentTween = DOTween.Sequence().SetUpdate(true);

        // DOTween: Starts by fading the dark overlay from transparent to 72% opacity.
        currentTween.Append(overlay.DOFade(0.72f, 0.25f));

        // DOTween: Moves the panel to OpenTarget at the same time, slowing smoothly near the end.
        currentTween.Join(panel.DOMove(openTarget.position, duration).SetEase(Ease.OutCubic));

        // DOTween: Fades in the menu content alongside the panel movement.
        currentTween.Join(menuGroup.DOFade(1f, duration));

        // DOTween: Inserts the nested staggered button sequence 0.18 seconds after opening begins.
        currentTween.Insert(0.18f, CreateButtonIntro());

        // DOTween: Enables button interaction only after the complete opening sequence finishes.
        currentTween.OnComplete(() => menuGroup.interactable = true);

        // DOTween: Clears the stored reference whenever this sequence is killed.
        currentTween.OnKill(() => currentTween = null);
    }

    public void ClosePauseMenu()
    {
        // Stop any unfinished opening animation before starting the closing animation.
        isOpen = false;

        // DOTween: Immediately stops the currently stored sequence, if one exists.
        currentTween?.Kill();

        // Disable interaction while the menu is leaving the screen.
        menuGroup.interactable = false;
        SetStatus("Closing the menu before gameplay resumes...");

        // DOTween: Creates the master closing sequence and lets it run while timeScale is still 0.
        currentTween = DOTween.Sequence().SetUpdate(true);

        // DOTween: Moves the panel back to ClosedTarget, accelerating smoothly as it leaves.
        currentTween.Append(panel.DOMove(closedTarget.position, duration).SetEase(Ease.InCubic));

        // DOTween: Fades out the menu content at the same time as the panel movement.
        currentTween.Join(menuGroup.DOFade(0f, duration));

        // DOTween: Fades out the dark overlay alongside the other closing animations.
        currentTween.Join(overlay.DOFade(0f, duration));

        // DOTween: Runs this block only after the complete closing sequence finishes.
        currentTween.OnComplete(() =>
        {
            // Return input control and normal time to the gameplay scene.
            menuGroup.blocksRaycasts = false;
            overlay.blocksRaycasts = false;
            pauseButton.interactable = true;
            Time.timeScale = 1f;
            SetStatus("Gameplay running again. Time.timeScale = 1.");
        });

        currentTween.OnComplete(() => menuGroup.interactable = true);

        // DOTween: Clears the stored reference whenever this sequence is killed.
        currentTween.OnKill(() => currentTween = null);
    }

    public void ShowOptionsMessage()
    {
        SetStatus("OPTIONS selected. This button is included to demonstrate the stagger.");
    }

    public void ShowMainMenuMessage()
    {
        SetStatus("MAIN MENU selected. Scene loading is intentionally not included.");
    }

    private Sequence CreateButtonIntro()
    {
        // DOTween: Creates a separate sequence that will be nested inside the master sequence.
        Sequence buttons = DOTween.Sequence();

        // Give every button a slightly later start time to create the staggered effect.
        for (int i = 0; i < buttonTransforms.Length; i++)
        {   
            float time = i * stagger;

            // DOTween: Inserts this button's scale animation at its calculated stagger time.
            buttons.Insert(time,
                buttonTransforms[i]
                    // DOTween: Enlarges the button from its prepared 85% scale to full size.
                    .DOScale(1f, 0.2f)
                    // DOTween: Adds a small overshoot or "pop" before settling at full size.
                    .SetEase(Ease.OutBack));

            // DOTween: Fades in the same button at the same stagger time as its scale animation.
            buttons.Insert(time, buttonGroups[i].DOFade(1f, 0.2f));
        }

        // Return the nested sequence so it can be inserted into the opening sequence.
        return buttons;
    }

    private void PrepareButtons()
    {
        // Reset every button before replaying the staggered entrance animation.
        for (int i = 0; i < buttonTransforms.Length; i++)
        {
            buttonTransforms[i].localScale = Vector3.one * 0.85f;
            buttonGroups[i].alpha = 0f;
        }
    }

    private void SetStatus(string message)
    {
        // The null check keeps the demo functional even if no status Text is assigned.
        if (statusText != null)
            statusText.text = message;
    }
}
