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
    [SerializeField] private Sprite x1SpeedSprite = default!; // 재생에도 사용됨
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
        currentSpeedButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
        pauseButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
        returnToLobbyButton.onClick.RemoveAllListeners();
    }


    public void UpdateSpeedUpButtonVisual(bool isSpeedUp, bool slowState)
    {
        if (slowState)
        {
            currentSpeedButton.interactable = false;
        }
        else
        {
            currentSpeedButton.interactable = true;
            
            // 현재 배속인 상태를 띄움
            currentSpeedText.text = isSpeedUp ? "x2" : "x1";
            currentSpeedImage.sprite = isSpeedUp ? x2SpeedSprite : x1SpeedSprite;
        }
    }

    public void UpdatePauseButtonVisual()
    {
        // 눌러서 바뀌는 상태를 띄움(정지 중일 때 재생, 재생 중일 때 정지)
        pauseImage.sprite = StageManager.Instance!.CurrentGameState == GameState.Paused ? x1SpeedSprite : pauseSprite;
    }

    private void OnReturnToLobbyButtonClicked()
    {
        Logger.Log("로비로 돌아가기 버튼이 클릭됨");

        // Pause패널이 나타났을 때에도 클릭될 수 있음
        if (StageManager.Instance!.CurrentGameState == GameState.Battle)
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
