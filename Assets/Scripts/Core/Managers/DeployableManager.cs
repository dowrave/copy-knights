using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 배치 가능한 요소들의 배치 로직을 담당함
/// </summary>
public class DeployableManager : MonoBehaviour
{
    public static DeployableManager Instance { get; private set; }

    [System.Serializable]
    public class DeployableInfo
    {
        public GameObject prefab;
        public int maxDeployCount;
        public float redeployTime;

        // 오퍼레이터일 때 할당
        public OwnedOperator? ownedOperator;
        public OperatorData? operatorData;

        // 일반 배치 가능한 유닛일 때 할당
        public DeployableUnitData? deployableUnitData;
    }

    private enum UIState
    {
        None,
        OperatorAction,
        OperatorDeploying
    }

    // 초기화 관련 정보들
    private UIState currentUIState = UIState.None;

    // UI 관련 변수
    public GameObject DeployableBoxPrefab;
    public RectTransform bottomPanel;

    // Deployable 관련 변수
    private List<DeployableInfo> allDeployables = new List<DeployableInfo>(); // 합친 거
    private Dictionary<DeployableInfo, DeployableBox> deployableUIBoxes = new Dictionary<DeployableInfo, DeployableBox>();
    private DeployableInfo currentDeployableInfo;
    private GameObject currentDeployablePrefab;
    private DeployableUnitEntity currentDeployable;

    // 각 DeployableInfo에 대한 게임 상태를 관리
    private Dictionary<DeployableInfo, DeployableGameState> gameStates = new Dictionary<DeployableInfo, DeployableGameState>();
    public Dictionary<DeployableInfo, DeployableGameState> GameStates => gameStates;


    [Header("Highlight Color")]
    // 하이라이트 관련 변수 - 인스펙터에서 설정
    public Color availableTileColor;
    public Color attackRangeTileColor;

    // 배치 과정 상태 변수
    public bool IsDeployableSelecting { get; private set; } = false; // 하단 UI에서 오퍼레이터를 클릭한 상태
    public bool IsDraggingDeployable { get; private set; } = false; 
    public bool IsSelectingDirection { get; private set; } = false;
    public bool IsMousePressed { get; set; } = false; 
    private int DeployableIndex = -1;
    private Vector3 placementDirection = Vector3.left;

    public int CurrentDeploymentOrder { get; private set; } = 0;
    private List<Tile> highlightedTiles = new List<Tile>();
    private Tile currentHoverTile;

    private float minDirectionDistance;
    public float MinDirectionDistance => minDirectionDistance;
    private const float INDICATOR_SIZE = 2.5f;

    [SerializeField] private LayerMask tileLayerMask;
    [SerializeField] private DeployableDeployingUI deployingUIPrefab;
    [SerializeField] private DeployableActionUI actionUIPrefab;

    private DeployableDeployingUI currentDeployingUI;
    private DeployableActionUI currentActionUI;

    private const float PLACEMENT_TIME_SCALE = 0.2f;
    private float originalTimeScale = 1f;

    private List<DeployableUnitEntity> deployedItems = new List<DeployableUnitEntity>();

    // 엔티티 이름과 deployableInfo를 매핑하는 딕셔너리
    private static Dictionary<string, DeployableInfo> deployableInfoMap = new Dictionary<string, DeployableInfo>();

    // 임시 클릭 방지 시간
    private float preventClickingTime = 0.1f;
    private float lastPlacementTime;
    public bool IsClickingPrevented => Time.time - lastPlacementTime < preventClickingTime;

    public event System.Action OnDeployableUIInitialized;

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

