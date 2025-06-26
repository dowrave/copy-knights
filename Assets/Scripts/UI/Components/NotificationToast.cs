using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 우측 상단에서 등장했다가 사라지는 알림 표시
/// 알림은 슬라이드 인 애니메이션으로 나타난다
/// </summary>
public class NotificationToast : MonoBehaviour
{
    [SerializeField] private RectTransform panelRect = default!;
    [SerializeField] private TextMeshProUGUI messageText = default!;

    private Sequence? currentSequence;
    private Action? onClosedCallback;
    private bool isDismissing = false; // 중복 호출 방지 플래그

    // OnClosed : 애니메이션이 끝나고 파괴될 때 호출될 콜백
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

    // 토스틀를 강제로 사라지게 한다.
    public void Dismiss()
    {
        if (isDismissing) return;
        isDismissing = true;

        currentSequence?.Kill();

        // 사라지는 애니메이션 실행
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
