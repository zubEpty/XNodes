using DG.Tweening;
using TMPro;
using UnityEngine;

public class MessageInitializer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private float targetRectScaleDuration = 1f;
    [SerializeField] private Vector3 targetRectScale = Vector3.one;

    private void Start()
    {
        if (messageText == null)
        {
            messageText = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    public void ShowPopup(string message)
    {
        messageText.text = message;
        targetRect.DOScale(targetRectScale, targetRectScaleDuration).SetEase(Ease.OutBack).OnComplete(() =>
        {
            DOVirtual.DelayedCall(targetRectScaleDuration * 2f, () =>
            {
                targetRect.DOScale(Vector3.zero, targetRectScaleDuration).SetEase(Ease.InBack);
            });
        });
    }
}