        OperatorIconHelper.OnIconDataInitialized += InitializeDeployableUI;
    }

    public void Initialize(
        List<OwnedOperator> squadData,
        List<MapDeployableData> stageDeployables
        )
    {
        allDeployables.Clear();
        deployableInfoMap.Clear();

        // OwnedOperator -> DeployableInfo로 변환해서 추가
        foreach (OwnedOperator op in squadData.Where(op => op != null))
        {
            var info = new DeployableInfo
            {
                prefab = op.BaseData.prefab,
                maxDeployCount = 1,
                redeployTime = op.BaseData.stats.RedeployTime,
                ownedOperator = op,
                operatorData = op.BaseData
            };

            allDeployables.Add(info);
            deployableInfoMap[op.BaseData.entityName] = info; // (오퍼레이터 엔티티 이름 - 배치 정보) 매핑
        }

        // 스테이지 제공 요소 -> DeployableInfo로 변환
        foreach (var deployable in stageDeployables)
        {
            var info = deployable.ToDeployableInfo();
            allDeployables.Add(info);
            deployableInfoMap[deployable.deployableData.entityName] = info; // (배치 요소 이름 - 배치 정보) 매핑
        }

        InitializeDeployableUI();
    }

    /// <summary>
    /// DeployableBox에 Deployable 요소 프리팹들을 할당하는 과정
    /// </summary>
    private void InitializeDeployableUI()
    {
        foreach (var deployableInfo in allDeployables)
        {
            // deployableInfo에 대한 각 게임 상태 생성
            gameStates[deployableInfo] = new DeployableGameState(deployableInfo);

            GameObject boxObject = Instantiate(DeployableBoxPrefab, bottomPanel);
            DeployableBox box = boxObject.GetComponent<DeployableBox>();

            if (box != null)
            {
                box.Initialize(deployableInfo);
                deployableUIBoxes[deployableInfo] = box;
                box.UpdateDisplay(gameStates[deployableInfo]);
                //box.UpdateRemainingCount(deployableInfo.maxDeployCount);
            }
        }
    }

    private void Update()
    {
        if (StageManager.Instance.currentState != GameState.Battle) { return; }

        // 여기에는 해당 상태일 때 계속 작동하고 있어야 하는 함수가 들어감
        // !! 상태를 변경할 때에만 작동되어야 하는 함수는 여기에 들어가면 안됨! !! 

        // 1. 하단 UI의 오퍼레이터 클릭 시 배치 가능한 타일들 하이라이트
        if (IsDeployableSelecting)
        {
            HighlightAvailableTiles();
        }
        // 2. 오퍼레이터를 드래그 중일 때 (타일 설정 상태)
        else if (IsDraggingDeployable)
        {
            UpdatePreviewDeployable();
        }
        // 3. 오퍼레이터의 방향을 정할 때 (방향 설정 상태)
        else if (IsSelectingDirection)
        {
            HandleDirectionSelection();
        }
    }

    private void HighlightAvailableTiles()
    {
        ResetHighlights();

        foreach (Tile tile in MapManager.Instance.GetAllTiles())
        {
            if (tile != null && tile.CanPlaceDeployable())
            {
                if (CheckTileCondition(tile))
                {
                    tile.Highlight(availableTileColor);
                    highlightedTiles.Add(tile);
                }
            }
        }
    }

    /// <summary>
    /// 타일 조건 체크
    /// </summary>
    private bool CheckTileCondition(Tile tile)
    {
        if (currentDeployableInfo.operatorData != null)
        {
            return (tile.data.terrain == TileData.TerrainType.Ground && currentDeployableInfo.operatorData.canDeployOnGround) ||
                    (tile.data.terrain == TileData.TerrainType.Hill && currentDeployableInfo.operatorData.canDeployOnHill);
        }
        else if (currentDeployableInfo.deployableUnitData != null)
        {
            return (tile.data.terrain == TileData.TerrainType.Ground && currentDeployableInfo.deployableUnitData.canDeployOnGround) ||
                    (tile.data.terrain == TileData.TerrainType.Hill && currentDeployableInfo.deployableUnitData.canDeployOnHill);
        }
        else 
            return false;
    }


    // BottomPanelOperatorBox 마우스버튼 다운 시 작동, 배치하려는 오퍼레이터의 정보를 변수에 넣는다.
    public void StartDeployableSelection(DeployableInfo deployableInfo)
    {
        ResetPlacement();

        if (currentDeployableInfo != deployableInfo)
        {    
            currentDeployableInfo = deployableInfo;
            currentDeployablePrefab = currentDeployableInfo.prefab;
            currentDeployable = currentDeployablePrefab.GetComponent<DeployableUnitEntity>();

            IsDeployableSelecting = true;

            UIManager.Instance.ShowUndeployedInfo(currentDeployableInfo);

            HighlightAvailableTiles();
        }
    }

    /// <summary>
    /// BottomPanelDeployableBox 마우스버튼 다운 시 동작
    /// </summary>
    public void StartDragging(DeployableInfo deployableInfo)
    {
        if (currentDeployableInfo == deployableInfo)
        {
            IsDeployableSelecting = false;
            IsDraggingDeployable = true;
            CreatePreviewDeployable();
            StageManager.Instance.SlowDownTime();
        }
    }

    /// <summary>
    /// BottomPanelDeployableBox 마우스버튼 다운 후 드래그 시 동작
    /// </summary>
    public void HandleDragging(DeployableInfo deployableInfo)
    {
        if (IsDraggingDeployable && currentDeployableInfo == deployableInfo)
        {
            UpdatePreviewDeployable();
        }
    }

    /// <summary>
    /// 드래그 중 커서를 뗐을 때의 동작
    /// </summary>
    public void EndDragging(GameObject deployablePrefab)
    {
        if (IsDraggingDeployable && currentDeployablePrefab == deployablePrefab)
        {
            Debug.Log("드래그 종료");
            IsDraggingDeployable = false;
            Tile hoveredTile = GetHoveredTile();

            if (hoveredTile && CanPlaceOnTile(hoveredTile))
            {
                // 방향 설정이 필요한 경우 방향 설정 단계로 진입
                if (currentDeployable is Operator)
                {
                    StartDirectionSelection(hoveredTile);
                }
                // 방향 설정이 필요 없다면 바로 배치
                else
                {
                    currentDeployable.Initialize(currentDeployable.BaseData);
                    DeployDeployable(hoveredTile);
                }
            }
            else
            {
                Debug.Log("배치 과정 취소");
                CancelDeployableSelection();
                UIManager.Instance.HideDeployableInfo();
            }
        }
    }

    private void CreatePreviewDeployable()
    {
        if (currentDeployablePrefab != null && currentDeployable != null)
        {
            GameObject deployableObject = Instantiate(currentDeployablePrefab);
            currentDeployable = deployableObject.GetComponent<DeployableUnitEntity>();

            if (currentDeployable is Operator op)
            {
                op.Initialize(currentDeployableInfo.ownedOperator);
            }
            else
            {
                currentDeployable.Initialize(currentDeployableInfo.deployableUnitData);
            }
        }
    }

    private void StartDirectionSelection(Tile tile)
    {
        IsSelectingDirection = true;
        ResetHighlights();
        currentHoverTile = tile;
        SetAboveTilePosition(currentDeployable, tile);
        ShowDeployingUI(tile.transform.position + Vector3.up * 0.5f);
        UpdatePreviewRotation();
    }

    public void ShowActionUI(DeployableUnitEntity deployable)
    {
        HideAllUIs();
        // 일관된 위치 구현하기
        Vector3 ActionUIPosition = new Vector3(deployable.transform.position.x, 1f, deployable.transform.position.z);
        currentActionUI = Instantiate(actionUIPrefab, ActionUIPosition, Quaternion.identity);
        currentActionUI.Initialize(deployable);
        currentUIState = UIState.OperatorAction;
    }

    public void ShowDeployingUI(Vector3 position)
    {
        HideAllUIs();
        currentDeployingUI = Instantiate(deployingUIPrefab, position, Quaternion.identity);
        currentDeployingUI.Initialize(currentDeployable);
        currentUIState = UIState.OperatorDeploying;

    }

    /// <summary>
    /// 오퍼레이터 주위에 나타난 ActionUI, DeployingUI 제거
    /// OperatorInfoPanel을 숨기는 건 별개의 메서드
    /// </summary>
    private void HideAllUIs()
    {
        if (currentActionUI != null)
        {
            Destroy(currentActionUI.gameObject);
            currentActionUI = null;
        }

        if (currentDeployingUI != null)
        {
            Destroy(currentDeployingUI.gameObject);
            currentDeployingUI = null;
        }

        currentUIState = UIState.None;
    }

    /// <summary>
    /// 방향 설정 관련 로직
    /// </summary>
    public void HandleDirectionSelection()
    {
        if (IsMousePressed)
        {
            ResetHighlights();

            Vector3 dragVector = Input.mousePosition - Camera.main.WorldToScreenPoint(currentHoverTile.transform.position);
            float dragDistance = dragVector.magnitude;
            Vector3 newDirection = DetermineDirection(dragVector);

            placementDirection = newDirection;

            if (currentDeployable != null && currentDeployable is Operator op)
            {
                op.SetDirection(placementDirection);
                op.HighlightAttackRange();
            }

            UpdatePreviewRotation();

            if (Input.GetMouseButtonUp(0))
            {
                // 일정 거리 이상 커서 이동 시 배치
                if (dragDistance > minDirectionDistance)
                {
                    DeployDeployable(currentHoverTile);
                    IsSelectingDirection = false;
                    IsMousePressed = false;
                    lastPlacementTime = Time.time; // 배치 시간 기록
                    ResetPlacement();
                }
                // 바운더리 이내라면 다시 방향 설정(클릭 X) 상태
                else
                {
                    IsMousePressed = false;
                    ResetHighlights();
                }
            }
        }
        //}
    }

    private void UpdatePreviewDeployable()
    {
        // 항상 커서 위치에 대한 월드 좌표 계산
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Vector3 cursorWorldPosition;

        if (groundPlane.Raycast(ray, out float distance))
        {
            cursorWorldPosition = ray.GetPoint(distance) + Vector3.up * 0.5f;
        }
        else
        {
            cursorWorldPosition = Camera.main.transform.position + Camera.main.transform.forward * 10f + Vector3.up * 0.5f;
        }

        Tile hoveredTile = GetHoveredTile();
        // 배치 가능한 타일 위라면 타일 위치로 스냅
        if (hoveredTile != null && highlightedTiles.Contains(hoveredTile))
        {
            SetAboveTilePosition(currentDeployable, hoveredTile);
        }
        else
        {
            // 아니라면 커서 위치에만 표시
            currentDeployable.transform.position = cursorWorldPosition;
        }
    }

    private Tile GetHoveredTile()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Tile")))
        {
            return hit.collider.GetComponentInParent<Tile>();
        }

        return null;
    }

    public void HighlightTiles(List<Tile> tiles, Color color)
    {
        ResetHighlights();
        foreach (Tile tile in tiles)
        {
            tile.Highlight(color);
            highlightedTiles.Add(tile);
        }
    }


    private Vector3 DetermineDirection(Vector3 dragVector)
    {
        float angle = Mathf.Atan2(dragVector.y, dragVector.x) * Mathf.Rad2Deg;
        if (angle < 45 && angle >= -45) return Vector3.right;
        if (angle < 135 && angle >= 45) return Vector3.forward;
        if (angle >= 135 || angle < -135) return Vector3.left;
        return Vector3.back;
    }

    /// <summary>
    /// Deploy를 쓸 경우 그것 이외의 처리도 함께 겸하는 이 메서드를 사용한다
    /// </summary>
    private void DeployDeployable(Tile tile)
    {
        DeployableGameState gameState = gameStates[currentDeployableInfo];
        int cost = gameState.CurrentDeploymentCost;

        // 코스트 지불 가능 & 배치 가능 상태
        if (StageManager.Instance.TryUseDeploymentCost(cost) && gameState.OnDeploy())
        {
            if (currentDeployable is Operator op)
            {
                op.Deploy(tile.transform.position);
                op.SetDirection(placementDirection);
            }
            else
            {
                currentDeployable.Deploy(tile.transform.position);
            }
        }

        // 배치된 유닛 목록에 추가
        deployedItems.Add(currentDeployable);
        UpdateDeployableUI(currentDeployableInfo);

        ResetPlacement();
        StageManager.Instance.UpdateTimeScale();

        // 배치 후 박스의 처리
        //if (deployableUIBoxes.TryGetValue(currentDeployableInfo, out DeployableBox box))
        //{
        //    box.UpdateRemainingCount(currentDeployableInfo.remainingDeployCount);
        //    box.StartCooldown(currentDeployableInfo.redeployTime);

        //    if (gameState.RemainingDeployCount <= 0)
        //    {
        //        box.gameObject.SetActive(false);
        //    }
        //}
    }
    
    private void UpdateDeployableUI(DeployableInfo info)
    {
        if (deployableUIBoxes.TryGetValue(info, out DeployableBox box))
        {
            var gameState = gameStates[info];
            box.UpdateDisplay(gameState);

            // 더 이상 배치할 수 없다면 박스 비활성화
            if (gameState.RemainingDeployCount <= 0)
            {
                box.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 배치 관련 조작 전으로 상태를 되돌림
    /// </summary>
    private void ResetPlacement()
    {
        IsDeployableSelecting = false;
        IsDraggingDeployable = false;
        IsSelectingDirection = false;
        IsMousePressed = false;

        if (currentDeployable != null)
        {
            if (currentDeployable.IsPreviewMode)
            {
                Destroy(currentDeployable.transform.gameObject);
            }
            currentDeployable = null;
        }
        currentDeployablePrefab = null;
        currentDeployableInfo = null;

        UIManager.Instance.HideDeployableInfo();
        StageManager.Instance.UpdateTimeScale(); // 시간 원상복구
        ResetHighlights();

        HideAllUIs();

    }

    public void CancelPlacement()
    {
        ResetPlacement();
    }

    private void ResetHighlights()
    {
        foreach (Tile tile in highlightedTiles)
        {
            tile.ResetHighlight();
        }
        highlightedTiles.Clear();
    }

    private void UpdatePreviewRotation()
    {
        if (currentDeployable != null)
        {
            currentDeployable.transform.rotation = Quaternion.LookRotation(placementDirection);
        }
    }

    /// <summary>
    /// 배치된 요소가 제거되었을 때 동작함
    /// </summary>
    public void OnDeployableRemoved(DeployableUnitEntity deployable)
    {
        deployedItems.Remove(deployable);
        UIManager.Instance.HideDeployableInfo();
        HideAllUIs();
        ResetHighlights();

        DeployableInfo info;
        if (deployable is Operator op)
        {
            info = GetDeployableInfoByName(op.BaseData.entityName);
        }
        else
        {
            info = GetDeployableInfoByName(deployable.BaseData.entityName);
        }

        if (info != null && gameStates.TryGetValue(info, out var gameState))
        {
            gameState.OnRemoved(); // 제거 시점에 상태 업데이트
            
            if (deployableUIBoxes.TryGetValue(info, out DeployableBox box))
            {
                box.UpdateDisplay(gameState);
                box.gameObject.SetActive(true);
            }
        }
    }

    public void SetActiveActionUI(DeployableActionUI ui)
    {
        // 현재 선택된 ui와 기존 선택된 actionUI가 다른 경우라면 숨김(자식 오브젝트라 숨김)
        if (currentActionUI != null && currentActionUI != ui)
        {
            Destroy(currentActionUI);
        }

        currentActionUI = ui;
    }

    public void CancelDeployableSelection()
    {
        CancelCurrentAction();
        ResetPlacement();
    }

    public void UpdateOperatorDirection(Operator op, Vector3 direction)
    {
        if (op != null)
        {
            op.SetDirection(direction);
        }
    }

    public void ShowDeployableInfoPanel(DeployableInfo deployableInfo)
    {
        UIManager.Instance.ShowUndeployedInfo(deployableInfo);
    }

    public void HideDeployableInfoPanel()
    {
        UIManager.Instance.HideDeployableInfo();
    }

    /// <summary>
    /// 배치 중이거나, 배치된 오퍼레이터를 클릭한 상태를 취소하는 동작
    /// </summary>
    public void CancelCurrentAction()
    {
        if (currentUIState != UIState.None) // Action이거나 Deploying일 때
        {
            HideAllUIs();
            ResetPlacement();
            ResetHighlights();
        }
    }

    /// <summary>
    /// 배치 드래그 최소 길이
    /// </summary>
    public void SetMinDirectionDistance(float screenDiamondRadius)
    {
        minDirectionDistance = screenDiamondRadius / 2;
    }

    private bool CanPlaceOnTile(Tile tile)
    {
        // 타일이 배치 가능한가? + 타일이 하이라이트되었는가? + 현재 선택한 객체를 그 타일에 배치할 수 잇는가?
        return tile.CanPlaceDeployable() &&
            highlightedTiles.Contains(tile);
    }

    // 타일 위에 배치되는 배치 가능한 요소의 위치 지정
    // 바리케이드가 붕 뜨길래 만들었음
    public void SetAboveTilePosition(DeployableUnitEntity deployable, Tile tile)
    {
        if (currentDeployable is Barricade barricade)
        {
            barricade.Transform.position = tile.transform.position + Vector3.up * 0.1f;
        }
        else
        {
            currentDeployable.transform.position = tile.transform.position + Vector3.up * 0.5f;
        }
    }

    public void UpdateDeploymentOrder()
    {
        CurrentDeploymentOrder += 1;
    }

    public DeployableInfo GetDeployableInfoByName(string entityName)
    {
        return deployableInfoMap.TryGetValue(entityName, out var info) ? info : null;
    }
}