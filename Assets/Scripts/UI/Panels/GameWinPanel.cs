using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 클리어시의 패널 연출
/// 검은 띠가 나타난 후, 작전 종료 테스트가 우 -> 좌로 이동 
/// </summary>
public class GameWinPanel : MonoBehaviour
{
    [Header("Main Components")]
    [SerializeField] private Image backgroundStrip = default!;
    [SerializeField] private RectTransform movingContainer = default!;

    [Header("Animation Settings")]
    [SerializeField] private float stripFadeInDuration = 0.1f;
    [SerializeField] private float stripFadeOutDuration = 0.1f;
    [SerializeField] private float textSlideDuration = 0.5f;
    [SerializeField] private float textPauseDuration = 0.5f;
    [SerializeField] private float textOffset = 1500f; // 화면 밖까지의 거리

    private Sequence? animationSequence;
    private System.Action? onAnimationComplete;

    private void Awake()
    {
        ResetElements();
    }

    public void PlayAnimation(System.Action? onComplete = null)
    {
        onAnimationComplete = onComplete; // 콜백 함수 저장(실행될 함수는 참조를 참조하시오)

        // 이전 애니메이션 정리 
        if (animationSequence != null)
        {
            animationSequence.Kill();
        }

        animationSequence = DOTween.Sequence()
            .SetUpdate(true) // Time.timeScale과 독립적으로 동작시킴
            .SetAutoKill();

        // 1. 검은 띠 페이드 인
        animationSequence.Append(backgroundStrip.DOFade(1f, stripFadeInDuration)); // 여기서 조절하는 게 알파값

        // 2. 텍스트 슬라이드 애니메이션
        animationSequence.Append(movingContainer.DOAnchorPosX(0, textSlideDuration)
            .SetEase(Ease.Linear)
            );

        // 3. 중앙에서 잠시 대기
        animationSequence.AppendInterval(textPauseDuration);

        // 4. 텍스트 왼쪽으로 퇴장
        animationSequence.Append(movingContainer.DOAnchorPosX(-textOffset, textSlideDuration)
            .SetEase(Ease.Linear)
            );

        // 5. 검은 띠 페이드 아웃
        animationSequence.Append(backgroundStrip.DOFade(0, stripFadeOutDuration));

        // 6. 애니메이션 완료 후 정리
        animationSequence.OnComplete(() =>
        {
            ResetElements();
            onAnimationComplete?.Invoke(); // 콜백 함수 실행
            gameObject.SetActive(false);
        });
    }

    private void ResetElements()
    {
        backgroundStrip.color = new Color(0, 0, 0, 0);
        movingContainer.anchoredPosition = new Vector2(textOffset, 0);
    }

    private void OnDestroy()
    {
        animationSequence?.Kill();
    }
}
