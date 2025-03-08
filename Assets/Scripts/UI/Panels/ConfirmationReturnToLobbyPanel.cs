using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ConfirmationReturnToLobbyPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] Button blurArea = default!; // 빈 영역 클릭 시 로비로 돌아감
    [SerializeField] Button ConfirmButton = default!;
    [SerializeField] Button CancelButton = default!;

    private CanvasGroup canvasGroup = default!;
    private float animationSpeed = 0.01f; // DOFade의 실제 알파값에 영향을 준다. 왜 그런지는 모르겠음.

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

        // 멈춤 오버레이가 활성화된 경우 비활성화
        UIManager.Instance!.HidePauseOverlay();
        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, animationSpeed)
                .SetUpdate(true); // Time.timeScale 무시
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
