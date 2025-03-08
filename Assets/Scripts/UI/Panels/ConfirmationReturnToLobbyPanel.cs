using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ConfirmationReturnToLobbyPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] Button blurArea = default!; // �� ���� Ŭ�� �� �κ�� ���ư�
    [SerializeField] Button ConfirmButton = default!;
    [SerializeField] Button CancelButton = default!;

    private CanvasGroup canvasGroup = default!;
    private float animationSpeed = 0.01f; // DOFade�� ���� ���İ��� ������ �ش�. �� �׷����� �𸣰���.

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Initialize()
    {
        if (StageManager.Instance!.currentState == GameState.Battle)
        {
            StageManager.Instance!.SetGameState(GameState.Paused);
        }

        // ���� �������̰� Ȱ��ȭ�� ��� ��Ȱ��ȭ
        UIManager.Instance!.HidePauseOverlay();
        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, animationSpeed)
                .SetUpdate(true); // Time.timeScale ����
        }
    }

    private void OnConfirmButtonClicked()
    {
        StageManager.Instance!.RequestExit();
    }

    private void OnCancelButtonClicked()
    {
        DisableThisPanel();
    }

    private void DisableThisPanel()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, animationSpeed)
                .SetUpdate(true);

            gameObject.SetActive(false);
        }

        //UIManager.Instance.HidePauseOverlay();
        StageManager.Instance!.SetGameState(GameState.Battle);
    }

    private void AddOnClickEventListeners()
    {
        ConfirmButton.onClick.AddListener(OnConfirmButtonClicked);
        CancelButton.onClick.AddListener(OnCancelButtonClicked);
        blurArea.onClick.AddListener(DisableThisPanel);
    }

    private void RemoveOnClickEventListeners()
    {
        ConfirmButton.onClick.RemoveAllListeners();
        CancelButton.onClick.RemoveAllListeners();
        blurArea.onClick.RemoveAllListeners();
    }

    private void OnEnable()
    {
        AddOnClickEventListeners();
    }

    private void OnDisable()
    {
        RemoveOnClickEventListeners();
    }
}
