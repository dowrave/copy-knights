using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;

public class OperatorManager : MonoBehaviour
{
    public static OperatorManager Instance { get; private set; }
    // UI ���� ����
    public GameObject bottomPanelOperatorBoxPrefab; // ���� ���۷����� ������, ��ġ �ڽ�Ʈ ���� ���� ���۷����� ������
    public RectTransform bottomPanel;

    // Operator ���� ����
    public List<OperatorData> availableOperators = new List<OperatorData>();
    private List<GameObject> operatorUIBoxes = new List<GameObject>();
    private GameObject currentOperatorPrefab; 

    public Color availableTileColor = Color.green;
    public Color unavailableTileColor = Color.red;
    public Color attackRangeTileColor = new Color(255, 127, 0);
    public GameObject dragIndicatorPrefab;

    private bool isDraggingFromUI = false;
    private bool isPlacingOperator = false;
    private bool isSelectingDirection = false;
    private int operatorIndex = -1; 

    private List<Tile> highlightedTiles = new List<Tile>();
    private Tile currentHoverTile;
    private Vector3 placementDirection = Vector3.left;

    private GameObject previewOperator;
    private GameObject dragIndicator;

    private float minDirectionDistance = 85f; // ���� �׽�Ʈ�ؼ� ���� ��
    private const float INDICATOR_SIZE = 2.5f;

    [SerializeField] private LayerMask tileLayerMask;

