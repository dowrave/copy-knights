using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// �������� Ŭ������� �г� ����
/// ���� �찡 ��Ÿ�� ��, ���� ���� �׽�Ʈ�� �� -> �·� �̵� 
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
    [SerializeField] private float textOffset = 1500f; // ȭ�� �۱����� �Ÿ�

    private Sequence? animationSequence;
    private System.Action? onAnimationComplete;

    private void Awake()
    {
        ResetElements();
    }

    public void PlayAnimation(System.Action? onComplete = null)
    {
        onAnimationComplete = onComplete; // �ݹ� �Լ� ����(����� �Լ��� ������ �����Ͻÿ�)

        // ���� �ִϸ��̼� ���� 
        if (animationSequence != null)
        {
            animationSequence.Kill();
        }

        animationSequence = DOTween.Sequence()
            .SetUpdate(true) // Time.timeScale�� ���������� ���۽�Ŵ
            .SetAutoKill();

        // 1. ���� �� ���̵� ��
        animationSequence.Append(backgroundStrip.DOFade(1f, stripFadeInDuration)); // ���⼭ �����ϴ� �� ���İ�

        // 2. �ؽ�Ʈ �����̵� �ִϸ��̼�
        animationSequence.Append(movingContainer.DOAnchorPosX(0, textSlideDuration)
            .SetEase(Ease.Linear)
            );

        // 3. �߾ӿ��� ��� ���
        animationSequence.AppendInterval(textPauseDuration);

        // 4. �ؽ�Ʈ �������� ����
        animationSequence.Append(movingContainer.DOAnchorPosX(-textOffset, textSlideDuration)
            .SetEase(Ease.Linear)
            );

        // 5. ���� �� ���̵� �ƿ�
        animationSequence.Append(backgroundStrip.DOFade(0, stripFadeOutDuration));

        // 6. �ִϸ��̼� �Ϸ� �� ����
        animationSequence.OnComplete(() =>
        {
            ResetElements();
            onAnimationComplete?.Invoke(); // �ݹ� �Լ� ����
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
