using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameWinPanel; // 여기는 애니메이션이 들어가니까 일단 냅두기만 합니다
    [SerializeField] private GameObject deploymentCostPanel;
    [SerializeField] private GameObject topCenterPanel; // 남은 적 수, 라이프 수
    [SerializeField] private GameObject bottomPanel;
    [SerializeField] private GameObject infoPanel;
    //[SerializeField] private GameObject overlayPanel;

    private InfoPanel infoPanelScript;

    // 상단 UI 요소
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI lifePointsText;

    // 우상단 UI 요소
    [SerializeField] private Button currentSpeedButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Image pauseOverlay;
    [SerializeField] private TextMeshProUGUI currentSpeedText;
    [SerializeField] private TextMeshProUGUI currentSpeedIcon;
    [SerializeField] private TextMeshProUGUI pauseButtonText;


    // Awake는 모든 오브젝트의 초기화 전에 실행되어서 다른 스크립트가 참조할 수 있도록 한다. 특히 UI는 Awake를 쓸 것.
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } 
        else
        {
            Destroy(gameObject);
        }


        // 인스펙터에서 할당하겠지만, 안전성을 위해
        if (gameOverPanel == null)
        {
            gameOverPanel = transform.Find("GameOverPanel").gameObject;
        }
        if (gameWinPanel == null)
        {
            gameWinPanel = transform.Find("GameWinPanel").gameObject;
        }
        if (infoPanel == null)
        {
            infoPanel = transform.Find("InfoPanel").gameObject;
        }

        // 비활성화 전에 참조를 넣어두는 게 좋다
        infoPanelScript = infoPanel.GetComponent<InfoPanel>();

        // 패널 비활성화
        gameOverPanel.SetActive(false);
        gameWinPanel.SetActive(false);
        infoPanel.SetActive(false);

        //InitializeListeners();

    }

    private void Start()
    {
        InitializeListeners();
    }

    private void InitializeListeners()
    {
        currentSpeedButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
        pauseButton.onClick.RemoveAllListeners(); // 기존 리스너 제거

        currentSpeedButton.onClick.AddListener(StageManager.Instance.ToggleSpeedUp);
        pauseButton.onClick.AddListener(StageManager.Instance.TogglePause);

        StageManager.Instance.OnLifePointsChanged += UpdateLifePointsText;
        StageManager.Instance.OnEnemyKilled += UpdateEnemyKillCountText;
    }

    public void InitializeUI()
    {
        UpdateEnemyKillCountText();
        UpdateLifePointsText(StageManager.Instance.CurrentLifePoints);
    }

    public void ShowGameOverUI()
    {
        gameOverPanel.SetActive(true);
    }
    public void ShowGameWinUI()
    {
        gameWinPanel.SetActive(true);
    }

    public void ShowDeployableInfo(DeployableUnitEntity deployable)
    {
        if (infoPanelScript != null)
        {
            infoPanel.SetActive(true);

            infoPanelScript.UpdateInfo(deployable);
            CameraManager.Instance.AdjustForDeployableInfo(true, deployable);
        }
    }

    public void HideDeployableInfo()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
            CameraManager.Instance.AdjustForDeployableInfo(false);
        }
    }

    public void UpdateEnemyKillCountText()
    {
        enemyCountText.text = $"{StageManager.Instance.KilledEnemyCount} / {StageManager.Instance.TotalEnemyCount}";
    }

    public void UpdateLifePointsText(int currentLifePoints)
    {
        lifePointsText.text = $"{currentLifePoints}";
    }

    public void UpdateSpeedUpButtonVisual()
    {
        currentSpeedText.text = StageManager.Instance.IsSpeedUp ? "2X" : "1X";
        currentSpeedIcon.text = StageManager.Instance.IsSpeedUp ? "▶▶" : "▶";
    }

    public void UpdatePauseButtonVisual()
    {
        pauseButtonText.text = (StageManager.Instance.currentState == GameState.Paused) ? "▶" : "||";
    }

    public void ShowPauseOverlay()
    {
        pauseOverlay.gameObject.SetActive(true);
    }

    public void HidePauseOverlay()
    {
        pauseOverlay.gameObject.SetActive(false);
    }

    
}
