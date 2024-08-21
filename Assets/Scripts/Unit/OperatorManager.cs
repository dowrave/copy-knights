using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;
using System.Runtime.CompilerServices;

public class OperatorManager : MonoBehaviour
{
    public static OperatorManager Instance { get; private set; }
    // UI 관련 변수
    public GameObject bottomPanelOperatorBoxPrefab; // 개별 오퍼레이터 아이콘, 배치 코스트 등을 감쌀 오퍼레이터 프리팹
    public RectTransform bottomPanel;

    // Operator 관련 변수
    public List<OperatorData> availableOperators = new List<OperatorData>();
    private Dictionary<OperatorData, BottomPanelOperatorBox> operatorUIBoxes = new Dictionary<OperatorData, BottomPanelOperatorBox>();
    private GameObject currentOperatorPrefab; // 현재 배치 중인 오퍼레이터 프리팹
    private OperatorData currentOperatorData; // 현재 배치 중인 오퍼레이터 정보
    private List<OperatorData> deployedOperators = new List<OperatorData>(); // 배치돼서 화면에 표시되지 않을 오퍼레이터
    private OperatorActionUI currentActiveActionUI;
    public OperatorActionUI CurrentActiveActionUI { get; private set; }

    // 하이라이트 관련 변수
    public Color availableTileColor = Color.green;
    public Color unavailableTileColor = Color.red;
    public Color attackRangeTileColor = new Color(255, 127, 0);

    // 배치 과정 중 어떤 상태인지에 대한 변수
    private bool isOperatorSelecting = false; // 하단 UI에서 오퍼레이터를 클릭한 상태
    private bool isDraggingOperator = false; // 타일 선택 상태 : 하단 UI에서 오퍼레이터를 MouseButtonDown한 상태로 드래그하고 있는 상태. 
    private bool isSelectingDirection = false; // 방향 선택 상태 : 타일은 정해졌고 오퍼레이터의 방향을 설정함
    private bool isMousePressed = false; // HandleDirectionSelection에서만 사용. 마우스가 클릭 중인지를 추적한다. 
    private int operatorIndex = -1; 
    private Vector3 placementDirection = Vector3.left;

    private List<Tile> highlightedTiles = new List<Tile>();
    private Tile currentHoverTile;
    private GameObject previewOperator;

    private float minDirectionDistance = 85f; // 직접 테스트해서 얻은 값
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

        // 여기에는 해당 상태일 때 계속 작동하고 있어야 하는 함수가 들어감
        // !! 상태를 변경할 때에만 작동되어야 하는 함수는 여기에 들어가면 안됨! !! 

        // 1. 하단 UI의 오퍼레이터 클릭 시 배치 가능한 타일들 하이라이트
        if (isOperatorSelecting)
        {
            HighlightAvailableTiles();
        }
        // 2. 오퍼레이터를 드래그 중일 때 (타일 설정 상태)
        else if (isDraggingOperator)
        {
            UpdatePreviewOperator();
        }
        // 3. 오퍼레이터의 방향을 정할 때 (방향 설정 상태)
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


    // BottomPanelOperatorBox 클릭 시 작동, 배치하려는 오퍼레이터의 정보를 변수에 넣는다.
    public void StartOperatorSelection(OperatorData operatorData)
    {
        // 현재 선택된 오퍼레이터가 없거나, 기존 선택된 오퍼레이터와 다른 오퍼레이터가 선택됐을 때만 동작
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
            isOperatorSelecting = false; // 드래그로 상태 변경
            isDraggingOperator = true;
            CreatePreviewOperator();
        }
    }

    public void HandleDragging(OperatorData operatorData)
    {
        if (isDraggingOperator && currentOperatorData == operatorData)
        {
            //UpdatePreviewOperator(); // 얘가 작동 안해도 OperatorManager.Update()에서 이미 동작하고 있음
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
        // 어떤 UI를 보여줄 경우 현재 활성화된 UI를 모두 비활성화해야 함
        if (currentActiveActionUI)
        {
            currentActiveActionUI.Hide(); // 이건 자식 오브젝트로 관리 중이라 숨겼다가 나타나게끔 함
        }

        if (deployingUI == null)
        {
            deployingUI = Instantiate(deployingUIPrefab);
        }

        deployingUI.Show(position);
    }

    // 방향 설정
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
                // 일정 거리 이상 커서 이동 시 배치
                if (dragDistance > minDirectionDistance)
                {
                    PlaceOperator(currentHoverTile);
                    EndDirectionSelection();
                    isMousePressed = false;
                }
                // 바운더리 이내라면 다시 방향 설정(클릭 X) 상태
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

    // 전체 배치 과정의 시작 : BottomPanelOperatorBox 클릭 시 호출됨 
    public void StartOperatorPlacement(OperatorData operatorData)
    {
        // 전역 배치 코스트가 오퍼레이터의 배치 코스트보다 높을 때에만 배치 가능
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
                Debug.LogError("오퍼레이터 데이터가 availableOperators List에 없음");
            }

        }
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
    /// 방향까지 결정된 뒤의 최종 배치 로직
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

            // 배치된 오퍼레이터 리스트에 추가
            deployedOperators.Add(currentOperatorData);

            // 하단 패널 UI에서 해당 오퍼레이터 박스 제거
            if (operatorUIBoxes.TryGetValue(currentOperatorData, out BottomPanelOperatorBox box))
            {
                box.gameObject.SetActive(false);
            }

            ResetPlacement();
        }
    }

    /// <summary>
    /// 배치 관련 조작 전으로 상태를 되돌림
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
        // 배치 UI가 떠있다면 제거, 배치 초기화
        if (deployingUI)
        {
            Destroy(deployingUI);
            ResetPlacement();
        }

        // 현재 선택된 ui와 기존 선택된 actionUI가 다른 경우라면 숨김(자식 오브젝트라 숨김)
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