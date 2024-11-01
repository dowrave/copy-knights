using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private IconData iconData;

    [Header("Panels")]
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameWinPanel; // 여기는 애니메이션이 들어가니까 일단 냅두기만 합니다
    [SerializeField] private GameObject deploymentCostPanel;
    [SerializeField] private GameObject topCenterPanel; // 남은 적 수, 라이프 수
    [SerializeField] private GameObject bottomPanel;
    [SerializeField] private GameObject infoPanel;
    //[SerializeField] private GameObject overlayPanel;

    private InfoPanel infoPanelScript;

    [Header("Top Panel Elements")]
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI lifePointsText;

    [Header("Top Right Panel Elements")]
    [SerializeField] private Button currentSpeedButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Image pauseOverlay;
    [SerializeField] private TextMeshProUGUI currentSpeedText;
    [SerializeField] private TextMeshProUGUI currentSpeedIcon;
    [SerializeField] private TextMeshProUGUI pauseButtonText;

    [Header("Cost Panel Elements")]
    [SerializeField] private GameObject costIcon;

    // 코스트 이펙트에서 사용할 좌표
    private Vector3 costIconWorldPosition;
    public Vector3 CostIconWorldPosition 
    {
        get => costIconWorldPosition;
        private set => costIconWorldPosition = value;
    }

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

        if (iconData == null)
        {
            Debug.Log("IconData가 할당되지 않음");
            return;
        }

        // Box 초기화가 Start 시점이라서 Awake 시점에서 생성
        IconHelper.Initialize(iconData);
    }

    private void Start()
    {
        InitializeListeners();


        // 최초 카메라에서 코스트 아이콘의 월드 포지션을 잡아줌
        RectTransform costIconComponent = costIcon.GetComponent<RectTransform>();
        CostIconWorldPosition = GetUIElementWorldProjection(costIconComponent);
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

    /// <summary>
    /// UI 요소의 게임 화면 상에서의 위치
    /// </summary>
    public Vector2 GetUIElementScreenPosition(RectTransform rectTransform)
    {
        Rect screenRect = GetScreenRect(rectTransform);
        return new Vector2(
            screenRect.x + screenRect.width * 0.5f,
            screenRect.y + screenRect.height * 0.5f
        );
    }
    private Rect GetScreenRect(RectTransform rectTransform)
    {
        Vector2 size = Vector2.Scale(rectTransform.rect.size, rectTransform.lossyScale);
        Rect rect = new Rect(0, 0, size.x, size.y);
        rect.center = rectTransform.position;
        return rect;
    }

    public Vector3 GetUIElementWorldProjection(RectTransform uiElement, float projectionHeight = 0f)
    {
        // UI 요소의 스크린 상의 좌표 얻기
        Vector2 screenPos = GetUIElementScreenPosition(uiElement);

        // 스크린 좌표를 레이로 변환
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        // 3. XZ 평면과의 교차점 계산
        float denominator = Vector3.Dot(ray.direction, Vector3.up);
        if (Mathf.Abs(denominator) > float.Epsilon)
        {
            float t = (projectionHeight - ray.origin.y) / ray.direction.y;
            Vector3 projectedPoint = ray.origin + ray.direction * t;

            Debug.Log($"UI Element '{uiElement.name}' projects to world position: {projectedPoint}");

            // 디버그 시각화
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);
            Debug.DrawLine(Vector3.zero, projectedPoint, Color.blue, 1f);

            return projectedPoint;
        }

        Debug.LogWarning("Could not project UI element to world space - ray is parallel to XZ plane");
        return Vector3.zero;
    }
    
}
