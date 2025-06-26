using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// ���� ��ܿ��� �����ߴٰ� ������� �˸� ǥ��
/// �˸��� �����̵� �� �ִϸ��̼����� ��Ÿ����
/// </summary>
public class NotificationToast : MonoBehaviour
{
    [SerializeField] private RectTransform panelRect = default!;
    [SerializeField] private TextMeshProUGUI messageText = default!;

    private Sequence? currentSequence;
    private Action? onClosedCallback;
    private bool isDismissing = false; // �ߺ� ȣ�� ���� �÷���

    // OnClosed : �ִϸ��̼��� ������ �ı��� �� ȣ��� �ݹ�
    public void Initialize(string message, float startY, Action OnClosed)
    {
        messageText.text = message;
        onClosedCallback = OnClosed;

        PlayShowAnimation(startY);
    }

    private void PlayShowAnimation(float startY)
    {
        currentSequence?.Kill();

        panelRect.anchoredPosition = new Vector2(400f, startY);

        currentSequence = DOTween.Sequence().SetUpdate(true)
            .Append(panelRect.DOAnchorPosX(-10f, 0.3f).SetEase(Ease.OutBack))
            .AppendInterval(2f)
            .Append(panelRect.DOAnchorPosX(400f, 0.3f).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                onClosedCallback?.Invoke();
                Destroy(gameObject);
            });
    }

    // �佺Ʋ�� ������ ������� �Ѵ�.
    public void Dismiss()
    {
        if (isDismissing) return;
        isDismissing = true;

        currentSequence?.Kill();

        // ������� �ִϸ��̼� ����
        DOTween.Sequence().SetUpdate(true)
            .Append(panelRect.DOAnchorPosX(400f, 0.1f).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                onClosedCallback?.Invoke();
                Destroy(gameObject);
            });
    }

    public void MoveToY(float targetY, float duration)
    {
        panelRect.DOAnchorPosY(targetY, duration).SetEase(Ease.OutQuad);
    }

    private void OnDestroy()
    {
        currentSequence?.Kill();
    }

}
