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
    [SerializeField] private Sprite x1SpeedSprite; // ������� ����
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
        currentSpeedButton.onClick.RemoveAllListeners(); // ���� ������ ����
        pauseButton.onClick.RemoveAllListeners(); // ���� ������ ����
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
        // ���� ����� ���¸� ���
        currentSpeedText.text = StageManager.Instance.IsSpeedUp ? "x2" : "x1";
        currentSpeedImage.sprite = StageManager.Instance.IsSpeedUp ? x2SpeedSprite : x1SpeedSprite;
    }

    public void UpdatePauseButtonVisual()
    {
        // ������ �ٲ�� ���¸� ���(���� ���� �� ���, ��� ���� �� ����)
        pauseImage.sprite = StageManager.Instance.currentState == GameState.Paused ? x1SpeedSprite : pauseSprite;
    }

    private void OnReturnToLobbyButtonClicked()
    {
        // Pause�г��� ��Ÿ���� ������ Ŭ���� �� ����
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
