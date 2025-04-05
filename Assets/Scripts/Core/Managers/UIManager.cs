using System;
using System.Collections;
using NUnit.Framework.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// �������� ���� Canvas�� �����ϴ� UI Manager
public class UIManager : MonoBehaviour
{
    public static UIManager? Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject statsPanelObject = default!; // ��� �г�
    [SerializeField] private GameObject gameOverPanelObject = default!;
    [SerializeField] private GameObject gameWinPanelObject = default!; // ����� �ִϸ��̼��� ���ϱ� �ϴ� ���α⸸ �մϴ�
    [SerializeField] private GameObject deploymentPanelObject = default!;
    [SerializeField] private GameObject topCenterPanelObject = default!; // ���� �� ��, ������ ��
    [SerializeField] private GameObject bottomPanelObject = default!;
    [SerializeField] private GameObject infoPanelObject = default!; // ���õ� ���۷����� ���� �г�
    [SerializeField] private GameObject stageResultPanelObject = default!;
    [SerializeField] private ConfirmationReturnToLobbyPanel confirmationReturnToLobbyPanel = default!;

    private InStageInfoPanel inStageInfoPanelScript = default!;

    [Header("Button Container")]
    [SerializeField] private InGameTopButtonContainer inGameTopButtonContainer = default!;

    [Header("Top Panel Elements")]
    [SerializeField] private TextMeshProUGUI enemyCountText = default!;
    [SerializeField] private TextMeshProUGUI lifePointsText = default!;

    [Header("Top Left Button")]
    [SerializeField] private Button toLobbyButton = default!;

    [Header("Overlays")]
    [SerializeField] private Image pauseOverlay = default!;

    [Header("Cost Panel Elements")]
    [SerializeField] private GameObject costIcon = default!;

    [Header("Left Deployment Count Text")]
    [SerializeField] private TextMeshProUGUI leftDeploymentCountText = default!;

    [Header("Item Popup")]
    [SerializeField] private StageItemInfoPopup stageItemInfoPopup = default!;

    [Header("ResultPanel Appearance Delay")]
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

        // ��Ȱ��ȭ ���� ������ �־�α�
        inStageInfoPanelScript = infoPanelObject.GetComponent<InStageInfoPanel>();

