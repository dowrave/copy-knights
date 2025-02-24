using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ��ġ ������ ��ҵ��� ��ġ ������ �����
/// </summary>
public class DeployableManager : MonoBehaviour
{
    public static DeployableManager Instance { get; private set; }
    private enum UIState
    {
        None,
        OperatorAction,
        OperatorDeploying
    }

    // �ʱ�ȭ ���� ������
    private UIState currentUIState = UIState.None;

    // UI ���� ����
    public GameObject DeployableBoxPrefab;
    public RectTransform bottomPanel;

    // Deployable ���� ����
    private List<DeployableInfo> allDeployables = new List<DeployableInfo>(); 
    private Dictionary<DeployableInfo, DeployableBox> deployableUIBoxes = new Dictionary<DeployableInfo, DeployableBox>();

    // ���� ���õ� �ڽ����� �ҷ����� ������ - ������ �����Ѵٴ� ���ŷο� ���� �ִµ� �׳� �̷��� ������
    private DeployableInfo currentDeployableInfo;
    private DeployableBox currentDeployableBox;
    private GameObject currentDeployablePrefab;
    private DeployableUnitEntity currentDeployable;

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

    [SerializeField] private LayerMask tileLayerMask;
    [SerializeField] private DeployableDeployingUI deployingUIPrefab;
    [SerializeField] private DeployableActionUI actionUIPrefab;

    [Header("Highlight Color")]
    // ���̶���Ʈ ���� ���� - �ν����Ϳ��� ����
    public Color availableTileColor;
    public Color attackRangeTileColor;

    // ��ġ ���� ���� ����
    public bool IsDeployableSelecting { get; private set; } = false; // �ϴ� UI���� ���۷����͸� Ŭ���� ����
    public bool IsDraggingDeployable { get; private set; } = false; 
    public bool IsSelectingDirection { get; private set; } = false;
    public bool IsMousePressed { get; set; } = false; 
    private Vector3 placementDirection = Vector3.left;

    public int CurrentDeploymentOrder { get; private set; } = 0;
    private List<Tile> highlightedTiles = new List<Tile>();
    private Tile currentHoverTile;

    private float minDirectionDistance;

    private DeployableDeployingUI currentDeployingUI;
    private DeployableActionUI currentActionUI;

    private List<DeployableUnitEntity> deployedItems = new List<DeployableUnitEntity>();

    // ��ƼƼ �̸��� deployableInfo�� �����ϴ� ��ųʸ�
    private static Dictionary<string, DeployableInfo> deployableInfoMap = new Dictionary<string, DeployableInfo>();

    // �ӽ� Ŭ�� ���� �ð�
    private float preventClickingTime = 0.1f;
    private float lastPlacementTime;

    public bool IsClickingPrevented => Time.time - lastPlacementTime < preventClickingTime;

