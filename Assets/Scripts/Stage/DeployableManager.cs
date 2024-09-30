using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class DeployableManager : MonoBehaviour
{
    public static DeployableManager Instance { get; private set; }

    [System.Serializable] 
    public class DeployableInfo
    {
        public GameObject prefab;
        public int maxDeployCount;
        public int remainingDeployCount;
        public bool isUserOperator;
        public float redeployTime;
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
    private Dictionary<GameObject, DeployableBox> deployableUIBoxes = new Dictionary<GameObject, DeployableBox>();
    private GameObject currentDeployablePrefab;
    private DeployableUnitEntity currentDeployable;

    // 하이라이트 관련 변수 - 인스펙터에서 설정
    public Color availableTileColor;
    public Color attackRangeTileColor;

    // 배치 과정 중 어떤 상태인지에 대한 변수
    private bool isDeployableSelecting = false; // 하단 UI에서 오퍼레이터를 클릭한 상태
    private bool isDraggingDeployable = false; // 타일 선택 상태 : 하단 UI에서 오퍼레이터를 MouseButtonDown한 상태로 드래그하고 있는 상태. 
    public bool IsDraggingDeployable => isDraggingDeployable;
    private bool isSelectingDirection = false; // 방향 선택 상태 : 타일은 정해졌고 오퍼레이터의 방향을 설정함
    public bool IsSelectingDirection => isSelectingDirection;

    private bool isMousePressed = false; // HandleDirectionSelection에서만 사용. 마우스가 클릭 중인지를 추적한다. 
    private int DeployableIndex = -1; 
    private Vector3 placementDirection = Vector3.left;

    public int CurrentDeploymentOrder { get; private set; } = 0;

    public bool IsMousePressed
    {
        get { return isMousePressed; }
        set { isMousePressed = value; }
    }

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
    }

    private void Start()
    {
        InitializeAllDeployables();
        InitializeDeployableUI();
    }

    private void InitializeAllDeployables()
    {
        allDeployables.Clear();

        foreach (var operatorPrefab in UserSquadManager.Instance.GetUserSquad())
        {
            AddDeployableInfo(operatorPrefab, 1, true);
        }

        foreach (var stageDeployable in StageManager.Instance.GetStageDeployables())
        {
            AddDeployableInfo(stageDeployable.deployablePrefab, stageDeployable.maxDeployCount, false);
        }
    }

    private void AddDeployableInfo(GameObject prefab, int maxCount, bool isUserOperator)
    {
        DeployableUnitEntity deployable = prefab.GetComponent<DeployableUnitEntity>();
        if (deployable != null)
        {
            allDeployables.Add(new DeployableInfo
            {
                prefab = prefab,
                maxDeployCount = maxCount,
                remainingDeployCount = maxCount,
                isUserOperator = isUserOperator,
                redeployTime = deployable.currentStats.RedeployTime
            });
        }
        else
        {
            Debug.LogWarning($"유효하지 않은 deployable Prefab : {prefab.name}");
        }
    }

    /// <summary>
    /// DeployableBox에 Deployable 요소 프리팹들을 할당하는 과정
    /// </summary>
    private void InitializeDeployableUI()
    {
        foreach (var deployableInfo in allDeployables)
        {
            GameObject boxObject = Instantiate(DeployableBoxPrefab, bottomPanel);
            DeployableBox box = boxObject.GetComponent<DeployableBox>();

            if (box != null)
            {
                box.Initialize(deployableInfo.prefab);
                deployableUIBoxes[deployableInfo.prefab] = box;
                box.UpdateRemainingCount(deployableInfo.remainingDeployCount);
            }
        }
    }

    private void Update()
    {
        if (StageManager.Instance.currentState != GameState.Battle) { return; }

        // 여기에는 해당 상태일 때 계속 작동하고 있어야 하는 함수가 들어감
        // !! 상태를 변경할 때에만 작동되어야 하는 함수는 여기에 들어가면 안됨! !! 

        // 1. 하단 UI의 오퍼레이터 클릭 시 배치 가능한 타일들 하이라이트
        if (isDeployableSelecting)
        {
            HighlightAvailableTiles();
        }
        // 2. 오퍼레이터를 드래그 중일 때 (타일 설정 상태)
        else if (isDraggingDeployable)
        {
            UpdatePreviewDeployable();
        }
        // 3. 오퍼레이터의 방향을 정할 때 (방향 설정 상태)
        else if (isSelectingDirection)
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
        if (currentDeployable is Operator op)
        {
            return (tile.data.terrain == TileData.TerrainType.Ground && op.Data.canDeployOnGround) ||
                    (tile.data.terrain == TileData.TerrainType.Hill && op.Data.canDeployOnHill);
        }
        else
        {
            return (tile.data.terrain == TileData.TerrainType.Ground && currentDeployable.Data.canDeployOnGround) ||
                    (tile.data.terrain == TileData.TerrainType.Hill && currentDeployable.Data.canDeployOnHill);
        }
    }


    // BottomPanelOperatorBox 마우스버튼 다운 시 작동, 배치하려는 오퍼레이터의 정보를 변수에 넣는다.
    public void StartDeployableSelection(GameObject deployablePrefab)
    {

        if (currentDeployablePrefab != deployablePrefab)
        {
            ResetPlacement();
            currentDeployablePrefab = deployablePrefab;
            currentDeployable = currentDeployablePrefab.GetComponent<DeployableUnitEntity>();
            isDeployableSelecting = true;

            UIManager.Instance.ShowDeployableInfo(currentDeployable);

            // Highlight available tiles
            HighlightAvailableTiles();
        }
    }

    /// <summary>
    /// BottomPanelDeployableBox 마우스버튼 다운 시 동작
    /// </summary>
    public void StartDragging(GameObject deployablePrefab)
    {
        if (currentDeployablePrefab == deployablePrefab)
        {
            isDeployableSelecting = false;
            isDraggingDeployable = true;
            CreatePreviewDeployable();
            StageManager.Instance.SlowDownTime();
        }
    }

    /// <summary>
    /// BottomPanelDeployableBox 마우스버튼 다운 후 드래그 시 동작
    /// </summary>
    public void HandleDragging(GameObject deployablePrefab)
    {
        if (isDraggingDeployable && currentDeployablePrefab == deployablePrefab)
        {
            UpdatePreviewDeployable();
        }
    }

    /// <summary>
    /// 배치되는 타일이 정해졌을 때 동작
    /// </summary>
    public void EndDragging(GameObject deployablePrefab)
    {

        if (isDraggingDeployable && currentDeployablePrefab == deployablePrefab)
        {
            isDraggingDeployable = false;
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
                    currentDeployable.Initialize(currentDeployable.Data);
                    DeployDeployable(hoveredTile);
                }
            }
            else
            {
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
                op.Initialize(op.Data);
            }
            else
            {
                currentDeployable.Initialize(currentDeployable.Data);
            }
        }
    }

    private void StartDirectionSelection(Tile tile)
    {
        isSelectingDirection = true;
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
                    isSelectingDirection = false;
                    IsMousePressed = false;
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
        DeployableInfo deployableInfo = allDeployables.Find(d => d.prefab == currentDeployablePrefab);
        if (deployableInfo == null)
        {
            Debug.LogError($"{deployableInfo}를 찾을 수 없음");
            return;
        }

        int nowDeploymentCost = currentDeployable.currentStats.DeploymentCost;
        if (StageManager.Instance.TryUseDeploymentCost(nowDeploymentCost))
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

            // 배치 리스트에 아이템 추가
            deployedItems.Add(currentDeployable); 

            // 배치 후 박스의 처리
            if (deployableUIBoxes.TryGetValue(currentDeployablePrefab, out DeployableBox box))
            {
                deployableInfo.remainingDeployCount--;
                
                box.UpdateRemainingCount(deployableInfo.remainingDeployCount);
                box.StartCooldown(deployableInfo.redeployTime);

                if (deployableInfo.remainingDeployCount <= 0)
                {
                    box.gameObject.SetActive(false);
                }
            }

            ResetPlacement();
            StageManager.Instance.UpdateTimeScale();
        }
    }

    /// <summary>
    /// 배치 관련 조작 전으로 상태를 되돌림
    /// </summary>
    private void ResetPlacement()
    {
        isDeployableSelecting = false;
        isDraggingDeployable = false;
        isSelectingDirection = false;
        isMousePressed = false;

        if (currentDeployable != null)
        {
            if (currentDeployable.IsPreviewMode)
            {
                Destroy(currentDeployable.transform.gameObject);
            }
            currentDeployable = null;
        }
        currentDeployablePrefab = null;

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

        GameObject prefab = deployable.Prefab;
        if (prefab != null && deployableUIBoxes.TryGetValue(prefab, out DeployableBox box))
        {
            box.gameObject.SetActive(true);
            
            // 일단 Operator는 제거됐을 때 재배치 쿨타임이 동작해야 함
            if (deployable is Operator op)
            {
                box.StartCooldown(op.currentStats.RedeployTime);
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

    public void ShowDeployableInfoPanel(DeployableUnitEntity deployable)
    {
        UIManager.Instance.ShowDeployableInfo(deployable);
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
    /// 오퍼레이터 위치 설정 후, 
    /// 마우스 버튼다운한 다음 배치를 위한 최소 드래그 길이(마우스 버튼업을 했을 때 배치되기 위한 최소 거리) 설정
    /// minDirectionDistance 값은 "스크린 상"에서의 길이가 된다.
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

    // deployableInfo 접근 메서드
    public DeployableInfo GetDeployableInfo(GameObject prefab)
    {
        return allDeployables.Find(d => d.prefab == prefab);
    }
}