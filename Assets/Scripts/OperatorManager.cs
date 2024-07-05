using UnityEngine;
using System.Collections.Generic;

public class OperatorManager : MonoBehaviour
{
    public GameObject operatorPrefab;
    public MapManager mapManager;
    public Color availableTileColor = Color.green;
    public Color unavailableTileColor = Color.red;
    public Color attackRangeTileColor = new Color(255, 127, 0);
    public GameObject dragIndicatorPrefab;

    private bool isDraggingFromUI = true;
    private bool isPlacingOperator = false;
    private bool isSelectingDirection = false;

    private List<Tile> highlightedTiles = new List<Tile>();
    private Tile currentHoverTile;
    private Vector3 placementDirection = Vector3.left;

    private GameObject previewOperator;
    private GameObject dragIndicator;

    private float minDirectionDistance = 1f;
    private const float INDICATOR_SIZE = 2.5f;

    [SerializeField] private LayerMask tileLayerMask; 

    public void Start()
    {
        StartDraggingFromUI();
    }
    private void Update()
    {
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

    // UI에서 오퍼레이터를 클릭했을 때 작동됨
    public void StartDraggingFromUI()
    {
        isDraggingFromUI = true;
        HighlightAvailableTiles();
    }

    private void HandleDraggingFromUI()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, tileLayerMask))
        {
            Tile hoveredTile = hit.collider.GetComponentInParent<Tile>();

            if (hoveredTile != null)
            {
                UpdatePreviewOperator(hoveredTile);
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (hoveredTile != null && highlightedTiles.Contains(hoveredTile))
                {
                    StartPlacingOperator(hoveredTile);
                }
                else
                {
                    CancelPlacement();
                }
            }
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

        //if (dragDistance > minDirectionDistance * Screen.height)
        {
            Vector3 newDirection = DetermineDirection(dragVector);
            //if (newDirection != placementDirection)

            placementDirection = newDirection;
            HighlightAttackRange(currentHoverTile, placementDirection);
            UpdatePreviewOperatorRotation();

        }

        if (Input.GetMouseButtonUp(0))
        {
            // 배치 완료
            if (dragDistance > minDirectionDistance)
            {
                PlaceOperator(currentHoverTile);
                Destroy(dragIndicator);
            }
            else
            {
                isSelectingDirection = false;
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

    private void UpdatePreviewOperator(Tile hoveredTile)
    {
        if (previewOperator == null)
        {
            previewOperator = Instantiate(operatorPrefab, hoveredTile.transform.position, Quaternion.identity);
            SetPreviewTransparency(0.5f);
        }

        if (highlightedTiles.Contains(hoveredTile))
        {
            previewOperator.transform.position = hoveredTile.transform.position + Vector3.up * 0.5f;
        }
        else
        {
            previewOperator.transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward * 10);
        }
    }

    private void HighlightAvailableTiles()
    {
        ResetHighlights();
        Operator operatorScript = operatorPrefab.GetComponent<Operator>();

        foreach (Tile tile in mapManager.GetAllTiles())
        {
            if (tile != null && tile.CanPlaceOperator())
            {
                if ((tile.Type == TileType.Ground && operatorScript.data.canDeployGround) ||
                    (tile.Type == TileType.Hill && operatorScript.data.canDeployHill))
                {
                    tile.Highlight(availableTileColor);
                    highlightedTiles.Add(tile);
                }
            }
        }
    }

    private void HighlightAttackRange(Tile tile, Vector3 direction)
    {
        Debug.Log("공격범위 표시 로직 작동");

        Operator operatorScript = operatorPrefab.GetComponent<Operator>();
        operatorScript.SetFacingDirection(direction);
        Vector2Int[] attackRange = operatorScript.data.attackableTiles;

        foreach (Vector2Int offset in attackRange)
        {
            Vector2Int rotatedOffset = RotateOffset(offset, direction);
            Vector2Int targetPos = new Vector2Int(tile.GridPosition.x + rotatedOffset.x,
                                                  tile.GridPosition.y + rotatedOffset.y);
            Tile targetTile = mapManager.GetTile(targetPos.x, targetPos.y);
            if (targetTile != null)
            {
                targetTile.Highlight(attackRangeTileColor);
                highlightedTiles.Add(targetTile);
            }
        }
    }

    private Vector2Int RotateOffset(Vector2Int offset, Vector3 direction)
    {
        if (direction == Vector3.left) return offset;
        if (direction == Vector3.right) return new Vector2Int(-offset.x, -offset.y);
        if (direction == Vector3.forward) return new Vector2Int(offset.y, -offset.x);
        if (direction == Vector3.back) return new Vector2Int(-offset.y, offset.x);
        return offset;
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
        GameObject placedOperator = Instantiate(operatorPrefab, tile.transform.position + Vector3.up * 0.5f, Quaternion.LookRotation(placementDirection));
        Operator operatorScript = placedOperator.GetComponent<Operator>();
        operatorScript.SetFacingDirection(placementDirection);

        tile.SetOccupied(true);
        Debug.Log("오퍼레이터 배치 완료");
        ResetPlacement();
    }

    private void ResetPlacement()
    {
        isDraggingFromUI = false;
        isPlacingOperator = false;
        isSelectingDirection = false;
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