    public event System.Action OnDeployableUIInitialized;
    public event System.Action OnCurrentOperatorDeploymentCountChanged;


     
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
        List<MapDeployableData> stageDeployables,
        int maxOperatorDeploymentCount
        )
    {
        allDeployables.Clear();
        deployableInfoMap.Clear();

        // OwnedOperator -> DeployableInfo�� ��ȯ�ؼ� �߰�
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

            // (���۷����� ��ƼƼ �̸� - ��ġ ����) ����
            deployableInfoMap[op.BaseData.entityName] = info; 
        }

        // �������� ���� ��� -> DeployableInfo�� ��ȯ
        foreach (var deployable in stageDeployables)
        {
            var info = deployable.ToDeployableInfo();
            allDeployables.Add(info);

            // (��ġ ��� �̸� - ��ġ ����) ����
            deployableInfoMap[deployable.deployableData.entityName] = info; 
        }

        MaxOperatorDeploymentCount = maxOperatorDeploymentCount;

        InitializeDeployableUI();
    }

    // DeployableBox�� Deployable ��� ������ �Ҵ�
    private void InitializeDeployableUI()
    {
        foreach (var deployableInfo in allDeployables)
        {
            // deployableInfo�� ���� �� ���� ���� ����
            unitStates[deployableInfo] = new DeployableUnitState(deployableInfo);

            GameObject boxObject = Instantiate(DeployableBoxPrefab, bottomPanel);
            DeployableBox box = boxObject.GetComponent<DeployableBox>();

            if (box != null)
            {
                box.Initialize(deployableInfo);
                deployableUIBoxes[deployableInfo] = box;
                box.UpdateDisplay(unitStates[deployableInfo]);
            }
        }
    }

    private void Update()
    {
        if (StageManager.Instance.currentState != GameState.Battle) { return; }

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


    // Ÿ�� ���� üũ
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


    // BottomPanelOperatorBox ���콺��ư �ٿ� �� �۵�, ��ġ�Ϸ��� ���۷������� ������ ������ �ִ´�.
    public void StartDeployableSelection(DeployableInfo deployableInfo)
    {
        ResetPlacement();

        if (currentDeployableInfo != deployableInfo)
        {    
            currentDeployableInfo = deployableInfo;
            currentDeployableBox = deployableUIBoxes[currentDeployableInfo];
            currentDeployablePrefab = currentDeployableInfo.prefab;
            currentDeployable = currentDeployablePrefab.GetComponent<DeployableUnitEntity>();

            IsDeployableSelecting = true;
            
            UIManager.Instance.ShowUndeployedInfo(currentDeployableInfo);

            // �ڽ� ���� ����
            currentDeployableBox.Select();


            HighlightAvailableTiles();
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
            StageManager.Instance.SlowDownTime();
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
    public void EndDragging(GameObject deployablePrefab)
    {
        if (IsDraggingDeployable && currentDeployablePrefab == deployablePrefab)
        {;
            IsDraggingDeployable = false;
            Tile hoveredTile = GetHoveredTile();

            if (hoveredTile && CanPlaceOnTile(hoveredTile))
            {
                // ���� ������ �ʿ��� ��� ���� ���� �ܰ�� ����
                if (currentDeployable is Operator)
                {
                    StartDirectionSelection(hoveredTile);
                }
                // ���� ������ �ʿ� ���ٸ� �ٷ� ��ġ
                else
                {
                    currentDeployable.Initialize(currentDeployableInfo);
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
                op.Initialize(currentDeployableInfo);
            }
            else
            {
                currentDeployable.Initialize(currentDeployableInfo);
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
        HideUIs();
        // �ϰ��� ��ġ �����ϱ�
        Vector3 ActionUIPosition = new Vector3(deployable.transform.position.x, 1f, deployable.transform.position.z);
        currentActionUI = Instantiate(actionUIPrefab, ActionUIPosition, Quaternion.identity);
        currentActionUI.Initialize(deployable);
        currentUIState = UIState.OperatorAction;
    }

    public void ShowDeployingUI(Vector3 position)
    {
        HideUIs();
        currentDeployingUI = Instantiate(deployingUIPrefab, position, Quaternion.identity);
        currentDeployingUI.Initialize(currentDeployable);
        currentUIState = UIState.OperatorDeploying;
    }


    // ���۷����� ������ ��Ÿ�� UI ����
    private void HideUIs()
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

    // ���� ���� ���� ����
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
                // ���� �Ÿ� �̻� Ŀ�� �̵� �� ��ġ
                if (dragDistance > minDirectionDistance)
                {
                    DeployDeployable(currentHoverTile);
                    IsSelectingDirection = false;
                    IsMousePressed = false;
                    lastPlacementTime = Time.time; // ��ġ �ð� ���
                    ResetPlacement();
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

    private void UpdatePreviewDeployable()
    {
        // �׻� Ŀ�� ��ġ�� ���� ���� ��ǥ ���
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
        // ��ġ ������ Ÿ�� ����� Ÿ�� ��ġ�� ����
        if (hoveredTile != null && highlightedTiles.Contains(hoveredTile))
        {
            SetAboveTilePosition(currentDeployable, hoveredTile);
        }
        else
        {
            // �ƴ϶�� Ŀ�� ��ġ���� ǥ��
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


    // ��ġ�Ǵ� ��� �����ϴ� �޼���
    private void DeployDeployable(Tile tile)
    {
        DeployableUnitState gameState = unitStates[currentDeployableInfo];
        int cost = gameState.CurrentDeploymentCost;

        // �ڽ�Ʈ ���� ���� & ��ġ ���� ����
        if (StageManager.Instance.TryUseDeploymentCost(cost) && gameState.OnDeploy(currentDeployable))
        {
            if (currentDeployable is Operator op)
            {
                op.Deploy(tile.transform.position);
                op.SetDirection(placementDirection);

                CurrentOperatorDeploymentCount++;
                currentDeployableBox.Deselect();
                currentDeployableBox = null;
            }
            else
            {
                currentDeployable.Deploy(tile.transform.position);
            }
        }

        // ��ġ�� ���� ��Ͽ� �߰�
        deployedItems.Add(currentDeployable);
        UpdateDeployableUI(currentDeployableInfo);
        ResetPlacement();
        StageManager.Instance.UpdateTimeScale();
    }
    
    private void UpdateDeployableUI(DeployableInfo info)
    {
        if (deployableUIBoxes.TryGetValue(info, out DeployableBox box))
        {
            var gameState = unitStates[info];
            box.UpdateDisplay(gameState);

            // �� �̻� ��ġ�� �� ���ٸ� �ڽ� ��Ȱ��ȭ
            if (gameState.RemainingDeployCount <= 0)
            {
                box.gameObject.SetActive(false);
            }
        }
    }


    // ��ġ ���� ������ ���¸� �ǵ���
    private void ResetPlacement()
    {
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
                Destroy(currentDeployable.transform.gameObject);
            }
            currentDeployable = null;
        }
        if (currentDeployableBox != null)
        {
            currentDeployableBox.Deselect();
            currentDeployableBox = null;
        }
        currentDeployablePrefab = null;
        currentDeployableInfo = null;

        UIManager.Instance.HideDeployableInfo();
        StageManager.Instance.UpdateTimeScale(); // �ð� ���󺹱�
        ResetHighlights();

        HideUIs();

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


    // ��ġ�� ��Ұ� ���ŵǾ��� �� ������
    public void OnDeployableRemoved(DeployableUnitEntity deployable)
    {
        deployedItems.Remove(deployable);
        UIManager.Instance.HideDeployableInfo();
        HideUIs();
        ResetHighlights();

        // �ڽ� �����. ���� ���� ������ ����
        if (StageManager.Instance != null && StageManager.Instance.currentState == GameState.Battle)
        {
            DeployableInfo info;
            if (deployable is Operator op)
            {
                CurrentOperatorDeploymentCount--;
                info = GetDeployableInfoByName(op.BaseData.entityName);
            }
            else
            {
                info = GetDeployableInfoByName(deployable.BaseData.entityName);
            }

            if (info != null && unitStates.TryGetValue(info, out var unitState))
            {
                unitState.OnRemoved(); // ���� ������ ���� ������Ʈ
            
                if (deployableUIBoxes.TryGetValue(info, out DeployableBox box))
                {
                    box.UpdateDisplay(unitState);
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
            HideUIs();
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

    public DeployableInfo GetDeployableInfoByName(string entityName)
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
        public GameObject prefab;
        public int maxDeployCount;
        public float redeployTime;

        // ���۷������� �� �Ҵ�
        public Operator? deployedOperator;
        public OwnedOperator? ownedOperator;
        public OperatorData? operatorData;

        // �Ϲ� ��ġ ������ ������ �� �Ҵ�
        public DeployableUnitEntity? deployedDeployable;
        public DeployableUnitData? deployableUnitData;
    }
}