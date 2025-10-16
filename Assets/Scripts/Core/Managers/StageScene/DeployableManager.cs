using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

// 배치 가능한 요소들의 배치 로직을 담당함
public class DeployableManager : MonoBehaviour
{
    public static DeployableManager? Instance { get; private set; }
    private enum UIState
    {
        None,
        OperatorAction,
        OperatorDeploying
    }

    // 초기화 관련 정보들
    private UIState currentUIState = UIState.None;

    // UI 관련 변수
    public GameObject DeployableBoxPrefab = default!;
    public RectTransform bottomPanel = default!;

    // Deployable 관련 변수
    private List<DeployableInfo> allDeployables = new List<DeployableInfo>(); 
    private Dictionary<DeployableInfo, DeployableBox> deployableUIBoxes = new Dictionary<DeployableInfo, DeployableBox>();

    // 현재 선택된 박스에서 불러오는 정보들 - 일일이 관리한다는 번거로운 감은 있는데 그냥 이렇게 쓰겠음
    private DeployableInfo? currentDeployableInfo;
    private DeployableBox? currentDeployableBox;
    private GameObject? currentDeployableObject;
    private DeployableUnitEntity? currentDeployable;

    // 배치 가능 수와 현재 배치 수
    public int MaxOperatorDeploymentCount { get; private set; }
    private int _currentOperatorDeploymentCount = 0; 
    public int CurrentOperatorDeploymentCount
    {
        get { return _currentOperatorDeploymentCount; }
        private set
        {
            if (_currentOperatorDeploymentCount != value) // 값이 변경될 때에만
            {
                _currentOperatorDeploymentCount = value;
                OnCurrentOperatorDeploymentCountChanged?.Invoke();
            }
        }
    }
                                                    

    // 각 DeployableInfo에 대한 게임 상태를 관리
    private Dictionary<DeployableInfo, DeployableUnitState> unitStates = new Dictionary<DeployableInfo, DeployableUnitState>();
    public Dictionary<DeployableInfo, DeployableUnitState> UnitStates => unitStates;

    [Header("References")]
    [SerializeField] private LayerMask tileLayerMask = default!;
    [SerializeField] private DeployableDeployingUI? currentDeployingUI;
    [SerializeField] private DeployableActionUI? currentActionUI;
    [SerializeField] private Camera mainCamera; 

    [Header("Highlight Color")]
    // 하이라이트 관련 변수 - 인스펙터에서 설정
    public Color attackRangeTileColor;

    // 배치 과정 상태 변수
    public bool IsDeployableSelecting { get; private set; } = false; // 하단 UI에서 오퍼레이터를 클릭한 상태
    public bool IsDraggingDeployable { get; private set; } = false; 
    public bool IsSelectingDirection { get; private set; } = false;
    public bool IsMousePressed { get; set; } = false; 
    private Vector3 placementDirection = Vector3.left;

    public int CurrentDeploymentOrder { get; private set; } = 0;
    private List<Tile> highlightedTiles = new List<Tile>();
    private Tile? currentHoverTile;

    private float minDirectionDistance;

    private List<DeployableUnitEntity> deployedItems = new List<DeployableUnitEntity>();

    // 엔티티 이름과 deployableInfo를 매핑하는 딕셔너리
    private static Dictionary<string, DeployableInfo> deployableInfoMap = new Dictionary<string, DeployableInfo>();

    // 임시 클릭 방지 시간
    private float preventClickingTime = 0.1f;
    private float lastPlacementTime;

    public bool IsClickingPrevented => Time.time - lastPlacementTime < preventClickingTime;

