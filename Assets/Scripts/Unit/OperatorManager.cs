using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;

public class OperatorManager : MonoBehaviour
{
    public GameObject operatorPrefab;
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


    private void Update()
    {
        if (StageManager.Instance.currentState != GameState.Battle) { return; }

        // Battle ���� ������ �����Ѵ�.
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

    // UI���� ���۷����͸� Ŭ������ �� �۵���
    public void StartDraggingFromUI()
    {
        isDraggingFromUI = true;
        //HighlightAvailableTiles();
    }

    private void HandleDraggingFromUI()
    {
        HighlightAvailableTiles();

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Tile hoveredTile = null;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, tileLayerMask))
        {
            hoveredTile = hit.collider.GetComponentInParent<Tile>();
        }

        UpdatePreviewOperator(hoveredTile);

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

        if (Input.GetMouseButtonUp(0))
        {
            // ��ġ �Ϸ�
            if (dragDistance > minDirectionDistance)
            {
                PlaceOperator(currentHoverTile);
                Destroy(dragIndicator);
            }
            else
            {
                isPlacingOperator = true;
            }

            isSelectingDirection = false;
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
    private void UpdatePreviewOperator(Tile hoveredTile)
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
            previewOperator = Instantiate(operatorPrefab, cursorWorldPosition, Quaternion.identity);
            Operator previewOperatorScript = previewOperator.GetComponent<Operator>();
            previewOperatorScript.IsPreviewMode = true;
            SetPreviewTransparency(0.5f);
        }

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

    private void HighlightAvailableTiles()
    {
        ResetHighlights();
        Operator operatorScript = operatorPrefab.GetComponent<Operator>();

        foreach (Tile tile in MapManager.Instance.GetAllTiles())
        {
            if (tile != null && tile.CanPlaceOperator())
            {
                if ((tile.data.terrain == TileData.TerrainType.Ground && operatorScript.data.canDeployGround) ||
                    (tile.data.terrain == TileData.TerrainType.Hill && operatorScript.data.canDeployHill))
                {
                    tile.Highlight(availableTileColor);
                    highlightedTiles.Add(tile);
                }
            }
        }
    }

    private void HighlightAttackRange(Tile tile, Vector3 direction)
    {
        Operator operatorScript = operatorPrefab.GetComponent<Operator>();
        Vector2Int[] attackRange = operatorScript.data.attackableTiles;

        foreach (Vector2Int offset in attackRange)
        {
            Vector2Int rotatedOffset = operatorScript.RotateOffset(offset, direction);
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
        GameObject placedOperator = Instantiate(operatorPrefab, tile.transform.position, Quaternion.LookRotation(placementDirection));
        Operator operatorScript = placedOperator.GetComponent<Operator>();
        operatorScript.Deploy(tile.transform.position + Vector3.up * 0.5f, placementDirection);
        tile.SetOccupied(true);
        operatorPrefab = null;

        Debug.Log($"���۷����� ��ġ �Ϸ� : {tile.GridPosition}");
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