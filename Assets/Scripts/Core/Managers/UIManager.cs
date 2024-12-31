using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject statsPanelObject; // ��� �г�
    [SerializeField] private GameObject gameOverPanelObject;
    [SerializeField] private GameObject gameWinPanelObject; // ����� �ִϸ��̼��� ���ϱ� �ϴ� ���α⸸ �մϴ�
    [SerializeField] private GameObject deploymentCostPanelObject;
    [SerializeField] private GameObject topCenterPanelObject; // ���� �� ��, ������ ��
    [SerializeField] private GameObject bottomPanelObject;
    [SerializeField] private GameObject infoPanelObject; // ���õ� ���۷����� ���� �г�
    [SerializeField] private GameObject stageResultPanelObject; 

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

    [SerializeField] private float resultDelay = 0.5f;

    // �ڽ�Ʈ ����Ʈ���� ����� ��ǥstatsPanelObject
    private Vector3 costIconWorldPosition;
    public Vector3 CostIconWorldPosition 
    {
        get => costIconWorldPosition;
        private set => costIconWorldPosition = value;
    }

    // Awake�� ��� ������Ʈ�� �ʱ�ȭ ���� ����Ǿ �ٸ� ��ũ��Ʈ�� ������ �� �ֵ��� �Ѵ�. Ư�� UI�� Awake�� �� ��.
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

        // ��Ȱ��ȭ ���� ������ �־�δ� �� ����
        infoPanelScript = infoPanelObject.GetComponent<InfoPanel>();

        // �г� ��Ȱ��ȭ
        gameOverPanelObject.SetActive(false);
        gameWinPanelObject.SetActive(false);
        infoPanelObject.SetActive(false);
        stageResultPanelObject.SetActive(false);
    }

    private void Start()
    {
        InitializeListeners();


        // ���� ī�޶󿡼� �ڽ�Ʈ �������� ���� �������� �����
        RectTransform costIconComponent = costIcon.GetComponent<RectTransform>();
        CostIconWorldPosition = GetUIElementWorldProjection(costIconComponent);
    }

    private void InitializeListeners()
    {
        currentSpeedButton.onClick.RemoveAllListeners(); // ���� ������ ����
        pauseButton.onClick.RemoveAllListeners(); // ���� ������ ����

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
        gameOverPanelObject.SetActive(true);

        var clickHandler = gameOverPanelObject.GetComponent<Button>() ?? gameOverPanelObject.AddComponent<Button>();
        clickHandler.onClick.AddListener(() =>
        {
            StartCoroutine(ShowResultAfterDelay(true));
            gameOverPanelObject.SetActive(false);
        });
    }

    /// <summary>
    /// GameWin �г��� ���� �̸� Ŭ���ϸ� ����� �Ѿ�ϴ�.
    /// </summary>
    public void ShowGameWinUI()
    {
        gameWinPanelObject.SetActive(true);
        GameWinPanel gameWinPanel = gameWinPanelObject.GetComponent<GameWinPanel>();

        // PlayAnimation �Լ��� ���� �� ���Ḧ ��ٸ� ��, ShowResultAfterDelay()�� ������
        gameWinPanel.PlayAnimation(() =>
        {
            StartCoroutine(ShowResultAfterDelay(true));
        });
    }

    /// <summary>
    /// ���� �¸�/�й� �г��� ��Ÿ�� �Ŀ� ��� �г� Ȱ��ȭ
    /// </summary>
    private IEnumerator ShowResultAfterDelay(bool isCleared)
    {
        yield return new WaitForSecondsRealtime(resultDelay); // Time.timeScale = 0�� �ǹǷ� �� �޼��带 �����

        var resultData = new StageResultData
        {
            passedEnemies = StageManager.Instance.PassedEnemies,
            isCleared = isCleared,
            //operatorStats = StatisticsManager.Instance.GetAllOperatorStats()
        };

        stageResultPanelObject.SetActive(true);
        StageResultPanel stageResultPanel = stageResultPanelObject.GetComponent<StageResultPanel>();
        stageResultPanel.Initialize(resultData);
        
    }

    /// <summary>
    /// ��ġ���� ���� ������ �̸����� ����
    /// </summary>
    public void ShowUndeployedInfo(DeployableManager.DeployableInfo deployableInfo)
    {
        if (infoPanelScript != null)
        {
            infoPanelObject.SetActive(true);

            infoPanelScript.UpdateUnDeployedInfo(deployableInfo);

            DeployableUnitEntity deployable = deployableInfo.prefab.GetComponent<DeployableUnitEntity>();
            CameraManager.Instance.AdjustForDeployableInfo(true, deployable);
        }
    }

    /// <summary>
    /// ��ġ�� ������ �̸����� ����
    /// </summary>
    public void ShowDeployedInfo(DeployableUnitEntity deployableUnitEntity)
    {
        if (infoPanelScript != null)
        {
            infoPanelObject.SetActive(true);
            infoPanelScript.UpdateDeployedInfo(deployableUnitEntity);
            CameraManager.Instance.AdjustForDeployableInfo(true, deployableUnitEntity);
        }
    }

    public void HideDeployableInfo()
    {
        if (infoPanelObject != null)
        {
            infoPanelObject.SetActive(false);
            CameraManager.Instance.AdjustForDeployableInfo(false);
        }
    }

    public void UpdateEnemyKillCountText()
    {
        enemyCountText.text = $"{StageManager.Instance.KilledEnemyCount} / {StageManager.Instance.TotalEnemyCount}";
    }

    public void UpdateLifePointsText(int currentLifePoints)
    {
        lifePointsText.text = $"<color=#ff7485>{currentLifePoints}</color>";
    }

    public void UpdateSpeedUpButtonVisual()
    {
        currentSpeedText.text = StageManager.Instance.IsSpeedUp ? "2X" : "1X";
        currentSpeedIcon.text = StageManager.Instance.IsSpeedUp ? "����" : "��";
    }

    public void UpdatePauseButtonVisual()
    {
        pauseButtonText.text = (StageManager.Instance.currentState == GameState.Paused) ? "��" : "||";
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
    /// UI ����� ���� ȭ�� �󿡼��� ��ġ
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
        // UI ����� ��ũ�� ���� ��ǥ ���
        Vector2 screenPos = GetUIElementScreenPosition(uiElement);

        // ��ũ�� ��ǥ�� ���̷� ��ȯ
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        // 3. XZ ������ ������ ���
        float denominator = Vector3.Dot(ray.direction, Vector3.up);
        if (Mathf.Abs(denominator) > float.Epsilon)
        {
            float t = (projectionHeight - ray.origin.y) / ray.direction.y;
            Vector3 projectedPoint = ray.origin + ray.direction * t;

            Debug.Log($"UI Element '{uiElement.name}' projects to world position: {projectedPoint}");

            // ����� �ð�ȭ
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);
            Debug.DrawLine(Vector3.zero, projectedPoint, Color.blue, 1f);

            return projectedPoint;
        }

        Debug.LogWarning("Could not project UI element to world space - ray is parallel to XZ plane");
        return Vector3.zero;
    }
    
}
