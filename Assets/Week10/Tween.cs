using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TweenPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] RectTransform panel;
    [SerializeField] RectTransform openTarget;
    [SerializeField] RectTransform closedTarget;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] float duration = 0.5f;

    [Header("Slider (handle moves by itself)")]
    [SerializeField] Slider slider;
    [SerializeField] float slideDuration = 2f;
    [SerializeField] Ease slideEase = Ease.InOutSine;
    [SerializeField] LoopType loopType = LoopType.Yoyo; 
    [SerializeField] bool lockSliderInput = true;    
    [SerializeField] bool openOnStart = true;  

    Sequence currentTween;
    Tween handleTween;

    void Awake()
    {
        panel.position = closedTarget.position;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (slider != null)
        {
            slider.value = slider.minValue;
            if (lockSliderInput) slider.interactable = false;
        }
    }

    void Start()
    {
        if (openOnStart) Open();
    }

    public void Open()
    {
        currentTween?.Kill();
        StopAutoSlide();

        slider.value = slider.minValue;
        canvasGroup.blocksRaycasts = true;

        currentTween = DOTween.Sequence().SetUpdate(true);
        currentTween.Append(panel.DOMove(openTarget.position, duration).SetEase(Ease.OutCubic));
        currentTween.Join(canvasGroup.DOFade(1f, duration));
        currentTween.OnComplete(() =>
        {
            canvasGroup.interactable = true;
            StartAutoSlide();
        });
        currentTween.OnKill(() => currentTween = null);
    }

    public void Close()
    {
        currentTween?.Kill();
        StopAutoSlide();

        canvasGroup.interactable = false;

        currentTween = DOTween.Sequence().SetUpdate(true);
        currentTween.Append(panel.DOMove(closedTarget.position, duration).SetEase(Ease.InCubic));
        currentTween.Join(canvasGroup.DOFade(0f, duration));
        currentTween.OnComplete(() => canvasGroup.blocksRaycasts = false);
        currentTween.OnKill(() => currentTween = null);
    }

    void StartAutoSlide()
    {
        StopAutoSlide();
        slider.value = slider.minValue;

        
        handleTween = slider.DOValue(slider.maxValue, slideDuration)
            .SetEase(slideEase)
            .SetLoops(-1, loopType)
            .SetUpdate(true);
    }

    void StopAutoSlide()
    {
        handleTween?.Kill();
        handleTween = null;
    }

    void OnDestroy()
    {
        currentTween?.Kill();
        handleTween?.Kill();
    }
}