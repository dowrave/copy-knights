using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;

public class OperatorManager : MonoBehaviour
{
    public static OperatorManager Instance { get; private set; }
    // UI 관련 변수
    public GameObject bottomPanelOperatorBoxPrefab; // 개별 오퍼레이터 아이콘, 배치 코스트 등을 감쌀 오퍼레이터 프리팹
    public RectTransform bottomPanel;

    // Operator 관련 변수
    public List<OperatorData> availableOperators = new List<OperatorData>();
    private Dictionary<OperatorData, BottomPanelOperatorBox> operatorUIBoxes = new Dictionary<OperatorData, BottomPanelOperatorBox>();
    private GameObject currentOperatorPrefab;
    private OperatorData currentOperatorData;
    private List<OperatorData> deployedOperators = new List<OperatorData>(); // 배치돼서 화면에 표시되지 않을 오퍼레이터
    private OperatorActionUI currentActiveUI;
    public OperatorActionUI CurrentActiveUI { get; private set; }

    // 하이라이트 관련 변수
    public Color availableTileColor = Color.green;
    public Color unavailableTileColor = Color.red;
    public Color attackRangeTileColor = new Color(255, 127, 0);
    public GameObject dragIndicatorPrefab;

    // 배치 과정 중 어떤 상태인지에 대한 변수
    private bool isDraggingFromUI = false;
    private bool isPlacingOperator = false;
    private bool isSelectingDirection = false;
    private int operatorIndex = -1; 
    private Vector3 placementDirection = Vector3.left;

    private List<Tile> highlightedTiles = new List<Tile>();
    private Tile currentHoverTile;
    private GameObject previewOperator;
    private GameObject dragIndicator;

    private float minDirectionDistance = 85f; // 직접 테스트해서 얻은 값
    private const float INDICATOR_SIZE = 2.5f;

    [SerializeField] private LayerMask tileLayerMask;




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
                operatorUIBoxes[operatorData] = box;
            }
        }
    }

    private void Update()
    {
        if (StageManager.Instance.currentState != GameState.Battle) { return; }

        //if (currentActiveUI) { Debug.Log($"currentActiveUI : {currentActiveUI}"); } 

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

    // BottomPanelOperatorBox 클릭 시 호출됨
    public void StartOperatorPlacement(OperatorData operatorData)
    {
        if (StageManager.Instance.CurrentDeploymentCost >= operatorData.deploymentCost)
        {
            operatorIndex = availableOperators.IndexOf(operatorData);
            if (operatorIndex != -1)
            {
                currentOperatorPrefab = operatorData.prefab;
                currentOperatorData = availableOperators[operatorIndex];
                Debug.Log($"오퍼레이터 배치 시작 : {currentOperatorData.operatorName}");
                isDraggingFromUI = true;
            }
            else
            {
                Debug.LogError("오퍼레이터 데이터가 availableOperators List에 없음");
            }

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

        if (Input.GetMouseButtonUp(0))
        {
            // 배치 완료
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

    // 배치 중일 때 오퍼레이터 미리 보기 표현
    private void UpdatePreviewOperator()
    {
        // 항상 커서 위치에 대한 월드 좌표 계산
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

        // 배치 가능한 타일 위라면 타일 위치로 스냅
        if (hoveredTile != null && highlightedTiles.Contains(hoveredTile))
        {
            previewOperator.transform.position = hoveredTile.transform.position + Vector3.up * 0.5f;
        }
        else
        {
            // 아니라면 커서 위치에만 표시
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
        if (StageManager.Instance.TryUseDeploymentCost((int)currentOperatorData.deploymentCost))
        {
            GameObject placedOperator = Instantiate(currentOperatorPrefab, tile.transform.position, Quaternion.LookRotation(placementDirection));
            Operator op = placedOperator.GetComponent<Operator>();
            op.Deploy(tile.transform.position + Vector3.up * 0.5f, placementDirection);
            tile.SetOccupied(op);
            currentOperatorPrefab = null;

            // 배치된 오퍼레이터 리스트에 추가
            deployedOperators.Add(currentOperatorData);

            // UI에서 해당 오퍼레이터 박스 제거
            if (operatorUIBoxes.TryGetValue(currentOperatorData, out BottomPanelOperatorBox box))
            {
                box.gameObject.SetActive(false);
            }

            ResetPlacement();
        }
    }

    private void ResetPlacement()
    {
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

    // 퇴각 / 사망 시 호출
    public void OnOperatorRemoved(OperatorData operatorData)
    {
        deployedOperators.Remove(operatorData);
        if (operatorUIBoxes.TryGetValue(operatorData, out BottomPanelOperatorBox box))
        {
            box.StartCooldown(operatorData.reDeployTime);
        }
    }

    public void SetActiveActionUI(OperatorActionUI ui)
    {
        if (CurrentActiveUI != null && CurrentActiveUI != ui)
        {
            CurrentActiveUI.Hide();
        }
        CurrentActiveUI = ui;
        Debug.LogWarning($"CurrentActiveUI : {CurrentActiveUI}");
    }


    public void HideAllActionUIs()
    {
        if (CurrentActiveUI != null)
        {
            CurrentActiveUI.Hide();
            CurrentActiveUI = null;
        }
    }
}