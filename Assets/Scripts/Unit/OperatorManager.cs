using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;
using System.Runtime.CompilerServices;

public class OperatorManager : MonoBehaviour
{
    public static OperatorManager Instance { get; private set; }
    // UI ���� ����
    public GameObject bottomPanelOperatorBoxPrefab; // ���� ���۷����� ������, ��ġ �ڽ�Ʈ ���� ���� ���۷����� ������
    public RectTransform bottomPanel;

    // Operator ���� ����
    public List<OperatorData> availableOperators = new List<OperatorData>();
    private Dictionary<OperatorData, BottomPanelOperatorBox> operatorUIBoxes = new Dictionary<OperatorData, BottomPanelOperatorBox>();
    private GameObject currentOperatorPrefab; // ���� ��ġ ���� ���۷����� ������
    private OperatorData currentOperatorData; // ���� ��ġ ���� ���۷����� ����
    private List<OperatorData> deployedOperators = new List<OperatorData>(); // ��ġ�ż� ȭ�鿡 ǥ�õ��� ���� ���۷�����
    private OperatorActionUI currentActiveActionUI;
    public OperatorActionUI CurrentActiveActionUI { get; private set; }

    // ���̶���Ʈ ���� ����
    public Color availableTileColor = Color.green;
    public Color unavailableTileColor = Color.red;
    public Color attackRangeTileColor = new Color(255, 127, 0);

    // ��ġ ���� �� � ���������� ���� ����
    private bool isOperatorSelecting = false; // �ϴ� UI���� ���۷����͸� Ŭ���� ����
    private bool isDraggingOperator = false; // Ÿ�� ���� ���� : �ϴ� UI���� ���۷����͸� MouseButtonDown�� ���·� �巡���ϰ� �ִ� ����. 
    private bool isSelectingDirection = false; // ���� ���� ���� : Ÿ���� �������� ���۷������� ������ ������
    private bool isMousePressed = false; // HandleDirectionSelection������ ���. ���콺�� Ŭ�� �������� �����Ѵ�. 
    private int operatorIndex = -1; 
    private Vector3 placementDirection = Vector3.left;

    private List<Tile> highlightedTiles = new List<Tile>();
    private Tile currentHoverTile;
    private GameObject previewOperator;

    private float minDirectionDistance = 85f; // ���� �׽�Ʈ�ؼ� ���� ��
    private const float INDICATOR_SIZE = 2.5f;

    [SerializeField] private LayerMask tileLayerMask;
    [SerializeField] private OperatorDeployingUI deployingUIPrefab;
    private OperatorDeployingUI deployingUI;


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

        // ���⿡�� �ش� ������ �� ��� �۵��ϰ� �־�� �ϴ� �Լ��� ��
        // !! ���¸� ������ ������ �۵��Ǿ�� �ϴ� �Լ��� ���⿡ ���� �ȵ�! !! 

