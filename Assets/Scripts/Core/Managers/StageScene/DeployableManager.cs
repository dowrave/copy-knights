using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

// ��ġ ������ ��ҵ��� ��ġ ������ �����
public class DeployableManager : MonoBehaviour
{
    public static DeployableManager? Instance { get; private set; }
    private enum UIState
    {
        None,
        OperatorAction,
        OperatorDeploying
    }

    // �ʱ�ȭ ���� ������
    private UIState currentUIState = UIState.None;

    // UI ���� ����
    public GameObject DeployableBoxPrefab = default!;
    public RectTransform bottomPanel = default!;

    // Deployable ���� ����
    private List<DeployableInfo> allDeployables = new List<DeployableInfo>(); 
    private Dictionary<DeployableInfo, DeployableBox> deployableUIBoxes = new Dictionary<DeployableInfo, DeployableBox>();

    // ���� ���õ� �ڽ����� �ҷ����� ������ - ������ �����Ѵٴ� ���ŷο� ���� �ִµ� �׳� �̷��� ������
    private DeployableInfo? currentDeployableInfo;
    private DeployableBox? currentDeployableBox;
    private GameObject? currentDeployableObject;
    private DeployableUnitEntity? currentDeployable;

    // ��ġ ���� ���� ���� ��ġ ��
    public int MaxOperatorDeploymentCount { get; private set; }
    private int _currentOperatorDeploymentCount = 0; 
    public int CurrentOperatorDeploymentCount
    {
        get { return _currentOperatorDeploymentCount; }
        private set
        {
            if (_currentOperatorDeploymentCount != value) // ���� ����� ������
            {
                _currentOperatorDeploymentCount = value;
                OnCurrentOperatorDeploymentCountChanged?.Invoke();
            }
        }
    }
                                                    

    // �� DeployableInfo�� ���� ���� ���¸� ����
    private Dictionary<DeployableInfo, DeployableUnitState> unitStates = new Dictionary<DeployableInfo, DeployableUnitState>();
    public Dictionary<DeployableInfo, DeployableUnitState> UnitStates => unitStates;

    [Header("References")]
    [SerializeField] private LayerMask tileLayerMask = default!;
    [SerializeField] private DeployableDeployingUI? currentDeployingUI;
    [SerializeField] private DeployableActionUI? currentActionUI;
    [SerializeField] private Camera mainCamera; 

    [Header("Highlight Color")]
    // ���̶���Ʈ ���� ���� - �ν����Ϳ��� ����
    public Color attackRangeTileColor;

    // ��ġ ���� ���� ����
    public bool IsDeployableSelecting { get; private set; } = false; // �ϴ� UI���� ���۷����͸� Ŭ���� ����
    public bool IsDraggingDeployable { get; private set; } = false; 
    public bool IsSelectingDirection { get; private set; } = false;
    public bool IsMousePressed { get; set; } = false; 
    private Vector3 placementDirection = Vector3.left;

    public int CurrentDeploymentOrder { get; private set; } = 0;
    private List<Tile> highlightedTiles = new List<Tile>();
    private Tile? currentHoverTile;

    private float minDirectionDistance;

    private List<DeployableUnitEntity> deployedItems = new List<DeployableUnitEntity>();

    // ��ƼƼ �̸��� deployableInfo�� �����ϴ� ��ųʸ�
    private static Dictionary<string, DeployableInfo> deployableInfoMap = new Dictionary<string, DeployableInfo>();

    // �ӽ� Ŭ�� ���� �ð�
    private float preventClickingTime = 0.1f;
    private float lastPlacementTime;

    public bool IsClickingPrevented => Time.time - lastPlacementTime < preventClickingTime;

    // �̺�Ʈ
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

