using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class HoverZoomEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float scaleFactor = 1.1f;
    public float duration = 0.2f;

    private Vector3 originalScale;
    private Tween currentTween;

    void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        currentTween?.Kill(); // Stop any existing tween
        currentTween = transform.DOScale(originalScale * scaleFactor, duration).SetEase(Ease.OutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        currentTween?.Kill();
        currentTween = transform.DOScale(originalScale, duration).SetEase(Ease.OutQuad);
    }
}
