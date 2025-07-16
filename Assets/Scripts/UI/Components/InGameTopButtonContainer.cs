using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class InGameTopButtonContainer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button returnToLobbyButton = default!;
    [SerializeField] private Button currentSpeedButton = default!;
    [SerializeField] private Button pauseButton = default!;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI currentSpeedText = default!;

    [Header("Image References")]
    //[SerializeField] private Image exitImage = default!;
    [SerializeField] private Image currentSpeedImage = default!;
    [SerializeField] private Image pauseImage = default!;

    [Header("Sprite From Resources")]
    [SerializeField] private Sprite x1SpeedSprite = default!; // ������� ����
    [SerializeField] private Sprite x2SpeedSprite = default!;
    [SerializeField] private Sprite pauseSprite = default!;

    private void Awake()
    {
        returnToLobbyButton.interactable = false;
        currentSpeedButton.interactable = false;
        pauseButton.interactable = false;
    }

    public void Initialize()
    {
        RemoveListeners();
        AddListeners();

        StageManager.Instance!.OnPreparationCompleted += ActivateButtons;
    }

    public void ActivateButtons()
    {
        returnToLobbyButton.interactable = true;
        currentSpeedButton.interactable = true;
        pauseButton.interactable = true;
    }

    private void AddListeners()
    {
        currentSpeedButton.onClick.AddListener(StageManager.Instance!.ToggleSpeedUp);
        pauseButton.onClick.AddListener(StageManager.Instance!.TogglePause);
        returnToLobbyButton.onClick.AddListener(OnReturnToLobbyButtonClicked);
    }

    private void RemoveListeners()
    {
        currentSpeedButton.onClick.RemoveAllListeners(); // ���� ������ ����
        pauseButton.onClick.RemoveAllListeners(); // ���� ������ ����
        returnToLobbyButton.onClick.RemoveAllListeners();
    }


    public void UpdateSpeedUpButtonVisual(bool isSpeedUp)
    {
        // ���� ����� ���¸� ���
        currentSpeedText.text = isSpeedUp ? "x2" : "x1";
        currentSpeedImage.sprite = isSpeedUp ? x2SpeedSprite : x1SpeedSprite;
    }

    public void UpdatePauseButtonVisual()
    {
        // ������ �ٲ�� ���¸� ���(���� ���� �� ���, ��� ���� �� ����)
        pauseImage.sprite = StageManager.Instance!.currentState == GameState.Paused ? x1SpeedSprite : pauseSprite;
    }

    private void OnReturnToLobbyButtonClicked()
    {
        Debug.Log("�κ�� ���ư��� ��ư�� Ŭ����");

        // Pause�г��� ��Ÿ���� ������ Ŭ���� �� ����
        if (StageManager.Instance!.currentState == GameState.Battle)
        {
            StageManager.Instance!.SetGameState(GameState.Paused);
        }

        StageUIManager.Instance!.InitializeReturnToLobbyPanel();
    }

    private void OnDisable()
    {
        StageManager.Instance!.OnPreparationCompleted -= ActivateButtons;
    }

    private void OnDestroy()
    {
        RemoveListeners(); 
    }
}
