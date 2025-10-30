using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스테이지 씬의 Canvas를 관리하는 UI Manager
public class StageUIManager : MonoBehaviour
{
    public static StageUIManager? Instance { get; private set; }

    [Header("Canvas")]
    [SerializeField] private Canvas mainCanvas; 

    [Header("Panels")]
    [SerializeField] private GameObject statsPanelObject = default!; // 통계 패널
    [SerializeField] private GameObject gameOverPanelObject = default!;
    [SerializeField] private GameObject gameWinPanelObject = default!; // 여기는 애니메이션이 들어가니까 일단 냅두기만 합니다
    [SerializeField] private GameObject deploymentPanelObject = default!;
    [SerializeField] private GameObject progressPanelObject = default!; // 남은 적 수, 라이프 수
    [SerializeField] private GameObject bottomPanelObject = default!;
    [SerializeField] private GameObject infoPanelObject = default!; // 선택된 오퍼레이터 정보 패널
    [SerializeField] private GameObject stageResultPanelObject = default!;
    [SerializeField] private ConfirmationReturnToLobbyPanel confirmationReturnToLobbyPanel = default!;

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

    [Header("Operator UIs")]
    [SerializeField] protected GameObject operatorUIPrefab = default!;
    [SerializeField] protected GameObject directionIndicator = default!; // 오퍼레이터의 자식 오브젝트로 들어감
    public GameObject OperatorUIPrefab => operatorUIPrefab;
    public GameObject DirectionIndicator => directionIndicator;

    [Header("Top Sub Boxes")]
    [SerializeField] private GameObject passedEnemiesBox = default!;
    [SerializeField] private TextMeshProUGUI passedEnemiesText = default!;

    // 코스트 이펙트에서 사용할 좌표statsPanelObject
    private Vector3 costIconWorldPosition;
    public Vector3 CostIconWorldPosition
    {
        get => costIconWorldPosition;
        private set => costIconWorldPosition = value;
    }



    private InStageInfoPanel inStageInfoPanelScript = default!;
    public InStageInfoPanel InStageInfoPanel => inStageInfoPanelScript;

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

        // 비활성화 전에 참조를 넣어두기
        inStageInfoPanelScript = infoPanelObject.GetComponent<InStageInfoPanel>();

        // 최초에 꺼져야 할 패널들 비활성화
        gameOverPanelObject.SetActive(false);
        gameWinPanelObject.SetActive(false);
        infoPanelObject.SetActive(false); // 해당 스크립트의 Hide
        stageResultPanelObject.SetActive(false);
        confirmationReturnToLobbyPanel.gameObject.SetActive(false);

        // 스테이지 로딩 패널이 완료되고 캔버스가 나타나도록 수정
        // mainCanvas.gameObject.SetActive(false);

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
        StageManager.Instance!.OnEnemyPassed += UpdatePassedEnemiesBox;
        DeployableManager.Instance!.OnCurrentOperatorDeploymentCountChanged += UpdateLeftDeploymentCountText;
    }

    public void Initialize()
    {
        // mainCanvas.gameObject.SetActive(true);

        inGameTopButtonContainer!.Initialize();

        // 최초 카메라에서 코스트 아이콘의 월드 포지션을 잡아줌
        RectTransform costIconComponent = costIcon!.GetComponent<RectTransform>();
        CostIconWorldPosition = GetUIElementWorldProjection(costIconComponent);

        UpdateEnemyKillCountText();
        UpdateLifePointsText(StageManager.Instance!.CurrentLifePoints);
        UpdatePassedEnemiesBox(StageManager.Instance!.PassedEnemies);
        UpdateLeftDeploymentCountText();

        passedEnemiesBox.gameObject.SetActive(false);

        // DeployableManager.Instance.InitializeDeployableUI();

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


    // GameWin 패널을 띄우고 이를 클릭하면 결과로 넘어갑니다.
    public void ShowGameWinUI(int stars)
    {
        gameWinPanelObject.SetActive(true);
        GameWinPanel gameWinPanel = gameWinPanelObject.GetComponent<GameWinPanel>();

        // PlayAnimation 함수를 실행 후 종료를 기다린 뒤, ShowResultAfterDelay()를 실행함
        gameWinPanel.PlayAnimation(() =>
        {
            StartCoroutine(ShowResultAfterDelay(stars));
        });
    }


    // 스테이지 종료 후 결과 패널 활성화 
    public IEnumerator ShowResultAfterDelay(int stars)
    {
        yield return new WaitForSecondsRealtime(resultDelay); // Time.timeScale = 0이 되므로 이 메서드를 사용함

        stageResultPanelObject.SetActive(true);
        StageResultPanel stageResultPanel = stageResultPanelObject.GetComponent<StageResultPanel>();
        stageResultPanel.Initialize(stars);
    }


    // 배치되지 않은 유닛의 정보 보기 동작
    public void ShowUndeployedInfo(DeployableInfo deployableInfo)
    {
        inStageInfoPanelScript.UpdateUnDeployedInfo(deployableInfo);

        if (deployableInfo.prefab != null)
        {
            DeployableUnitEntity deployable = deployableInfo.prefab.GetComponent<DeployableUnitEntity>();
            CameraManager.Instance!.AdjustForDeployableInfo(true, deployable);
        }
    }


    // 배치된 유닛의 정보 보기 동작
    public void ShowDeployedInfo(DeployableUnitEntity deployableUnitEntity)
    {
        inStageInfoPanelScript.UpdateDeployedInfo(deployableUnitEntity);
        CameraManager.Instance!.AdjustForDeployableInfo(true, deployableUnitEntity);
    }

    public void HideDeployableInfo()
    {
        inStageInfoPanelScript.Hide();
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
        leftDeploymentCountText!.text = $"남은 배치 수 : {leftDeploymentCount}";
    }


    // UI 요소의 게임 화면상의 위치 위치
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

    public void ShowItemPopup(ItemUIElement itemUIElement)
    {
        stageItemInfoPopup.Show(itemUIElement);
    }

    public void HideItemPopup()
    {
        stageItemInfoPopup.Hide();
    }

    public void UpdatePassedEnemiesBox(int passedEnemies)
    {
        if (passedEnemies == 0)
        {
            passedEnemiesBox.gameObject.SetActive(false);
            return;
        }

        passedEnemiesBox.gameObject.SetActive(true);
        passedEnemiesText.text = $"-{passedEnemies}";
    }

    public void HideInfoPanelIfDisplaying(DeployableUnitEntity entity)
    {
        if (inStageInfoPanelScript.IsCurrentlyDisplaying(entity))
        {
            HideDeployableInfo();
        }
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