        // OwnedOperator -> DeployableInfo�� ��ȯ�ؼ� �߰�
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
            // (���۷����� ��ƼƼ �̸� - ��ġ ����) ����
            deployableInfoMap[op.OperatorProgressData!.entityName!] = info;
        }

        // �������� ���� ��� -> DeployableInfo�� ��ȯ
        foreach (var deployable in stageDeployables)
        {
            var info = deployable.ToDeployableInfo();
            allDeployables.Add(info);

            InstanceValidator.ValidateInstance(deployable.DeployableData);

            // (��ġ ��� �̸� - ��ġ ����) ����
            deployableInfoMap[deployable.DeployableData!.entityName!] = info; 
        }

        MaxOperatorDeploymentCount = maxOperatorDeploymentCount;

        InitializeDeployableUI();
    }

    // DeployableBox�� Deployable ��� ������ �Ҵ�
    private void InitializeDeployableUI()
    {
        if (DeployableBoxPrefab == null) throw new InvalidOperationException("deployableBoxPrefab�� �Ҵ���� ����");

        foreach (var deployableInfo in allDeployables)
        {
            // deployableInfo�� ���� �� ���� ���� ����
            unitStates[deployableInfo] = new DeployableUnitState(deployableInfo);

            GameObject boxObject = Instantiate(DeployableBoxPrefab, bottomPanel);
            DeployableBox box = boxObject.GetComponent<DeployableBox>();

            // �̸� ����
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
        if (StageManager.Instance == null) throw new InvalidOperationException("StageManager.Instance�� null��");

        if (StageManager.Instance.currentState != GameState.Battle) { return; }

        UpdateDeployableStateCooldown();

        // 1. �ϴ� UI�� ���۷����� Ŭ�� �� ��ġ ������ Ÿ�ϵ� ���̶���Ʈ
        if (IsDeployableSelecting)
        {
            HighlightAvailableTiles();
        }
        // 2. ���۷����͸� �巡�� ���� �� (Ÿ�� ���� ����)
        else if (IsDraggingDeployable)
        {
            UpdatePreviewDeployable();
        }
        // 3. ���۷������� ������ ���� �� (���� ���� ����)
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

    // ��ġ ������ Ÿ���� ���̶���Ʈ
    private void HighlightAvailableTiles()
    {
        ResetHighlights();
        if (MapManager.Instance == null)
        {
            throw new InvalidOperationException("�� �Ŵ��� �ν��Ͻ��� �ʱ�ȭ���� �ʾ���");
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


    // Ÿ�� ���� üũ
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


    // BottomPanelOperatorBox ���콺��ư �ٿ� �� �۵�, ��ġ�Ϸ��� ���۷������� ������ ������ �ִ´�.
    public void StartDeployableSelection(DeployableInfo deployableInfo)
    {
        Debug.Log("StartDeployableSelection ����");
        ResetPlacement();

        SetCurrentDeployableInfo(deployableInfo);

        IsDeployableSelecting = true;

        StageUIManager.Instance!.ShowUndeployedInfo(currentDeployableInfo);

        // �ڽ� ���� ����
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
            if (currentDeployableInfo == null) throw new InvalidOperationException("currentDeployableInfo�� null��");

            currentDeployableBox = deployableUIBoxes[currentDeployableInfo];
            if (currentDeployableBox == null) throw new InvalidOperationException("currentDeployableBox�� null��");
        }
    }

    // �ϴ� �ڽ� ���콺 ��ư �ٿ� �� ����
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

    // �ϴ� �ڽ� �巡�� �� ����
    public void HandleDragging(DeployableInfo deployableInfo)
    {
        if (IsDraggingDeployable && currentDeployableInfo == deployableInfo)
        {
            UpdatePreviewDeployable();
        }
    }

    // �ϴ� �ڽ� �巡�� �� Ŀ���� ���� ���� ����
    public void EndDragging()
    {
        if (currentDeployable == null) throw new InvalidOperationException("currentDeployable�� null��");
        if (currentDeployableInfo == null) throw new InvalidOperationException("currentDeployableInfo�� null��");

        if (IsDraggingDeployable)
        {
            IsDraggingDeployable = false;
            Tile? hoveredTile = GetHoveredTile();

            if (hoveredTile != null && CanPlaceOnTile(hoveredTile))
            {
                // ���� ������ �ʿ��� ��� ���� ���� �ܰ�� ����
                if (currentDeployable is Operator)
                {
                    StartDirectionSelection(hoveredTile);
                }
                // ���� ������ �ʿ� ���ٸ� �ٷ� ��ġ
                else
                {
                    // currentDeployable.Initialize(currentDeployableInfo); // �ߺ��� ��?
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
        Debug.Log($"{currentDeployableObject} Ǯ���� ���� ������");
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

        // ��ġ ���� �� Ȱ��ȭ
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


    // ���۷����� ������ ��Ÿ�� UI ����
    private void HideOperatorUIs()
    {
        // null�̾ ��� ����

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


    // ���� ���� ���� ����
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
                    // ���� �Ÿ� �̻� Ŀ�� �̵� �� ��ġ
                    if (dragDistance > minDirectionDistance)
                    {
                        DeployDeployable(currentHoverTile);
                        IsSelectingDirection = false;
                        IsMousePressed = false;
                        lastPlacementTime = Time.time; // ��ġ �ð� ���
                    }
                    // �ٿ���� �̳���� �ٽ� ���� ����(Ŭ�� X) ����
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
        if (currentDeployable == null) throw new InvalidOperationException("currentDeployable�� null��");

        Tile? hoveredTile = GetHoveredTile();

        // ��ġ ������ Ÿ�� ����� Ÿ�� ��ġ�� ����
        if (hoveredTile != null && highlightedTiles.Contains(hoveredTile))
        {
            SetAboveTilePosition(currentDeployable, hoveredTile);
        }
        // ��ġ �Ұ����� Ÿ���̶�� Ŀ�� ��ġ�� ǥ��
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


    // ��ġ�Ǵ� ��� �����ϴ� �޼���
    private void DeployDeployable(Tile tile)
    {
        InstanceValidator.ValidateInstance(currentDeployable);
        InstanceValidator.ValidateInstance(currentDeployableInfo);
        InstanceValidator.ValidateInstance(StageManager.Instance);

        DeployableUnitState gameState = unitStates[currentDeployableInfo!];
        int cost = gameState.CurrentDeploymentCost;

        // �ڽ�Ʈ ���� ���� & ��ġ ���� ����
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

        // ��ġ�� ���� ��Ͽ� �߰�
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


    // ��ġ ���� ������ ���¸� �ǵ���
    private void ResetPlacement()
    {
        InstanceValidator.ValidateInstance(StageManager.Instance);
        InstanceValidator.ValidateInstance(StageUIManager.Instance);

        IsDeployableSelecting = false;
        IsDraggingDeployable = false;
        IsSelectingDirection = false;
        IsMousePressed = false;

        // currentDeployableInfo ���� ������
        if (currentDeployable != null)
        {
            // �̸����� ���� ���� �ش� ������Ʈ �ı�
            if (currentDeployable.IsPreviewMode)
            {
                ObjectPoolManager.Instance.ReturnToPool(currentDeployableInfo.poolTag, currentDeployableObject);
                Debug.Log($"[ResetPlacement] - {currentDeployableObject} Ǯ�� ��ȯ��");
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

    //     // currentDeployableInfo ���� ������
    //     if (currentDeployable != null)
    //     {
    //         // �̸����� ���� ���� �ش� ������Ʈ �ǵ���
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


    // ��ġ�� ��Ұ� ���ŵǾ��� �� ������
    public void OnDeployableRemoved(DeployableUnitEntity deployable)
    {
        deployedItems.Remove(deployable);

        // ���� deployable�� ���� ������ �����ְ� �ִٸ� ����
        StageUIManager.Instance.HideInfoPanelIfDisplaying(deployable);
        HideOperatorUIsOnCondition(deployable);

        ResetHighlights();

        // �ڽ� �����. ���� ���� ������ ����
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
                unitState.OnRemoved(); // ���� ������ ���� ������Ʈ
            
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
        // ���� ���õ� ui�� ���� ���õ� actionUI�� �ٸ� ����� ����(�ڽ� ������Ʈ�� ����)
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

    // ��ġ ���̰ų�, ��ġ�� ���۷����͸� Ŭ���� ���¸� ����ϴ� ����
    public void CancelCurrentAction()
    {
        if (currentUIState != UIState.None) // Action�̰ų� Deploying�� ��
        {
            HideOperatorUIs();
            ResetPlacement();
            ResetHighlights();

            // �ڽ����� �ִϸ��̼� �ʱ�ȭ
            foreach (var box in deployableUIBoxes.Values)
            {
                box.ResetAnimation();
            }
        }
    }

    // ��ġ �巡�� �ּ� ����
    public void SetMinDirectionDistance(float screenDiamondRadius)
    {
        minDirectionDistance = screenDiamondRadius / 2;
    }

    private bool CanPlaceOnTile(Tile tile)
    {
        return tile.CanPlaceDeployable() && // ��ġ ������ Ÿ���ΰ�
            highlightedTiles.Contains(tile); // ���̶���Ʈ�� Ÿ���ΰ�
    }

    // Ÿ�� ���� ��ġ�Ǵ� ��ġ ������ ����� ��ġ ����
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

        // ���۷������� �� �Ҵ�
        public Operator? deployedOperator;
        public OwnedOperator? ownedOperator;
        public OperatorData? operatorData;
        public int? skillIndex;

        // �Ϲ� ��ġ ������ ������ �� �Ҵ�
        public DeployableUnitEntity? deployedDeployable;
        public DeployableUnitData? deployableUnitData;
    }
}