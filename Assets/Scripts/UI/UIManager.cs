using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameWinPanel; // ����� �ִϸ��̼��� ���ϱ� �ϴ� ���α⸸ �մϴ�
    [SerializeField] private GameObject deploymentCostPanel;
    [SerializeField] private GameObject topCenterPanel; // ���� �� ��, ������ ��
    [SerializeField] private GameObject bottomPanel;
    [SerializeField] private GameObject infoPanel;
    //[SerializeField] private GameObject overlayPanel;

    private InfoPanel infoPanelScript;

    // ��� UI ���
    [SerializeField] private TextMeshProUGUI enemyCountText;
    [SerializeField] private TextMeshProUGUI lifePointsText;

    // ���� UI ���
    [SerializeField] private Button currentSpeedButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Image pauseOverlay;
    [SerializeField] private TextMeshProUGUI currentSpeedText;
    [SerializeField] private TextMeshProUGUI currentSpeedIcon;
    [SerializeField] private TextMeshProUGUI pauseButtonText;


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


        // �ν����Ϳ��� �Ҵ��ϰ�����, �������� ����
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

        // ��Ȱ��ȭ ���� ������ �־�δ� �� ����
        infoPanelScript = infoPanel.GetComponent<InfoPanel>();

        // �г� ��Ȱ��ȭ
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

    
}