        // 1. �ϴ� UI�� ���۷����� Ŭ�� �� ��ġ ������ Ÿ�ϵ� ���̶���Ʈ
        if (isOperatorSelecting)
        {
            HighlightAvailableTiles();
        }
        // 2. ���۷����͸� �巡�� ���� �� (Ÿ�� ���� ����)
        else if (isDraggingOperator)
        {
            UpdatePreviewOperator();
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


    // BottomPanelOperatorBox Ŭ�� �� �۵�, ��ġ�Ϸ��� ���۷������� ������ ������ �ִ´�.
    public void StartOperatorSelection(OperatorData operatorData)
    {
        // ���� ���õ� ���۷����Ͱ� ���ų�, ���� ���õ� ���۷����Ϳ� �ٸ� ���۷����Ͱ� ���õ��� ���� ����
        if (currentOperatorData != operatorData)
        {
            HideAllUnitUIs();

            ResetPlacement();
            currentOperatorData = operatorData;
            currentOperatorPrefab = operatorData.prefab;
            isOperatorSelecting = true;
            HighlightAvailableTiles();
        }
    }

    
    public void StartDragging(OperatorData operatorData)
    {
        if (currentOperatorData == operatorData)
        {
            isOperatorSelecting = false; // �巡�׷� ���� ����
            isDraggingOperator = true;
            CreatePreviewOperator();
        }
    }

    public void HandleDragging(OperatorData operatorData)
    {
        if (isDraggingOperator && currentOperatorData == operatorData)
        {
            //UpdatePreviewOperator(); // �갡 �۵� ���ص� OperatorManager.Update()���� �̹� �����ϰ� ����
        }
    }

    public void EndDragging(OperatorData operatorData)
    {
        if (isDraggingOperator && currentOperatorData == operatorData)
        {
            isDraggingOperator = false;
            Tile hoveredTile = GetHoveredTile();
            if (hoveredTile && highlightedTiles.Contains(hoveredTile))
            {
                StartDirectionSelection(hoveredTile);
            }
            else
            {
                CancelOperatorSelection();
            }
        }
    }

    private void CreatePreviewOperator()
    {
        if (previewOperator == null)
        {
            previewOperator = Instantiate(currentOperatorPrefab, Vector3.zero, Quaternion.identity);
            Operator previewOp = previewOperator.GetComponent<Operator>();
            previewOp.IsPreviewMode = true;
            SetPreviewTransparency(0.5f);
        }
    }

    private void StartDirectionSelection(Tile tile)
    {
        isSelectingDirection = true;

        ResetHighlights();
        currentHoverTile = tile;

        ShowDeployingUI(tile.transform.position + Vector3.up * 0.5f);
        UpdatePreviewOperatorRotation();
    }

    private void ShowDeployingUI(Vector3 position)
    {
        // � UI�� ������ ��� ���� Ȱ��ȭ�� UI�� ��� ��Ȱ��ȭ�ؾ� ��
        if (currentActiveActionUI)
        {
            currentActiveActionUI.Hide(); // �̰� �ڽ� ������Ʈ�� ���� ���̶� ����ٰ� ��Ÿ���Բ� ��
        }

        if (deployingUI == null)
        {
            deployingUI = Instantiate(deployingUIPrefab);
        }

        deployingUI.Show(position);
    }

    // ���� ����
    public void HandleDirectionSelection()
    { 
        //if (!Input.GetMouseButton(0)) return;
        if (Input.GetMouseButtonDown(0))
        {
            isMousePressed = true;
        }

        if (isMousePressed)
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
                // ���� �Ÿ� �̻� Ŀ�� �̵� �� ��ġ
                if (dragDistance > minDirectionDistance)
                {
                    PlaceOperator(currentHoverTile);
                    EndDirectionSelection();
                    isMousePressed = false;
                }
                // �ٿ���� �̳���� �ٽ� ���� ����(Ŭ�� X) ����
                else
                {
                    isMousePressed = false;
                    ResetHighlights();
                }
            }
        }
    }

    private void EndDirectionSelection()
    {
        isSelectingDirection = false;
        //Destroy(dragIndicator);
        ResetPlacement();
    }

    // ��ü ��ġ ������ ���� : BottomPanelOperatorBox Ŭ�� �� ȣ��� 
    public void StartOperatorPlacement(OperatorData operatorData)
    {
        // ���� ��ġ �ڽ�Ʈ�� ���۷������� ��ġ �ڽ�Ʈ���� ���� ������ ��ġ ����
        if (StageManager.Instance.CurrentDeploymentCost >= operatorData.deploymentCost)
        {
            operatorIndex = availableOperators.IndexOf(operatorData);
            if (operatorIndex != -1)
            {
                currentOperatorPrefab = operatorData.prefab;
                currentOperatorData = availableOperators[operatorIndex];
                isOperatorSelecting = true;
            }
            else
            {
                Debug.LogError("���۷����� �����Ͱ� availableOperators List�� ����");
            }

        }
    }


    // ��ġ ���� �� ���۷����� �̸� ���� ǥ��
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


    /// <summary>
    /// ������� ������ ���� ���� ��ġ ����
    /// </summary>
    private void PlaceOperator(Tile tile)
    {
        if (StageManager.Instance.TryUseDeploymentCost((int)currentOperatorData.deploymentCost))
        {
            GameObject placedOperator = Instantiate(currentOperatorPrefab, tile.transform.position, Quaternion.LookRotation(placementDirection));
            Operator op = placedOperator.GetComponent<Operator>();
            op.Deploy(tile.transform.position + Vector3.up * 0.5f, placementDirection);
            tile.SetOccupied(op);
            currentOperatorPrefab = null;

            // ��ġ�� ���۷����� ����Ʈ�� �߰�
            deployedOperators.Add(currentOperatorData);

            // �ϴ� �г� UI���� �ش� ���۷����� �ڽ� ����
            if (operatorUIBoxes.TryGetValue(currentOperatorData, out BottomPanelOperatorBox box))
            {
                box.gameObject.SetActive(false);
            }

            ResetPlacement();
        }
    }

    /// <summary>
    /// ��ġ ���� ���� ������ ���¸� �ǵ���
    /// </summary>
    private void ResetPlacement()
    {
        isOperatorSelecting = false;
        isDraggingOperator = false;
        isSelectingDirection = false;
        isMousePressed = false;

        currentOperatorData = null;
        currentOperatorPrefab = null;
        operatorIndex = -1;

        ResetHighlights();

        if (previewOperator != null)
        {
            Destroy(previewOperator);
            previewOperator = null;
        }

        if (deployingUI != null)
        {
            Destroy(deployingUI.gameObject);
            deployingUI = null;
        }
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

    // �� / ��� �� ȣ��
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
        // ��ġ UI�� ���ִٸ� ����, ��ġ �ʱ�ȭ
        if (deployingUI)
        {
            Destroy(deployingUI);
            ResetPlacement();
        }

        // ���� ���õ� ui�� ���� ���õ� actionUI�� �ٸ� ����� ����(�ڽ� ������Ʈ�� ����)
        if (CurrentActiveActionUI != null && CurrentActiveActionUI != ui)
        {
            CurrentActiveActionUI.Hide();
        }

        CurrentActiveActionUI = ui;
    }

    public void CancelOperatorSelection()
    {
        ResetPlacement();
    }

    private void HideDeployingUI()
    {
        if (deployingUI != null)
        {
            deployingUI.Hide();
        }
    }

    private void HideAllUnitUIs()
    {
        if (CurrentActiveActionUI != null)
        {
            CurrentActiveActionUI.Hide();
            CurrentActiveActionUI = null;
        }

        if (deployingUI != null)
        {
            Destroy(deployingUI);
        }
    }
}