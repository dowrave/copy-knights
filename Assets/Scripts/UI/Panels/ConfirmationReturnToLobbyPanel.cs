using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ConfirmationReturnToLobbyPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] Button blurArea; // �� ���� Ŭ�� �� �κ�� ���ư�
    [SerializeField] Button ConfirmButton;
    [SerializeField] Button CancelButton;

    CanvasGroup canvasGroup;
    private float animationSpeed = 0.01f; // DOFade�� ���� ���İ��� ������ �ش�. �� �׷����� �𸣰���.

    private void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        gameObject.SetActive(false);
    }

    public void Initialize()
    {
        StageManager.Instance.SetGameState(GameState.Paused);
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, animationSpeed);
        }
    }

    private void OnConfirmButtonClicked()
    {
        StageManager.Instance.RequestExit();
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
            canvasGroup.DOFade(0f, animationSpeed);

            gameObject.SetActive(false);
        }
        StageManager.Instance.SetGameState(GameState.Battle);
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
