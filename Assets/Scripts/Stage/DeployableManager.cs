using UnityEngine;
using System.Collections.Generic;

public class DeployableManager : MonoBehaviour
{
    private enum UIState
    {
        None,
        OperatorAction,
        OperatorDeploying
    }

    private UIState currentUIState = UIState.None;

    public static DeployableManager Instance { get; private set; }
    // UI ���� ����
    public GameObject bottomPanelDeployableBoxPrefab;
    public RectTransform bottomPanel;

    // Deployable ���� ����
    public List<GameObject> availableDeployables = new List<GameObject>();
    private Dictionary<GameObject, BottomPanelDeployableBox> deployableUIBoxes = new Dictionary<GameObject, BottomPanelDeployableBox>();
    private GameObject currentDeployablePrefab;
    private DeployableUnitEntity currentDeployable;

    // ���̶���Ʈ ���� ����
    public Color availableTileColor = new Color(0, 1, 0, 0.5f); 
    public Color attackRangeTileColor = new Color(255, 127, 0);

    // ��ġ ���� �� � ���������� ���� ����
    private bool isDeployableSelecting = false; // �ϴ� UI���� ���۷����͸� Ŭ���� ����
    private bool isDraggingDeployable = false; // Ÿ�� ���� ���� : �ϴ� UI���� ���۷����͸� MouseButtonDown�� ���·� �巡���ϰ� �ִ� ����. 
    private bool isSelectingDirection = false; // ���� ���� ���� : Ÿ���� �������� ���۷������� ������ ������
    private bool isMousePressed = false; // HandleDirectionSelection������ ���. ���콺�� Ŭ�� �������� �����Ѵ�. 
    private int DeployableIndex = -1; 
    private Vector3 placementDirection = Vector3.left;

    public bool IsMousePressed
    {
        get { return isMousePressed; }
        set { isMousePressed = value; }
    }

    private List<Tile> highlightedTiles = new List<Tile>();
    private Tile currentHoverTile;

    private float minDirectionDistance;
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
        InitializeDeployableUI();
    }

    private void InitializeDeployableUI()
    {
        foreach (GameObject deployablePrefab in availableDeployables)
        {
            GameObject boxObject = Instantiate(bottomPanelDeployableBoxPrefab, bottomPanel);
            BottomPanelDeployableBox box = boxObject.GetComponent<BottomPanelDeployableBox>();

            if (box != null)
            {
                box.Initialize(deployablePrefab);
                deployableUIBoxes[deployablePrefab] = box;
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
                if ((tile.data.terrain == TileData.TerrainType.Ground && currentDeployable.Data.canDeployOnGround) ||
                    (tile.data.terrain == TileData.TerrainType.Hill && currentDeployable.Data.canDeployOnHill))
                {
                    tile.Highlight(availableTileColor);
                    highlightedTiles.Add(tile);
                }
            }
        }
    }


    // BottomPanelOperatorBox Ŭ�� �� �۵�, ��ġ�Ϸ��� ���۷������� ������ ������ �ִ´�.
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

    public void HandleDragging(GameObject deployablePrefab)
    {
        if (isDraggingDeployable && currentDeployablePrefab == deployablePrefab)
        {
            UpdatePreviewDeployable();
        }
    }

    public void EndDragging(GameObject deployablePrefab)
    {
        if (isDraggingDeployable && currentDeployablePrefab == deployablePrefab)
        {
            isDraggingDeployable = false;
            Tile hoveredTile = GetHoveredTile();

            if (hoveredTile && CanPlaceOnTile(hoveredTile))
            {
                StartDirectionSelection(hoveredTile);
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
            if (currentDeployable != null)
            {
                currentDeployable.IsPreviewMode = true;
                currentDeployable.SetPreviewTransparency(0.5f);
                (currentDeployable as MonoBehaviour).gameObject.SetActive(true);
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

    // ���� ����
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

    private void EndDirectionSelection()
    {


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

        (currentDeployable as MonoBehaviour).gameObject.SetActive(true);

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
    /// ������� ������ ���� ���� ��ġ ����
    /// </summary>
    private void DeployDeployable(Tile tile)
    {
        if (StageManager.Instance.TryUseDeploymentCost(currentDeployable.DeploymentCost))
        {
            currentDeployable.Initialize(currentDeployable.Data); // ��ġ �ÿ� ������ ���� ����
            currentDeployable.Deploy(tile.transform.position);

            if (currentDeployable is Operator op)
            {
                op.SetDirection(placementDirection);
            }

            tile.SetOccupied(currentDeployable);

            deployedItems.Add(currentDeployable);

            if (deployableUIBoxes.TryGetValue(currentDeployablePrefab, out BottomPanelDeployableBox box))
            {
                box.gameObject.SetActive(false);
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

    private void SetPreviewTransparency(float alpha)
    {
        if (currentDeployable != null)
        {
            currentDeployable.SetPreviewTransparency(alpha);
        }
    }

    private void UpdatePreviewRotation()
    {
        if (currentDeployable != null)
        {
            currentDeployable.transform.rotation = Quaternion.LookRotation(placementDirection);
        }
    }

    public void OnDeployableRemoved(DeployableUnitEntity deployable)
    {
        deployedItems.Remove(deployable);
        UIManager.Instance.HideDeployableInfo();

        HideAllUIs();
        ResetHighlights();

        GameObject prefab = deployable.Prefab;
        if (prefab != null && deployableUIBoxes.TryGetValue(prefab, out BottomPanelDeployableBox box))
        {
            box.gameObject.SetActive(true);
            box.StartCooldown(70f); // Assuming a fixed cooldown time
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
            Debug.Log("CancelCurrentAction ����");
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
        // Ÿ���� ��ġ �����Ѱ�? + Ÿ���� ���̶���Ʈ�Ǿ��°�?
        return tile.CanPlaceDeployable() && highlightedTiles.Contains(tile);
    }

    // Ÿ�� ���� ��ġ�Ǵ� ��ġ ������ ����� ��ġ ����
    // �ٸ����̵尡 �� �߱淡 �������
    private void SetAboveTilePosition(DeployableUnitEntity deployable, Tile tile)
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
}