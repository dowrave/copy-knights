using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ��ġ ������ ��ҵ��� ��ġ ������ �����
/// </summary>
public class DeployableManager : MonoBehaviour
{
    public static DeployableManager Instance { get; private set; }

    [System.Serializable]
    public class DeployableInfo
    {
        public GameObject prefab;
        public int maxDeployCount;
        public int remainingDeployCount;
        public float redeployTime;

        // ���۷������� �� �Ҵ�
        public OwnedOperator? ownedOperator;
        public OperatorData? operatorData;

        // �Ϲ� ��ġ ������ ������ �� �Ҵ�
        public DeployableUnitData? deployableUnitData;
    }

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
    private List<DeployableInfo> allDeployables = new List<DeployableInfo>(); // ��ģ ��
    private Dictionary<GameObject, DeployableBox> deployableUIBoxes = new Dictionary<GameObject, DeployableBox>();
    private DeployableInfo currentDeployableInfo;
    private GameObject currentDeployablePrefab;
    private DeployableUnitEntity currentDeployable;

    [Header("Highlight Color")]
    // ���̶���Ʈ ���� ���� - �ν����Ϳ��� ����
    public Color availableTileColor;
    public Color attackRangeTileColor;

    // ��ġ ���� ���� ����
    public bool IsDeployableSelecting { get; private set; } = false; // �ϴ� UI���� ���۷����͸� Ŭ���� ����
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

    // ��ƼƼ �̸��� deployableInfo�� �����ϴ� ��ųʸ�
    private static Dictionary<string, DeployableInfo> deployableInfoMap = new Dictionary<string, DeployableInfo>();

    // �ӽ� Ŭ�� ���� �ð�
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

        // OwnedOperator -> DeployableInfo�� ��ȯ�ؼ� �߰�
        foreach (OwnedOperator op in squadData.Where(op => op != null))
        {
            var info = new DeployableInfo
            {
                prefab = op.BaseData.prefab,
                maxDeployCount = 1,
                remainingDeployCount = 1,
                redeployTime = op.BaseData.stats.RedeployTime,
                ownedOperator = op,
                operatorData = op.BaseData
            };

            allDeployables.Add(info);
            deployableInfoMap[op.BaseData.entityName] = info; // (���۷����� ��ƼƼ �̸� - ��ġ ����) ����
        }

        // �������� ���� ��� -> DeployableInfo�� ��ȯ
        foreach (var deployable in stageDeployables)
        {
            var info = deployable.ToDeployableInfo();
            allDeployables.Add(info);
            deployableInfoMap[deployable.deployableData.entityName] = info; // (��ġ ��� �̸� - ��ġ ����) ����
        }

