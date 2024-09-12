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
    // UI 관련 변수
    public GameObject bottomPanelDeployableBoxPrefab;
    public RectTransform bottomPanel;

    // Deployable 관련 변수
    public List<GameObject> availableDeployables = new List<GameObject>();
    private Dictionary<GameObject, BottomPanelDeployableBox> deployableUIBoxes = new Dictionary<GameObject, BottomPanelDeployableBox>();
    private GameObject currentDeployablePrefab;
    private DeployableUnitEntity currentDeployable;

    // 하이라이트 관련 변수
    public Color availableTileColor = new Color(0, 1, 0, 0.5f); 
    public Color attackRangeTileColor = new Color(255, 127, 0);

    // 배치 과정 중 어떤 상태인지에 대한 변수
    private bool isDeployableSelecting = false; // 하단 UI에서 오퍼레이터를 클릭한 상태
    private bool isDraggingDeployable = false; // 타일 선택 상태 : 하단 UI에서 오퍼레이터를 MouseButtonDown한 상태로 드래그하고 있는 상태. 
    private bool isSelectingDirection = false; // 방향 선택 상태 : 타일은 정해졌고 오퍼레이터의 방향을 설정함
    private bool isMousePressed = false; // HandleDirectionSelection에서만 사용. 마우스가 클릭 중인지를 추적한다. 
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

        // 여기에는 해당 상태일 때 계속 작동하고 있어야 하는 함수가 들어감
        // !! 상태를 변경할 때에만 작동되어야 하는 함수는 여기에 들어가면 안됨! !! 

        // 1. 하단 UI의 오퍼레이터 클릭 시 배치 가능한 타일들 하이라이트
        if (isDeployableSelecting)
        {
            HighlightAvailableTiles();
        }
        // 2. 오퍼레이터를 드래그 중일 때 (타일 설정 상태)
        else if (isDraggingDeployable)
        {
            UpdatePreviewDeployable();
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


    // BottomPanelOperatorBox 클릭 시 작동, 배치하려는 오퍼레이터의 정보를 변수에 넣는다.
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
        // 일관된 위치 구현하기
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
    /// 오퍼레이터 주위에 나타난 ActionUI, DeployingUI 제거
    /// OperatorInfoPanel을 숨기는 건 별개의 메서드
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

    // 방향 설정
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
                // 일정 거리 이상 커서 이동 시 배치
                if (dragDistance > minDirectionDistance)
                {
                    DeployDeployable(currentHoverTile);
                    isSelectingDirection = false;
                    IsMousePressed = false;
                    ResetPlacement();

                }
                // 바운더리 이내라면 다시 방향 설정(클릭 X) 상태
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

        (currentDeployable as MonoBehaviour).gameObject.SetActive(true);

        Tile hoveredTile = GetHoveredTile();

        // 배치 가능한 타일 위라면 타일 위치로 스냅
        if (hoveredTile != null && highlightedTiles.Contains(hoveredTile))
        {
            SetAboveTilePosition(currentDeployable, hoveredTile);
        }
        else
        {
            // 아니라면 커서 위치에만 표시
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
    /// 방향까지 결정된 뒤의 최종 배치 로직
    /// </summary>
    private void DeployDeployable(Tile tile)
    {
        if (StageManager.Instance.TryUseDeploymentCost(currentDeployable.DeploymentCost))
        {
            currentDeployable.Initialize(currentDeployable.Data); // 배치 시에 프리팹 참조 전달
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
    /// 배치 관련 조작 전으로 상태를 되돌림
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
        StageManager.Instance.UpdateTimeScale(); // 시간 원상복구
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
        // 현재 선택된 ui와 기존 선택된 actionUI가 다른 경우라면 숨김(자식 오브젝트라 숨김)
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
    /// 배치 중이거나, 배치된 오퍼레이터를 클릭한 상태를 취소하는 동작
    /// </summary>
    public void CancelCurrentAction()
    {
        if (currentUIState != UIState.None) // Action이거나 Deploying일 때
        {
            Debug.Log("CancelCurrentAction 동작");
            HideAllUIs();
            ResetPlacement();
            ResetHighlights();
        }
    }

    /// <summary>
    /// 오퍼레이터 위치 설정 후, 
    /// 마우스 버튼다운한 다음 배치를 위한 최소 드래그 길이(마우스 버튼업을 했을 때 배치되기 위한 최소 거리) 설정
    /// minDirectionDistance 값은 "스크린 상"에서의 길이가 된다.
    /// </summary>
    public void SetMinDirectionDistance(float screenDiamondRadius)
    {
        minDirectionDistance = screenDiamondRadius / 2; 
    }

    private bool CanPlaceOnTile(Tile tile)
    {
        // 타일이 배치 가능한가? + 타일이 하이라이트되었는가?
        return tile.CanPlaceDeployable() && highlightedTiles.Contains(tile);
    }

    // 타일 위에 배치되는 배치 가능한 요소의 위치 지정
    // 바리케이드가 붕 뜨길래 만들었음
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