    // 이벤트
    public event Action OnCurrentOperatorDeploymentCountChanged = delegate { };

     
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

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        OperatorIconHelper.OnIconDataInitialized += InitializeDeployableUI;
    }

    public void Initialize(
        List<SquadOperatorInfo> squadData,
        List<MapDeployableData> stageDeployables,
        int maxOperatorDeploymentCount
        )
    {
        allDeployables.Clear();
        deployableInfoMap.Clear();

        // OwnedOperator -> DeployableInfo로 변환해서 추가
        foreach (SquadOperatorInfo opInfo in squadData.Where(op => op != null))
        {
            OwnedOperator op = opInfo.op;

            var info = new DeployableInfo
            {
                prefab = op.OperatorProgressData.prefab,
                poolTag = op.OperatorProgressData.GetUnitTag(),
                maxDeployCount = 1,
                redeployTime = op.OperatorProgressData.stats.RedeployTime,
                ownedOperator = op,
                skillIndex = opInfo.skillIndex,
                operatorData = op.OperatorProgressData,
            };

            allDeployables.Add(info);

            InstanceValidator.ValidateInstance(op.OperatorProgressData);
            // (오퍼레이터 엔티티 이름 - 배치 정보) 매핑
            deployableInfoMap[op.OperatorProgressData!.entityName!] = info;
        }

        // 스테이지 제공 요소 -> DeployableInfo로 변환
        foreach (var deployable in stageDeployables)
        {
            var info = deployable.ToDeployableInfo();
            allDeployables.Add(info);

            InstanceValidator.ValidateInstance(deployable.DeployableData);

            // (배치 요소 이름 - 배치 정보) 매핑
            deployableInfoMap[deployable.DeployableData!.entityName!] = info; 
        }

        MaxOperatorDeploymentCount = maxOperatorDeploymentCount;

        InitializeDeployableUI();
    }

    // DeployableBox에 Deployable 요소 프리팹 할당
    private void InitializeDeployableUI()
    {
        if (DeployableBoxPrefab == null) throw new InvalidOperationException("deployableBoxPrefab이 할당되지 않음");

        foreach (var deployableInfo in allDeployables)
        {
            // deployableInfo에 대한 각 게임 상태 생성
            unitStates[deployableInfo] = new DeployableUnitState(deployableInfo);

            GameObject boxObject = Instantiate(DeployableBoxPrefab, bottomPanel);
            DeployableBox box = boxObject.GetComponent<DeployableBox>();

            // 이름 설정
            if (deployableInfo.operatorData != null)
            {
                box.gameObject.name = $"DeployableBox({deployableInfo.operatorData.entityName})";
            }
            else
            {
                box.gameObject.name = $"DeployableBox({deployableInfo.deployableUnitData.entityName})";
            }


            if (box != null)
            {
                box.Initialize(deployableInfo);
                deployableUIBoxes[deployableInfo] = box;
                box.UpdateVisuals();
            }
        }
    }

    private void Update()
    {
        if (StageManager.Instance == null) throw new InvalidOperationException("StageManager.Instance가 null임");

        if (StageManager.Instance.currentState != GameState.Battle) { return; }

        UpdateDeployableStateCooldown();

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

    private void UpdateDeployableStateCooldown()
    {
        foreach (DeployableUnitState unitState in unitStates.Values)
        {
            unitState.UpdateCooldown();
        }
    }

    // 배치 가능한 타일을 하이라이트
    private void HighlightAvailableTiles()
    {
        ResetHighlights();
        if (MapManager.Instance == null)
        {
            throw new InvalidOperationException("맵 매니저 인스턴스가 초기화되지 않았음");
        }


        foreach (Tile tile in MapManager.Instance.GetAllTiles())
        {
            if (tile != null && tile.CanPlaceDeployable())
            {
                if (CheckTileCondition(tile))
                {
                    highlightedTiles.Add(tile);
                    tile.Highlight();
                }
            }
        }
    }


    // 타일 조건 체크
    private bool CheckTileCondition(Tile tile)
    {
        if (currentDeployableInfo?.operatorData != null)
        {
            return (tile.data.terrain == TileData.TerrainType.Ground && currentDeployableInfo.operatorData.canDeployOnGround) ||
                    (tile.data.terrain == TileData.TerrainType.Hill && currentDeployableInfo.operatorData.canDeployOnHill);
        }
        else if (currentDeployableInfo?.deployableUnitData != null)
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
        Debug.Log("StartDeployableSelection 동작");
        ResetPlacement();

        SetCurrentDeployableInfo(deployableInfo);

        IsDeployableSelecting = true;

        StageUIManager.Instance!.ShowUndeployedInfo(currentDeployableInfo);

        // 박스 선택 상태
        currentDeployableBox.Select();

        HighlightAvailableTiles();
    }
    
    private void SetCurrentDeployableInfo(DeployableInfo deployableInfo)
    {
        if (deployableInfo != null)
        {
            currentDeployableInfo = deployableInfo;

            currentDeployable = currentDeployableInfo.deployedOperator != null ?
                currentDeployableInfo.deployedOperator :
                currentDeployableInfo.deployedDeployable;
            if (currentDeployableInfo == null) throw new InvalidOperationException("currentDeployableInfo가 null임");

            currentDeployableBox = deployableUIBoxes[currentDeployableInfo];
            if (currentDeployableBox == null) throw new InvalidOperationException("currentDeployableBox가 null임");
        }
    }

    // 하단 박스 마우스 버튼 다운 시 동작
    public void StartDragging(DeployableInfo deployableInfo)
    {
        if (currentDeployableInfo == deployableInfo)
        {
            IsDeployableSelecting = false;
            IsDraggingDeployable = true;
            CreatePreviewDeployable();
            //StageManager.Instance!.SlowDownTime();
            GameManagement.Instance!.TimeManager.SetPlacementTimeScale();
        }
    }

    // 하단 박스 드래그 시 동작
    public void HandleDragging(DeployableInfo deployableInfo)
    {
        if (IsDraggingDeployable && currentDeployableInfo == deployableInfo)
        {
            UpdatePreviewDeployable();
        }
    }

    // 하단 박스 드래그 중 커서를 뗐을 때의 동작
    public void EndDragging()
    {
        if (currentDeployable == null) throw new InvalidOperationException("currentDeployable이 null임");
        if (currentDeployableInfo == null) throw new InvalidOperationException("currentDeployableInfo이 null임");

        if (IsDraggingDeployable)
        {
            IsDraggingDeployable = false;
            Tile? hoveredTile = GetHoveredTile();

            if (hoveredTile != null && CanPlaceOnTile(hoveredTile))
            {
                // 방향 설정이 필요한 경우 방향 설정 단계로 진입
                if (currentDeployable is Operator)
                {
                    StartDirectionSelection(hoveredTile);
                }
                // 방향 설정이 필요 없다면 바로 배치
                else
                {
                    // currentDeployable.Initialize(currentDeployableInfo); // 중복인 듯?
                    DeployDeployable(hoveredTile);
                }
            }
            else
            {
                CancelDeployableSelection();
                StageUIManager.Instance!.HideDeployableInfo();
            }
        }
    }



    private void CreatePreviewDeployable()
    {
        InstanceValidator.ValidateInstance(currentDeployableInfo);

        currentDeployableObject = ObjectPoolManager.Instance.SpawnFromPool(currentDeployableInfo.poolTag, Vector3.zero, Quaternion.identity);
        Debug.Log($"{currentDeployableObject} 풀에서 꺼내 생성됨");
        currentDeployable = currentDeployableObject.GetComponent<DeployableUnitEntity>();

        if (currentDeployable is Operator op)
        {
            op.Initialize(currentDeployableInfo!);
        }
        else
        {
            currentDeployable!.Initialize(currentDeployableInfo!);
        }

    }

    private void StartDirectionSelection(Tile tile)
    {
        InstanceValidator.ValidateInstance(currentDeployable);

        IsSelectingDirection = true;
        ResetHighlights();
        currentHoverTile = tile;
        SetAboveTilePosition(currentDeployable!, tile);
        ShowDeployingUI(tile.transform.position + Vector3.up * 0.5f);
        UpdatePreviewRotation();
    }

    public void ShowActionUI(DeployableUnitEntity deployable)
    {
        HideOperatorUIs();

        // 위치 설정 후 활성화
        Vector3 ActionUIPosition = new Vector3(deployable.transform.position.x, 1f, deployable.transform.position.z);
        // currentActionUI = Instantiate(actionUIPrefab, ActionUIPosition, Quaternion.identity);
        currentActionUI.transform.position = ActionUIPosition;
        currentActionUI.gameObject.SetActive(true);
        currentActionUI.Initialize(deployable);

        currentUIState = UIState.OperatorAction;
    }

    public void ShowDeployingUI(Vector3 position)
    {
        InstanceValidator.ValidateInstance(currentDeployable);
        // InstanceValidator.ValidateInstance(deployingUIPrefab);

        HideOperatorUIs();

        currentDeployingUI.transform.position = position;
        currentDeployingUI.gameObject.SetActive(true);
        currentDeployingUI.Initialize(currentDeployable);

        currentUIState = UIState.OperatorDeploying;
    }


    // 오퍼레이터 주위에 나타난 UI 제거
    private void HideOperatorUIs()
    {
        // null이어도 상관 없음

        if (currentActionUI != null)
        {
            currentActionUI.gameObject.SetActive(false);
        }

        if (currentDeployingUI != null)
        {
            currentDeployingUI.gameObject.SetActive(false);
        }

        currentUIState = UIState.None;
    }

    private void HideOperatorUIsOnCondition(DeployableUnitEntity deployable)
    {
        if  (currentActionUI != null && currentActionUI.Deployable == deployable)
        {
            currentActionUI.gameObject.SetActive(false);
        }

        if (currentDeployingUI != null && currentDeployingUI.Deployable == deployable)
        {
            currentDeployingUI.gameObject.SetActive(false);
        }
    
        currentUIState = UIState.None;
    }


    // 방향 설정 관련 로직
    public void HandleDirectionSelection()
    {
        if (IsMousePressed)
        {
            ResetHighlights();

            if (currentHoverTile != null)
            {
                Vector3 dragVector = Input.mousePosition - mainCamera.WorldToScreenPoint(currentHoverTile.transform.position);
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
                    }
                    // 바운더리 이내라면 다시 방향 설정(클릭 X) 상태
                    else
                    {
                        IsMousePressed = false;
                        ResetHighlights();
                    }
                }
            }
        }

    }

    private void UpdatePreviewDeployable()
    {
        if (currentDeployable == null) throw new InvalidOperationException("currentDeployable이 null임");

        Tile? hoveredTile = GetHoveredTile();

        // 배치 가능한 타일 위라면 타일 위치로 스냅
        if (hoveredTile != null && highlightedTiles.Contains(hoveredTile))
        {
            SetAboveTilePosition(currentDeployable, hoveredTile);
        }
        // 배치 불가능한 타일이라면 커서 위치에 표시
        else
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            Vector3 cursorWorldPosition;

            if (groundPlane.Raycast(ray, out float distance))
            {
                cursorWorldPosition = ray.GetPoint(distance) + Vector3.up * 0.5f;
            }
            else
            {
                cursorWorldPosition = mainCamera.transform.position + mainCamera.transform.forward * 10f + Vector3.up * 0.5f;
            }

            currentDeployable.transform.position = cursorWorldPosition;
        }
    }

    private Tile? GetHoveredTile()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Tile")))
        {
            // return hit.collider.GetComponentInParent<Tile>();
            return hit.collider.GetComponent<Tile>();

        }

        return null;
    }

    public void HighlightAttackRanges(List<Tile> tiles, bool isMedic)
    {
        ResetHighlights();
        foreach (Tile tile in tiles)
        {
            tile.ShowAttackRange(isMedic);
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


    // 배치되는 경우 동작하는 메서드
    private void DeployDeployable(Tile tile)
    {
        InstanceValidator.ValidateInstance(currentDeployable);
        InstanceValidator.ValidateInstance(currentDeployableInfo);
        InstanceValidator.ValidateInstance(StageManager.Instance);

        DeployableUnitState gameState = unitStates[currentDeployableInfo!];
        int cost = gameState.CurrentDeploymentCost;

        // 코스트 지불 가능 & 배치 가능 상태
        if (StageManager.Instance!.TryUseDeploymentCost(cost) && gameState.OnDeploy(currentDeployable!))
        {
            if (currentDeployable is Operator op)
            {
                op.Deploy(tile.transform.position);
                op.SetDirection(placementDirection);
                
                CurrentOperatorDeploymentCount++;
                if (currentDeployableBox != null)
                {
                    currentDeployableBox.Deselect();
                    currentDeployableBox = null;
                }
            }
            else
            {
                currentDeployable!.Deploy(tile.transform.position);
            }
        }

        // 배치된 유닛 목록에 추가
        deployedItems.Add(currentDeployable!);
        UpdateDeployableUI(currentDeployableInfo!);
        ResetPlacement();
        GameManagement.Instance.TimeManager.UpdateTimeScale();
    }
    
    private void UpdateDeployableUI(DeployableInfo info)
    {
        if (deployableUIBoxes.TryGetValue(info, out DeployableBox box))
        {
            box.UpdateVisuals();
        }
    }


    // 배치 조작 전으로 상태를 되돌림
    private void ResetPlacement()
    {
        InstanceValidator.ValidateInstance(StageManager.Instance);
        InstanceValidator.ValidateInstance(StageUIManager.Instance);

        IsDeployableSelecting = false;
        IsDraggingDeployable = false;
        IsSelectingDirection = false;
        IsMousePressed = false;

        // currentDeployableInfo 관련 변수들
        if (currentDeployable != null)
        {
            // 미리보기 중일 때는 해당 오브젝트 파괴
            if (currentDeployable.IsPreviewMode)
            {
                ObjectPoolManager.Instance.ReturnToPool(currentDeployableInfo.poolTag, currentDeployableObject);
                Debug.Log($"[ResetPlacement] - {currentDeployableObject} 풀로 반환됨");
            }
            currentDeployable = null;
        }
        if (currentDeployableBox != null)
        {
            currentDeployableBox.Deselect();
            currentDeployableBox = null;
        }

        currentDeployableObject = null;
        currentDeployableInfo = null;

        StageUIManager.Instance!.HideDeployableInfo();
        GameManagement.Instance!.TimeManager.UpdateTimeScale();

        ResetHighlights();
        HideOperatorUIs();
    }

    // private void ResetPlacementWithObjectReturn()
    // {
    //     InstanceValidator.ValidateInstance(StageManager.Instance);
    //     InstanceValidator.ValidateInstance(StageUIManager.Instance);

    //     IsDeployableSelecting = false;
    //     IsDraggingDeployable = false;
    //     IsSelectingDirection = false;
    //     IsMousePressed = false;

    //     // currentDeployableInfo 관련 변수들
    //     if (currentDeployable != null)
    //     {
    //         // 미리보기 중일 때는 해당 오브젝트 되돌림
    //         if (currentDeployable.IsPreviewMode)
    //         {
    //             ObjectPoolManager.Instance.ReturnToPool(currentDeployableInfo.poolTag, currentDeployable.gameObject);
    //         }
    //         currentDeployable = null;
    //     }
    //     if (currentDeployableBox != null)
    //     {
    //         currentDeployableBox.Deselect();
    //         currentDeployableBox = null;
    //     }
    //     // currentDeployablePrefab = null;
    //     if (currentDeployableObject != null)
    //     {
    //         currentDeployableObject = null;
    //     }

    //     currentDeployableInfo = null;

    //     StageUIManager.Instance!.HideDeployableInfo();
    //     GameManagement.Instance!.TimeManager.UpdateTimeScale();

    //     ResetHighlights();
    //     HideOperatorUIs();
    // }

    public void CancelPlacement()
    {
        ResetPlacement();
    }

    private void ResetHighlights()
    {
        foreach (Tile tile in highlightedTiles)
        {
            tile.HideAttackRange();
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


    // 배치된 요소가 제거되었을 때 동작함
    public void OnDeployableRemoved(DeployableUnitEntity deployable)
    {
        deployedItems.Remove(deployable);

        // 현재 deployable에 대한 정보를 보여주고 있다면 숨김
        StageUIManager.Instance.HideInfoPanelIfDisplaying(deployable);
        HideOperatorUIsOnCondition(deployable);

        ResetHighlights();

        // 박스 재생성. 전투 중일 때에만 동작
        if (StageManager.Instance != null && StageManager.Instance.currentState == GameState.Battle)
        {
            DeployableInfo? info;
            if (deployable is Operator op)
            {
                CurrentOperatorDeploymentCount--;
                InstanceValidator.ValidateInstance(op.OperatorData);
                info = GetDeployableInfoByName(op.OperatorData?.entityName!);
            }
            else
            {
                InstanceValidator.ValidateInstance(deployable.DeployableUnitData);
                info = GetDeployableInfoByName(deployable.DeployableUnitData.entityName!);
            }

            if (info != null && unitStates.TryGetValue(info, out var unitState))
            {
                unitState.OnRemoved(); // 제거 시점에 상태 업데이트
            
                if (deployableUIBoxes.TryGetValue(info, out DeployableBox box))
                {
                    box.UpdateVisuals();
                    box.gameObject.SetActive(true);
                }
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

    // 배치 중이거나, 배치된 오퍼레이터를 클릭한 상태를 취소하는 동작
    public void CancelCurrentAction()
    {
        if (currentUIState != UIState.None) // Action이거나 Deploying일 때
        {
            HideOperatorUIs();
            ResetPlacement();
            ResetHighlights();

            // 박스들의 애니메이션 초기화
            foreach (var box in deployableUIBoxes.Values)
            {
                box.ResetAnimation();
            }
        }
    }

    // 배치 드래그 최소 길이
    public void SetMinDirectionDistance(float screenDiamondRadius)
    {
        minDirectionDistance = screenDiamondRadius / 2;
    }

    private bool CanPlaceOnTile(Tile tile)
    {
        return tile.CanPlaceDeployable() && // 배치 가능한 타일인가
            highlightedTiles.Contains(tile); // 하이라이트된 타일인가
    }

    // 타일 위에 배치되는 배치 가능한 요소의 위치 설정
    public void SetAboveTilePosition(DeployableUnitEntity deployable, Tile tile)
    {
        if (currentDeployable is Operator op)
        {
            currentDeployable.transform.position = tile.transform.position + Vector3.up * 0.5f;
            op.SetGridPosition();
        }
        else
        {
            deployable.transform.position = tile.transform.position + Vector3.up * 0.1f;
        }
    }

    public void UpdateDeploymentOrder()
    {
        CurrentDeploymentOrder += 1;
    }

    public DeployableInfo? GetDeployableInfoByName(string entityName)
    {
        return deployableInfoMap.TryGetValue(entityName, out var info) ? info : null;
    }


    private void OnDestroy()
    {
        OperatorIconHelper.OnIconDataInitialized -= InitializeDeployableUI;
    }

    [System.Serializable]
    public class DeployableInfo
    {
        public GameObject prefab = default!;
        public string poolTag;
        public int maxDeployCount = 0;
        public float redeployTime = 0f;

        // 오퍼레이터일 때 할당
        public Operator? deployedOperator;
        public OwnedOperator? ownedOperator;
        public OperatorData? operatorData;
        public int? skillIndex;

        // 일반 배치 가능한 유닛일 때 할당
        public DeployableUnitEntity? deployedDeployable;
        public DeployableUnitData? deployableUnitData;
    }
}