        InitializeDeployableUI();
    }

    /// <summary>
    /// DeployableBox�� Deployable ��� �����յ��� �Ҵ��ϴ� ����
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

        // ���⿡�� �ش� ������ �� ��� �۵��ϰ� �־�� �ϴ� �Լ��� ��
        // !! ���¸� ������ ������ �۵��Ǿ�� �ϴ� �Լ��� ���⿡ ���� �ȵ�! !! 

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

    /// <summary>
    /// Ÿ�� ���� üũ
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


    // BottomPanelOperatorBox ���콺��ư �ٿ� �� �۵�, ��ġ�Ϸ��� ���۷������� ������ ������ �ִ´�.
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
    /// BottomPanelDeployableBox ���콺��ư �ٿ� �� ����
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
    /// BottomPanelDeployableBox ���콺��ư �ٿ� �� �巡�� �� ����
    /// </summary>
    public void HandleDragging(DeployableInfo deployableInfo)
    {
        if (IsDraggingDeployable && currentDeployableInfo == deployableInfo)
        {
            UpdatePreviewDeployable();
        }
    }

    /// <summary>
    /// �巡�� �� Ŀ���� ���� ���� ����
    /// </summary>
    public void EndDragging(GameObject deployablePrefab)
    {
        if (IsDraggingDeployable && currentDeployablePrefab == deployablePrefab)
        {
            Debug.Log("�巡�� ����");
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
                    currentDeployable.Initialize(currentDeployable.BaseData);
                    DeployDeployable(hoveredTile);
                }
            }
            else
            {
                Debug.Log("��ġ ���� ���");
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
        // �ϰ��� ��ġ �����ϱ�
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
    /// ���۷����� ������ ��Ÿ�� ActionUI, DeployingUI ����
    /// OperatorInfoPanel�� ����� �� ������ �޼���
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
    /// ���� ���� ���� ����
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
        //}
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

    /// <summary>
    /// Deploy�� �� ��� �װ� �̿��� ó���� �Բ� ���ϴ� �� �޼��带 ����Ѵ�
    /// </summary>
    private void DeployDeployable(Tile tile)
    {
        DeployableInfo deployableInfo = allDeployables.Find(d => d.prefab == currentDeployablePrefab);
        if (deployableInfo == null)
        {
            Debug.LogError($"{deployableInfo}�� ã�� �� ����");
            return;
        }

        // DeployableBox���� ���� ����Ǿ�� �� �ڽ�Ʈ�� ������
        DeployableBox box_ = deployableUIBoxes[currentDeployablePrefab];
        int deploymentCost = box_.GetCurrentDeploymentCost();

        if (currentDeployable is Operator op)
        {
            if (StageManager.Instance.TryUseDeploymentCost(deploymentCost))
            {
                op.Deploy(tile.transform.position);
                op.SetDirection(placementDirection);
            }
            else
            {
                Debug.LogError($"��ġ ����: �ʿ� �ڽ�Ʈ({deploymentCost}) > ���� �ڽ�Ʈ({StageManager.Instance.CurrentDeploymentCost})");
                return;
            }
        }
        else
        {
            if (StageManager.Instance.TryUseDeploymentCost(deploymentCost))
            {
                currentDeployable.Deploy(tile.transform.position);
            }
            else
            {
                Debug.LogError($"��ġ ����: �ʿ� �ڽ�Ʈ({deploymentCost}) > ���� �ڽ�Ʈ({StageManager.Instance.CurrentDeploymentCost})");
                return;
            }
        }

        // ��ġ ����Ʈ�� ������ �߰�
        deployedItems.Add(currentDeployable);

        // ��ġ �� �ڽ��� ó��
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

    /// <summary>
    /// ��ġ ���� ���� ������ ���¸� �ǵ���
    /// </summary>
    private void ResetPlacement()
    {
        Debug.Log("ResetPlacement ����");
        IsDeployableSelecting = false;
        IsDraggingDeployable = false;
        IsSelectingDirection = false;
        IsMousePressed = false;

        if (currentDeployable != null)
        {
            Debug.Log($"currentDeployable.IsPreviewMode : {currentDeployable.IsPreviewMode}");

            if (currentDeployable.IsPreviewMode)
            {
                Destroy(currentDeployable.transform.gameObject);
            }
            currentDeployable = null;
        }
        currentDeployablePrefab = null;
        currentDeployableInfo = null;

        UIManager.Instance.HideDeployableInfo();
        StageManager.Instance.UpdateTimeScale(); // �ð� ���󺹱�
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
    /// ��ġ�� ��Ұ� ���ŵǾ��� �� ������
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

            // �ϴ� Operator�� ���ŵ��� �� ���ġ ��Ÿ���� �����ؾ� ��
            if (deployable is Operator op)
            {
                box.OnOperatorReturn(); // �ڽ�Ʈ ����
                box.StartCooldown(op.currentStats.RedeployTime);
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

    public void ShowDeployableInfoPanel(DeployableInfo deployableInfo)
    {
        UIManager.Instance.ShowUndeployedInfo(deployableInfo);
    }

    public void HideDeployableInfoPanel()
    {
        UIManager.Instance.HideDeployableInfo();
    }

    /// <summary>
    /// ��ġ ���̰ų�, ��ġ�� ���۷����͸� Ŭ���� ���¸� ����ϴ� ����
    /// </summary>
    public void CancelCurrentAction()
    {
        if (currentUIState != UIState.None) // Action�̰ų� Deploying�� ��
        {
            HideAllUIs();
            ResetPlacement();
            ResetHighlights();
        }
    }

    /// <summary>
    /// ��ġ �巡�� �ּ� ����
    /// </summary>
    public void SetMinDirectionDistance(float screenDiamondRadius)
    {
        minDirectionDistance = screenDiamondRadius / 2;
    }

    private bool CanPlaceOnTile(Tile tile)
    {
        // Ÿ���� ��ġ �����Ѱ�? + Ÿ���� ���̶���Ʈ�Ǿ��°�? + ���� ������ ��ü�� �� Ÿ�Ͽ� ��ġ�� �� �մ°�?
        return tile.CanPlaceDeployable() &&
            highlightedTiles.Contains(tile);
    }

    // Ÿ�� ���� ��ġ�Ǵ� ��ġ ������ ����� ��ġ ����
    // �ٸ����̵尡 �� �߱淡 �������
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

    // deployableInfo ���� �޼���
    public DeployableInfo GetDeployableInfo(GameObject prefab)
    {
        return allDeployables.Find(d => d.prefab == prefab);
    }

    public DeployableInfo GetDeployableInfoByName(string entityName)
    {
        return deployableInfoMap.TryGetValue(entityName, out var info) ? info : null;
    }
}