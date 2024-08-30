using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;
using System.Runtime.CompilerServices;

public class OperatorManager : MonoBehaviour
{
    private enum UIState
    {
        None,
        OperatorAction,
        OperatorDeploying
    }

    private UIState currentUIState = UIState.None;

    public static OperatorManager Instance { get; private set; }
    // UI ���� ����
    public GameObject bottomPanelOperatorBoxPrefab; // ���� ���۷����� ������, ��ġ �ڽ�Ʈ ���� ���� ���۷����� ������
    public RectTransform bottomPanel;

    // Operator ���� ����
    public List<OperatorData> availableOperators = new List<OperatorData>();
    private Dictionary<OperatorData, BottomPanelOperatorBox> operatorUIBoxes = new Dictionary<OperatorData, BottomPanelOperatorBox>();
    private Operator currentOperator;
    private GameObject currentOperatorPrefab; // ���� ��ġ ���� ���۷����� ������
    private OperatorData currentOperatorData; // ���� ��ġ ���� ���۷����� ����
    private List<OperatorData> deployedOperators = new List<OperatorData>(); // ��ġ�ż� ȭ�鿡 ǥ�õ��� ���� ���۷�����

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

    private float minDirectionDistance;
    private const float INDICATOR_SIZE = 2.5f;

    [SerializeField] private LayerMask tileLayerMask;
    [SerializeField] private OperatorDeployingUI deployingUIPrefab;
    [SerializeField] private OperatorActionUI actionUIPrefab;

    private OperatorDeployingUI currentDeployingUI;
    private OperatorActionUI currentActionUI;

    private const float PLACEMENT_TIME_SCALE = 0.2f;
    private float originalTimeScale = 1f;


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
            HideAllUIs();
            ResetPlacement();
            currentOperatorData = operatorData;
            currentOperatorPrefab = operatorData.prefab;
            isOperatorSelecting = true;

            if (currentOperator != null)
            {
                Destroy(currentOperator.gameObject);
            }

            currentOperator = Instantiate(currentOperatorPrefab, Vector3.zero, Quaternion.identity).GetComponent<Operator>();
            currentOperator.IsPreviewMode = true;
            currentOperator.gameObject.SetActive(false);

            HighlightAvailableTiles();

            Debug.Log($"OperatorData : {operatorData}");
            ShowOperatorInfoPanel(operatorData);

