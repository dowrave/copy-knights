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

    // �ʱ�ȭ ���� ������
    private UIState currentUIState = UIState.None;

    // UI ���� ����
    public GameObject DeployableBoxPrefab;
    public RectTransform bottomPanel;

    // Deployable ���� ����
    private List<DeployableInfo> allDeployables = new List<DeployableInfo>(); // ��ģ ��
    private Dictionary<GameObject, DeployableBox> deployableUIBoxes = new Dictionary<GameObject, DeployableBox>();
    private GameObject currentDeployablePrefab;
    private DeployableUnitEntity currentDeployable;

    // ���̶���Ʈ ���� ���� - �ν����Ϳ��� ����
    public Color availableTileColor;
    public Color attackRangeTileColor;

    // ��ġ ���� �� � ���������� ���� ����
    private bool isDeployableSelecting = false; // �ϴ� UI���� ���۷����͸� Ŭ���� ����
    private bool isDraggingDeployable = false; // Ÿ�� ���� ���� : �ϴ� UI���� ���۷����͸� MouseButtonDown�� ���·� �巡���ϰ� �ִ� ����. 
    public bool IsDraggingDeployable => isDraggingDeployable;
    private bool isSelectingDirection = false; // ���� ���� ���� : Ÿ���� �������� ���۷������� ������ ������
    public bool IsSelectingDirection => isSelectingDirection;

    private bool isMousePressed = false; // HandleDirectionSelection������ ���. ���콺�� Ŭ�� �������� �����Ѵ�. 
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
            Debug.LogWarning($"��ȿ���� ���� deployable Prefab : {prefab.name}");
        }
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
        if (isDeployableSelecting)
        {
            HighlightAvailableTiles();
        }
        // 2. ���۷����͸� �巡�� ���� �� (Ÿ�� ���� ����)
        else if (isDraggingDeployable)
        {
            UpdatePreviewDeployable();
        }
        // 3. ���۷������� ������ ���� �� (���� ���� ����)
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
    /// Ÿ�� ���� üũ
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


    // BottomPanelOperatorBox ���콺��ư �ٿ� �� �۵�, ��ġ�Ϸ��� ���۷������� ������ ������ �ִ´�.
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
    /// BottomPanelDeployableBox ���콺��ư �ٿ� �� ����
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
    /// BottomPanelDeployableBox ���콺��ư �ٿ� �� �巡�� �� ����
    /// </summary>
    public void HandleDragging(GameObject deployablePrefab)
    {
        if (isDraggingDeployable && currentDeployablePrefab == deployablePrefab)
        {
            UpdatePreviewDeployable();
        }
    }

    /// <summary>
    /// ��ġ�Ǵ� Ÿ���� �������� �� ����
    /// </summary>
    public void EndDragging(GameObject deployablePrefab)
    {

        if (isDraggingDeployable && currentDeployablePrefab == deployablePrefab)
        {
            isDraggingDeployable = false;
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
                    isSelectingDirection = false;
                    IsMousePressed = false;
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
    }

    /// <summary>
    /// ��ġ ���� ���� ������ ���¸� �ǵ���
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

    public void ShowDeployableInfoPanel(DeployableUnitEntity deployable)
    {
        UIManager.Instance.ShowDeployableInfo(deployable);
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
    /// ���۷����� ��ġ ���� ��, 
    /// ���콺 ��ư�ٿ��� ���� ��ġ�� ���� �ּ� �巡�� ����(���콺 ��ư���� ���� �� ��ġ�Ǳ� ���� �ּ� �Ÿ�) ����
    /// minDirectionDistance ���� "��ũ�� ��"������ ���̰� �ȴ�.
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
}