        // �г� ��Ȱ��ȭ
        gameOverPanelObject.SetActive(false);
        gameWinPanelObject.SetActive(false);
        infoPanelObject.SetActive(false);
        stageResultPanelObject.SetActive(false);
        HideItemPopup();
    }

    private void Start()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnSpeedUpChanged += UpdateSpeedUpButtonVisual;
        }
    }

    private void InitializeListeners()
    {
        StageManager.Instance!.OnLifePointsChanged += UpdateLifePointsText;
        StageManager.Instance!.OnEnemyKilled += UpdateEnemyKillCountText;
        DeployableManager.Instance!.OnCurrentOperatorDeploymentCountChanged += UpdateLeftDeploymentCountText;
    }

    public void Initialize()
    {

        inGameTopButtonContainer!.Initialize();

        // ���� ī�޶󿡼� �ڽ�Ʈ �������� ���� �������� �����
        RectTransform costIconComponent = costIcon!.GetComponent<RectTransform>();
        CostIconWorldPosition = GetUIElementWorldProjection(costIconComponent);

        UpdateEnemyKillCountText();
        UpdateLifePointsText(StageManager.Instance!.CurrentLifePoints);
        UpdateLeftDeploymentCountText();

        InitializeListeners();
    }

    public void ShowGameOverUI()
    {
        gameOverPanelObject.SetActive(true);

        var clickHandler = gameOverPanelObject.GetComponent<Button>() ?? gameOverPanelObject.AddComponent<Button>();
        clickHandler.onClick.AddListener(() =>
        {
            StartCoroutine(ShowResultAfterDelay(0));
            gameOverPanelObject.SetActive(false);
        });
    }


    // GameWin �г��� ���� �̸� Ŭ���ϸ� ����� �Ѿ�ϴ�.
    public void ShowGameWinUI(int stars)
    {
        gameWinPanelObject.SetActive(true);
        GameWinPanel gameWinPanel = gameWinPanelObject.GetComponent<GameWinPanel>();

        // PlayAnimation �Լ��� ���� �� ���Ḧ ��ٸ� ��, ShowResultAfterDelay()�� ������
        gameWinPanel.PlayAnimation(() =>
        {
            StartCoroutine(ShowResultAfterDelay(stars));
        });
    }


    // �������� ���� �� ��� �г� Ȱ��ȭ 
    public IEnumerator ShowResultAfterDelay(int stars)
    {
        yield return new WaitForSecondsRealtime(resultDelay); // Time.timeScale = 0�� �ǹǷ� �� �޼��带 �����

        stageResultPanelObject.SetActive(true);
        StageResultPanel stageResultPanel = stageResultPanelObject.GetComponent<StageResultPanel>();
        stageResultPanel.Initialize(stars);
    }


    // ��ġ���� ���� ������ ���� ���� ����
    public void ShowUndeployedInfo(DeployableManager.DeployableInfo deployableInfo)
    {
        infoPanelObject.SetActive(true);
        inStageInfoPanelScript.UpdateInfo(deployableInfo, false);

        if (deployableInfo.prefab != null)
        {
            DeployableUnitEntity deployable = deployableInfo.prefab.GetComponent<DeployableUnitEntity>();
            CameraManager.Instance!.AdjustForDeployableInfo(true, deployable);
        }
        
    }


    // ��ġ�� ������ ���� ���� ����
    public void ShowDeployedInfo(DeployableUnitEntity deployableUnitEntity)
    {
        infoPanelObject.SetActive(true);
        inStageInfoPanelScript.UpdateInfo(deployableUnitEntity.DeployableInfo!, true);
        CameraManager.Instance!.AdjustForDeployableInfo(true, deployableUnitEntity);
        
    }

    public void HideDeployableInfo()
    {
        infoPanelObject.SetActive(false);
        CameraManager.Instance!.AdjustForDeployableInfo(false);
    }

    public void UpdateEnemyKillCountText()
    {
        enemyCountText.text = $"{StageManager.Instance!.KilledEnemyCount} / {StageManager.Instance!.TotalEnemyCount}";
    }

    public void UpdateLifePointsText(int currentLifePoints)
    {
        lifePointsText!.text = $"<color=#ff7485>{currentLifePoints}</color>";
    }

    public void UpdateSpeedUpButtonVisual(bool isSpeedUp)
    {
        inGameTopButtonContainer!.UpdateSpeedUpButtonVisual(isSpeedUp);
    }

    public void UpdatePauseButtonVisual()
    {
        inGameTopButtonContainer!.UpdatePauseButtonVisual();
    }

    public void ShowPauseOverlay()
    {
        if (pauseOverlay!.gameObject.activeSelf == false)
        {
            pauseOverlay!.gameObject.SetActive(true);
        }
    }

    public void HidePauseOverlay()
    {
        if (pauseOverlay!.gameObject.activeSelf == true)
        {
            pauseOverlay!.gameObject.SetActive(false);
        }
    }

    public void InitializeReturnToLobbyPanel()
    {
        confirmationReturnToLobbyPanel!.Initialize();
    }

    public void UpdateLeftDeploymentCountText()
    {
        int leftDeploymentCount = DeployableManager.Instance!.MaxOperatorDeploymentCount - DeployableManager.Instance!.CurrentOperatorDeploymentCount;
        leftDeploymentCountText!.text = $"���� ��ġ �� : {leftDeploymentCount}";
    }


    // UI ����� ���� ȭ����� ��ġ ��ġ
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

    public void ShowItemPopup(ItemUIElement itemUIElement)
    {
        stageItemInfoPopup.Show(itemUIElement);
    }

    public void HideItemPopup()
    {
        stageItemInfoPopup.Hide();
    }

    private void OnDisable()
    {
        DeployableManager.Instance!.OnCurrentOperatorDeploymentCountChanged -= UpdateLeftDeploymentCountText;

        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnSpeedUpChanged -= UpdateSpeedUpButtonVisual;
        }
    }
}