            // OverlayPanel�� CancelOperatorSelection �̺�Ʈ ���(�� ���� Ŭ�� �� ��ġ ���� ��ҵ�)
            //UIManager.Instance.ActivateOverlay(() => CancelOperatorSelection());
        }
    }

    
    public void StartDragging(OperatorData operatorData)
    {
        if (currentOperatorData == operatorData)
        {
            isOperatorSelecting = false; // �巡�׷� ���� ����
            isDraggingOperator = true;
            CreatePreviewOperator();
            SlowDownTime(); // �ð� ������ �����
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
                HideOperatorInfoPanel();
            }
        }
    }

    private void CreatePreviewOperator()
    {
        if (currentOperator == null)
        {
            currentOperator.IsPreviewMode = true;
            SetPreviewTransparency(0.5f);
            //previewOperator = Instantiate(currentOperatorPrefab, Vector3.zero, Quaternion.identity);
            //Operator previewOp = previewOperator.GetComponent<Operator>();
            //previewOp.IsPreviewMode = true;
        }
    }

    private void StartDirectionSelection(Tile tile)
    {
        isSelectingDirection = true;
        ResetHighlights();
        currentHoverTile = tile;

        if (currentOperator != null)
        {
            currentOperator.transform.position = tile.transform.position + Vector3.up * 0.5f;
            currentOperator.ShowDirectionIndicator(true);
        }

        ShowDeployingUI(tile.transform.position + Vector3.up * 0.5f);
        UpdatePreviewOperatorRotation();
    }

    public void ShowActionUI(Operator op)
    {
        HideAllUIs();
        currentActionUI = Instantiate(actionUIPrefab, op.transform.position, Quaternion.identity);
        currentActionUI.Initialize(op);
        currentUIState = UIState.OperatorAction;

        //UIManager.Instance.ShowOperatorActionUI();
    }

    public void ShowDeployingUI(Vector3 position)
    {
        HideAllUIs();
        currentDeployingUI = Instantiate(deployingUIPrefab, position, Quaternion.identity);
        currentDeployingUI.Initialize(currentOperatorData);
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
            //HighlightAttackRange(currentHoverTile, placementDirection);
            if (currentOperator != null)
            {
                currentOperator.SetDirection(placementDirection);
                currentOperator.HighlightAttackRange();
            }

            UpdatePreviewOperatorRotation();

            if (Input.GetMouseButtonUp(0))
            {
                // ���� �Ÿ� �̻� Ŀ�� �̵� �� ��ġ
                if (dragDistance > minDirectionDistance)
                {
                    DeployOperator(currentHoverTile);
                    isSelectingDirection = false;
                    isMousePressed = false;
                    ResetPlacement();
                    
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

        currentOperator.gameObject.SetActive(true);

        Tile hoveredTile = GetHoveredTile();

        // ��ġ ������ Ÿ�� ����� Ÿ�� ��ġ�� ����
        if (hoveredTile != null && highlightedTiles.Contains(hoveredTile))
        {
            currentOperator.transform.position = hoveredTile.transform.position + Vector3.up * 0.5f;
        }
        else
        {
            // �ƴ϶�� Ŀ�� ��ġ���� ǥ��
            currentOperator.transform.position = cursorWorldPosition;
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
    private void DeployOperator(Tile tile)
    {
        if (StageManager.Instance.TryUseDeploymentCost((int)currentOperatorData.deploymentCost))
        {
            if (currentOperator != null)
            {
                currentOperator.IsPreviewMode = false;
                currentOperator.Deploy(tile.transform.position + Vector3.up * 0.5f, placementDirection);
                tile.SetOccupied(currentOperator);
                SetPreviewTransparency(1f);
            }

            // ��ġ�� ���۷����� ����Ʈ�� �߰�
            deployedOperators.Add(currentOperatorData);

            // �ϴ� �г� UI���� �ش� ���۷����� �ڽ� ����
            if (operatorUIBoxes.TryGetValue(currentOperatorData, out BottomPanelOperatorBox box))
            {
                box.gameObject.SetActive(false);
            }

            ResetPlacement(); // ���� �ʱ�ȭ
            RestoreNormalTime(); // �ð� �帧 �������� �ǵ���
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
        if (currentOperator != null)
        {
            if (currentOperator.IsPreviewMode)
            {
                Destroy(currentOperator.gameObject);
            }
            currentOperator = null;
        }
        operatorIndex = -1;

        HideOperatorInfoPanel();
        RestoreNormalTime();
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
        Renderer renderer = currentOperator.GetComponentInChildren<Renderer>();
        Material mat = renderer.material;
        Color color = mat.color;
        color.a = alpha;
        mat.color = color;
    }

    private void UpdatePreviewOperatorRotation()
    {
        if (currentOperator != null)
        {
            currentOperator.transform.rotation = Quaternion.LookRotation(placementDirection);
        }
    }

    // �� / ��� �� ȣ��
    public void OnOperatorRemoved(OperatorData operatorData)
    {
        deployedOperators.Remove(operatorData);
        HideOperatorInfoPanel();
        HideAllUIs();
        ResetHighlights();

        if (operatorUIBoxes.TryGetValue(operatorData, out BottomPanelOperatorBox box))
        {
            box.StartCooldown(operatorData.reDeployTime);
        }
    }

    public void SetActiveActionUI(OperatorActionUI ui)
    {
        // ���� ���õ� ui�� ���� ���õ� actionUI�� �ٸ� ����� ����(�ڽ� ������Ʈ�� ����)
        if (currentActionUI != null && currentActionUI != ui)
        {
            Destroy(currentActionUI);
        }

        currentActionUI = ui;
    }

    public void CancelOperatorSelection()
    {
        CancelCurrentAction();
        ResetPlacement();
    }

    private void SlowDownTime()
    {
        Time.timeScale = PLACEMENT_TIME_SCALE;
    }

    private void RestoreNormalTime()
    {
        Time.timeScale = originalTimeScale;
    }

    public void UpdateOperatorDirection(Operator op, Vector3 direction)
    {
        if (op != null)
        {
            op.SetDirection(direction);
        }
    }

    public void ShowOperatorInfoPanel(OperatorData operatorData)
    {
        UIManager.Instance.ShowOperatorInfo(operatorData);
    }

    public void HideOperatorInfoPanel()
    {
        UIManager.Instance.HideOperatorInfo();
    }

    /// <summary>
    /// ��ġ ���̰ų�, ��ġ�� ���۷����͸� Ŭ���� ���¸� ����ϴ� ����
    /// </summary>
    public void CancelCurrentAction()
    {
        Debug.Log("CancelCurrentAction ����");
        if (currentUIState != UIState.None)
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
}