    private OperatorData currentOperatorData;

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
        InitializeOperatorUI();
    }

    private void InitializeOperatorUI()
    {
        foreach (OperatorData operatorData in availableOperators)
        {
            GameObject boxObject = Instantiate(bottomPanelOperatorBoxPrefab, bottomPanel);
            BottomPanelOperatorBox box = boxObject.GetComponent<BottomPanelOperatorBox>();
            if (box != null)
            {
                box.Initialize(operatorData);
            }
        }
    }

    private void Update()
    {
        Debug.Log($"{operatorIndex}");
        if (StageManager.Instance.currentState != GameState.Battle) { return; }

        if (isDraggingFromUI)
        {
            HandleDraggingFromUI();
        }
        else if (isPlacingOperator)
        {
            HandleOperatorPlacement();
        }
        else if (isSelectingDirection)
        {
            HandleDirectionSelection();
        }
    }

    // BottomPanelOperatorBox Ŭ�� �� ȣ���
    public void StartOperatorPlacement(OperatorData operatorData)
    {
        operatorIndex = availableOperators.IndexOf(operatorData);
        if (operatorIndex != -1)
        {
            currentOperatorPrefab = operatorData.prefab;
            currentOperatorData = availableOperators[operatorIndex];
            isDraggingFromUI = true;
        }
        else
        {
            Debug.LogError("���۷����� �����Ͱ� availableOperators List�� ����");
        }
    }

    private void HandleDraggingFromUI()
    {

        HighlightAvailableTiles();
        UpdatePreviewOperator();

        Tile hoveredTile = GetHoveredTile();

        if (hoveredTile && highlightedTiles.Contains(hoveredTile) && Input.GetMouseButton(0))
        {
            StartPlacingOperator(hoveredTile);
        }
    }

    private void StartPlacingOperator(Tile tile)
    {
        isDraggingFromUI = false;
        isPlacingOperator = true;
        currentHoverTile = tile;
        CreateDragIndicator(tile);
        HighlightAttackRange(tile, placementDirection);
    }

    private void HandleOperatorPlacement()
    {
        ResetHighlights();
        if (Input.GetMouseButton(0))
        {
            isPlacingOperator = false;
            isSelectingDirection = true;
        }
    }

    private void HandleDirectionSelection()
    {
        ResetHighlights();
        Vector3 dragVector = Input.mousePosition - Camera.main.WorldToScreenPoint(currentHoverTile.transform.position);
        float dragDistance = dragVector.magnitude;
        
        Vector3 newDirection = DetermineDirection(dragVector);

        placementDirection = newDirection;
        HighlightAttackRange(currentHoverTile, placementDirection);
        UpdatePreviewOperatorRotation();

        Debug.Log($"dragDistance : {dragDistance}");

        if (Input.GetMouseButtonUp(0))
        {
            // ��ġ �Ϸ�
            if (dragDistance > minDirectionDistance)
            {
                PlaceOperator(currentHoverTile);
                Destroy(dragIndicator);
                isSelectingDirection = false;
            }
            else
            {
                isPlacingOperator = true;
            }

            
        }
    }

    private void CreateDragIndicator(Tile tile)
    {
        if (dragIndicator != null)
        {
            Destroy(dragIndicator);
        }

        dragIndicator = Instantiate(dragIndicatorPrefab, tile.transform.position + Vector3.up * 0.1f, Quaternion.Euler(90, 0, 0));
        dragIndicator.transform.localScale = Vector3.one * INDICATOR_SIZE;
    }

    // ��ġ ���� �� ���۷���Ŀ �̸� ���� ǥ��
    private void UpdatePreviewOperator()
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

        if (previewOperator == null)
        {
            previewOperator = Instantiate(currentOperatorPrefab, cursorWorldPosition, Quaternion.identity);
            Operator previewOp = previewOperator.GetComponent<Operator>();
            previewOp.IsPreviewMode = true;
            SetPreviewTransparency(0.5f);
        }

        Tile hoveredTile = GetHoveredTile();

        // ��ġ ������ Ÿ�� ����� Ÿ�� ��ġ�� ����
        if (hoveredTile != null && highlightedTiles.Contains(hoveredTile))
        {
            previewOperator.transform.position = hoveredTile.transform.position + Vector3.up * 0.5f;
        }
        else
        {
            // �ƴ϶�� Ŀ�� ��ġ���� ǥ��
            previewOperator.transform.position = cursorWorldPosition;
        }
    }

    private Tile GetHoveredTile()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, tileLayerMask))
        {
            return hit.collider.GetComponentInParent<Tile>();
        }
        return null; 
    }

    private void HighlightAvailableTiles()
    {
        ResetHighlights();
        Operator op = currentOperatorPrefab.GetComponent<Operator>();
        foreach (Tile tile in MapManager.Instance.GetAllTiles())
        {
            if (tile != null && tile.CanPlaceOperator())
            {
                if ((tile.data.terrain == TileData.TerrainType.Ground && op.data.canDeployGround) ||
                    (tile.data.terrain == TileData.TerrainType.Hill && op.data.canDeployHill))
                {
                    tile.Highlight(availableTileColor);
                    highlightedTiles.Add(tile);
                }
            }
        }
    }

    private void HighlightAttackRange(Tile tile, Vector3 direction)
    {
        Operator op = currentOperatorPrefab.GetComponent<Operator>();
        Vector2Int[] attackRange = op.data.attackableTiles;

        foreach (Vector2Int offset in attackRange)
        {
            Vector2Int rotatedOffset = op.RotateOffset(offset, direction);
            Vector2Int targetPos = new Vector2Int(tile.GridPosition.x + rotatedOffset.x,
                                                  tile.GridPosition.y + rotatedOffset.y);
            Tile targetTile = MapManager.Instance.GetTile(targetPos.x, targetPos.y);
            if (targetTile != null)
            {
                targetTile.Highlight(attackRangeTileColor);
                highlightedTiles.Add(targetTile);
            }
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

    private void PlaceOperator(Tile tile)
    {
        GameObject placedOperator = Instantiate(currentOperatorPrefab, tile.transform.position, Quaternion.LookRotation(placementDirection));
        Operator op = placedOperator.GetComponent<Operator>();
        op.Deploy(tile.transform.position + Vector3.up * 0.5f, placementDirection);
        tile.SetOccupied(op);
        currentOperatorPrefab = null;

        Debug.Log($"���۷����� ��ġ �Ϸ� : {tile.GridPosition}");
        ResetPlacement();
    }

    private void ResetPlacement()
    {
        Debug.Log("ResetPlacement �۵�");
        isDraggingFromUI = false;
        isPlacingOperator = false;
        isSelectingDirection = false;
        operatorIndex = -1;

        ResetHighlights();

        if (previewOperator != null)
        {
            Destroy(previewOperator);
        }
        if (dragIndicator != null)
        {
            Destroy(dragIndicator);
        }
    }

    private void CancelPlacement()
    {
        Debug.Log("CancelPlacement �۵�");
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
        Renderer renderer = previewOperator.GetComponentInChildren<Renderer>();
        Material mat = renderer.material;
        Color color = mat.color;
        color.a = alpha;
        mat.color = color;
    }

    private void UpdatePreviewOperatorRotation()
    {
        if (previewOperator != null)
        {
            previewOperator.transform.rotation = Quaternion.LookRotation(placementDirection);
        }
    }
}