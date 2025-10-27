using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

// 배치 가능한 요소들의 배치 로직을 담당함
public class DeployableManager : MonoBehaviour
{
    public static DeployableManager? Instance { get; private set; }

    // UI 관련 변수
    public GameObject DeployableBoxPrefab = default!;
    public RectTransform bottomPanel = default!;

    // Deployable 관련 변수
    private List<DeployableInfo> allDeployables = new List<DeployableInfo>(); 
    private Dictionary<DeployableInfo, DeployableBox> deployableUIBoxes = new Dictionary<DeployableInfo, DeployableBox>();

    // --- 상태 변수들 ---
    public DeployableInfo? CurrentDeployableInfo { get; private set; }
    public DeployableBox? CurrentDeployableBox { get; private set; }
    public GameObject? CurrentDeployableObject { get; private set; }
    public DeployableUnitEntity? CurrentDeployableEntity { get; private set; }
    private List<Tile> highlightedTiles = new List<Tile>();

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

    public int CurrentDeploymentOrder { get; private set; } = 0;

    private Tile? currentHoverTile;

    private List<DeployableUnitEntity> deployedItems = new List<DeployableUnitEntity>();

    // 엔티티 이름과 deployableInfo를 매핑하는 딕셔너리
    private static Dictionary<string, DeployableInfo> deployableInfoMap = new Dictionary<string, DeployableInfo>();

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
    }

    private void UpdateDeployableStateCooldown()
    {
        foreach (DeployableUnitState unitState in unitStates.Values)
        {
            unitState.UpdateCooldown();
        }
    }

    // 배치 가능한 타일을 하이라이트
    public void HighlightAvailableTiles()
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
        if (CurrentDeployableInfo?.operatorData != null)
        {
            return (tile.data.terrain == TileData.TerrainType.Ground && CurrentDeployableInfo.operatorData.canDeployOnGround) ||
                    (tile.data.terrain == TileData.TerrainType.Hill && CurrentDeployableInfo.operatorData.canDeployOnHill);
        }
        else if (CurrentDeployableInfo?.deployableUnitData != null)
        {
            return (tile.data.terrain == TileData.TerrainType.Ground && CurrentDeployableInfo.deployableUnitData.canDeployOnGround) ||
                    (tile.data.terrain == TileData.TerrainType.Hill && CurrentDeployableInfo.deployableUnitData.canDeployOnHill);
        }
        else
            return false;
    }

    // BottomPanelOperatorBox 마우스버튼 다운 시 작동, 배치하려는 오퍼레이터의 정보를 변수에 넣는다.
    public bool StartDeployableSelection(DeployableInfo deployableInfo)
    {
        ResetPlacement();
        SetCurrentDeployableInfo(deployableInfo);
        StageUIManager.Instance!.ShowUndeployedInfo(CurrentDeployableInfo);

        // 박스 선택 상태
        CurrentDeployableBox.Select();

        // 하이라이트 처리
        HighlightAvailableTiles();

        return true;
    }
    
    private void SetCurrentDeployableInfo(DeployableInfo deployableInfo)
    {
        if (deployableInfo != null)
        {
            CurrentDeployableInfo = deployableInfo;

            CurrentDeployableEntity = CurrentDeployableInfo.deployedOperator != null ?
                CurrentDeployableInfo.deployedOperator :
                CurrentDeployableInfo.deployedDeployable;
            if (CurrentDeployableInfo == null) throw new InvalidOperationException("CurrentDeployableInfo가 null임");

            CurrentDeployableBox = deployableUIBoxes[CurrentDeployableInfo];
            if (CurrentDeployableBox == null) throw new InvalidOperationException("CurrentDeployableBox가 null임");
        }
    }

    public void CreatePreviewDeployable()
    {
        InstanceValidator.ValidateInstance(CurrentDeployableInfo);

        CurrentDeployableObject = ObjectPoolManager.Instance.SpawnFromPool(CurrentDeployableInfo.poolTag, Vector3.zero, Quaternion.identity);
        CurrentDeployableEntity = CurrentDeployableObject.GetComponent<DeployableUnitEntity>();

        if (CurrentDeployableEntity is Operator op)
        {
            op.Initialize(CurrentDeployableInfo!);
        }
        else
        {
            CurrentDeployableEntity!.Initialize(CurrentDeployableInfo!);
        }

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

        // currentUIState = UIState.OperatorAction;
    }

    public void ShowDeployingUI(Vector3 position)
    {
        InstanceValidator.ValidateInstance(CurrentDeployableEntity);

        HideOperatorUIs();

        currentDeployingUI.transform.position = position;
        currentDeployingUI.gameObject.SetActive(true);
        currentDeployingUI.Initialize(CurrentDeployableEntity);
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
    }

    public void UpdatePreviewDeployable()
    {
        if (CurrentDeployableEntity == null) throw new InvalidOperationException("currentDeployable이 null임");

        Tile? hoveredTile = GetHoveredTile();

        // 배치 가능한 타일 위라면 타일 위치로 스냅
        if (hoveredTile != null && highlightedTiles.Contains(hoveredTile))
        {
            SetAboveTilePosition(CurrentDeployableEntity, hoveredTile);
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

            CurrentDeployableEntity.transform.position = cursorWorldPosition;
        }
    }

    public Tile? GetHoveredTile()
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

    public Vector3 DetermineDirection(Vector3 dragVector)
    {
        float angle = Mathf.Atan2(dragVector.y, dragVector.x) * Mathf.Rad2Deg;
        if (angle < 45 && angle >= -45) return Vector3.right;
        if (angle < 135 && angle >= 45) return Vector3.forward;
        if (angle >= 135 || angle < -135) return Vector3.left;
        return Vector3.back;
    }


    // 배치되는 경우 동작하는 메서드
    public void DeployDeployable(Tile tile, Vector3 placementDirection)
    {
        // 기본값을 Vector3.zero로 설정, 이 값이 들어오면 배치되지 않음
        if (placementDirection == Vector3.zero) return;

        InstanceValidator.ValidateInstance(CurrentDeployableEntity);
        InstanceValidator.ValidateInstance(CurrentDeployableInfo);
        InstanceValidator.ValidateInstance(StageManager.Instance);

        DeployableUnitState gameState = unitStates[CurrentDeployableInfo!];
        int cost = gameState.CurrentDeploymentCost;

        // 코스트 지불 가능 & 배치 가능 상태
        if (StageManager.Instance!.TryUseDeploymentCost(cost) && gameState.OnDeploy(CurrentDeployableEntity!))
        {
            if (CurrentDeployableEntity is Operator op)
            {
                op.Deploy(tile.transform.position);
                op.SetDirection(placementDirection);
                
                CurrentOperatorDeploymentCount++;
                if (CurrentDeployableBox != null)
                {
                    CurrentDeployableBox.Deselect();
                    CurrentDeployableBox = null;
                }
            }
            else
            {
                CurrentDeployableEntity!.Deploy(tile.transform.position);
            }
        }

        // 배치된 유닛 목록에 추가
        deployedItems.Add(CurrentDeployableEntity!);
        UpdateDeployableUI(CurrentDeployableInfo!);
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

        DeploymentInputHandler.Instance!.ResetState();

        // CurrentDeployableInfo 관련 변수들
        if (CurrentDeployableEntity != null)
        {
            // 미리보기 중일 때는 해당 오브젝트 반환
            if (CurrentDeployableEntity.IsPreviewMode)
            {
                ObjectPoolManager.Instance.ReturnToPool(CurrentDeployableInfo.poolTag, CurrentDeployableObject);
                Debug.Log($"[ResetPlacement] - {CurrentDeployableObject} 풀로 반환됨");
            }
            CurrentDeployableEntity = null;
        }
        if (CurrentDeployableBox != null)
        {
            CurrentDeployableBox.Deselect();
            CurrentDeployableBox = null;
        }

        CurrentDeployableObject = null;
        CurrentDeployableInfo = null;

        StageUIManager.Instance!.HideDeployableInfo();
        GameManagement.Instance!.TimeManager.UpdateTimeScale();

        ResetHighlights();
        HideOperatorUIs();
    }

    public void CancelPlacement()
    {
        ResetPlacement();
    }

    public void ResetHighlights()
    {
        foreach (Tile tile in highlightedTiles)
        {
            tile.HideAttackRange();
            tile.ResetHighlight();
        }
        highlightedTiles.Clear();
    }

    public void UpdatePreviewRotation(Vector3 placementDirection)
    {
        if (CurrentDeployableEntity != null)
        {
            CurrentDeployableEntity.transform.rotation = Quaternion.LookRotation(placementDirection);
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
        // 현재 선택된 ui와 기존 선택된 actionUI가 다른 경우라면 숨김
        if (currentActionUI != null && currentActionUI != ui)
        {
            // Destroy(currentActionUI);
            currentActionUI.gameObject.SetActive(false);
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
        HideOperatorUIs();
        ResetPlacement();
        ResetHighlights();

        // 박스들의 애니메이션 초기화
        foreach (var box in deployableUIBoxes.Values)
        {
            box.ResetAnimation();
        }
    }

    public bool CanPlaceOnTile(Tile tile)
    {
        return tile.CanPlaceDeployable() && // 배치 가능한 타일인가
            highlightedTiles.Contains(tile); // 하이라이트된 타일인가
    }

    // 타일 위에 배치되는 배치 가능한 요소의 위치 설정
    public void SetAboveTilePosition(DeployableUnitEntity deployable, Tile tile)
    {
        if (CurrentDeployableEntity is Operator op)
        {
            CurrentDeployableEntity.transform.position = tile.transform.position + Vector3.up * 0.5f;
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
}