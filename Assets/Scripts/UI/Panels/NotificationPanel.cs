using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 우측 상단에서 등장했다가 사라지는 알림 표시
/// 알림은 슬라이드 인 애니메이션으로 나타난다
/// </summary>
public class NotificationPanel : MonoBehaviour
{
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private TextMeshProUGUI messageText;

    private Sequence currentSequence;

    public void Initialize(string message, System.Action? onClosedCallback = null)
    {
        messageText.text = message;
        PlayShowAnimation();
    }

    private void PlayShowAnimation()
    {
        if (currentSequence != null)
        {
            currentSequence.Kill();
        }

        currentSequence = DOTween.Sequence().SetUpdate(true)
            .Append(panelRect.DOAnchorPosX(-10f, 0.3f).SetEase(Ease.OutBack))
            .AppendInterval(2f)
            .Append(panelRect.DOAnchorPosX(400f, 0.3f).SetEase(Ease.InBack))
            .OnComplete(() =>
            {
                Destroy(gameObject);
            });
    }

    private void OnDestroy()
    {
        currentSequence?.Kill();
    }

}
