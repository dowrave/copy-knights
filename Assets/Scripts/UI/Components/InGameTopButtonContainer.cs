using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class InGameTopButtonContainer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button returnToLobbyButton;
    [SerializeField] private Button currentSpeedButton;
    [SerializeField] private Button pauseButton;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI currentSpeedText;

    [Header("Image References")]
    [SerializeField] private Image exitImage;
    [SerializeField] private Image currentSpeedImage;
    [SerializeField] private Image pauseImage;

    [Header("Sprite From Resources")]
    [SerializeField] private Sprite x1SpeedSprite; // 재생에도 사용됨
    [SerializeField] private Sprite x2SpeedSprite;
    [SerializeField] private Sprite pauseSprite;

    private void Awake()
    {
        returnToLobbyButton.interactable = false;
        currentSpeedButton.interactable = false;
        pauseButton.interactable = false;
    }

    public void Initialize()
    {
        currentSpeedButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
        pauseButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
        returnToLobbyButton.onClick.RemoveAllListeners();

        currentSpeedButton.onClick.AddListener(StageManager.Instance.ToggleSpeedUp);
        pauseButton.onClick.AddListener(StageManager.Instance.TogglePause);
        returnToLobbyButton.onClick.AddListener(OnReturnToLobbyButtonClicked);

        StageManager.Instance.OnPreparationCompleted += ActivateButtons;
    }

    public void ActivateButtons()
    {
        returnToLobbyButton.interactable = true;
        currentSpeedButton.interactable = true;
        pauseButton.interactable = true;
    }


    public void UpdateSpeedUpButtonVisual()
    {
        // 현재 배속인 상태를 띄움
        currentSpeedText.text = StageManager.Instance.IsSpeedUp ? "x2" : "x1";
        currentSpeedImage.sprite = StageManager.Instance.IsSpeedUp ? x2SpeedSprite : x1SpeedSprite;
    }

    public void UpdatePauseButtonVisual()
    {
        // 눌러서 바뀌는 상태를 띄움(정지 중일 때 재생, 재생 중일 때 정지)
        pauseImage.sprite = StageManager.Instance.currentState == GameState.Paused ? x1SpeedSprite : pauseSprite;
    }

    private void OnReturnToLobbyButtonClicked()
    {
        // Pause패널이 나타났을 때에도 클릭될 수 있음
        if (StageManager.Instance.currentState == GameState.Battle)
        {
            StageManager.Instance.SetGameState(GameState.Paused);
        }

        UIManager.Instance.InitializeReturnToLobbyPanel();
    }

    private void OnDisable()
    {
        StageManager.Instance.OnPreparationCompleted -= ActivateButtons;
    }
}
