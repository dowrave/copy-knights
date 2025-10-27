using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

// ��ġ ������ ��ҵ��� ��ġ ������ �����
public class DeployableManager : MonoBehaviour
{
    public static DeployableManager? Instance { get; private set; }

    // UI ���� ����
    public GameObject DeployableBoxPrefab = default!;
    public RectTransform bottomPanel = default!;

    // Deployable ���� ����
    private List<DeployableInfo> allDeployables = new List<DeployableInfo>(); 
    private Dictionary<DeployableInfo, DeployableBox> deployableUIBoxes = new Dictionary<DeployableInfo, DeployableBox>();

    // --- ���� ������ ---
    public DeployableInfo? CurrentDeployableInfo { get; private set; }
    public DeployableBox? CurrentDeployableBox { get; private set; }
    public GameObject? CurrentDeployableObject { get; private set; }
    public DeployableUnitEntity? CurrentDeployableEntity { get; private set; }
    private List<Tile> highlightedTiles = new List<Tile>();

    // ��ġ ���� ���� ���� ��ġ ��
    public int MaxOperatorDeploymentCount { get; private set; }
    private int _currentOperatorDeploymentCount = 0; 
    public int CurrentOperatorDeploymentCount
    {
        get { return _currentOperatorDeploymentCount; }
        private set
        {
            if (_currentOperatorDeploymentCount != value) // ���� ����� ������
            {
                _currentOperatorDeploymentCount = value;
                OnCurrentOperatorDeploymentCountChanged?.Invoke();
            }
        }
    }
                                                    
    // �� DeployableInfo�� ���� ���� ���¸� ����
    private Dictionary<DeployableInfo, DeployableUnitState> unitStates = new Dictionary<DeployableInfo, DeployableUnitState>();
    public Dictionary<DeployableInfo, DeployableUnitState> UnitStates => unitStates;

    [Header("References")]
    [SerializeField] private LayerMask tileLayerMask = default!;
    [SerializeField] private DeployableDeployingUI? currentDeployingUI;
    [SerializeField] private DeployableActionUI? currentActionUI;
    [SerializeField] private Camera mainCamera;

    [Header("Highlight Color")]
    // ���̶���Ʈ ���� ���� - �ν����Ϳ��� ����
    public Color attackRangeTileColor;

    public int CurrentDeploymentOrder { get; private set; } = 0;

    private Tile? currentHoverTile;

    private List<DeployableUnitEntity> deployedItems = new List<DeployableUnitEntity>();

    // ��ƼƼ �̸��� deployableInfo�� �����ϴ� ��ųʸ�
    private static Dictionary<string, DeployableInfo> deployableInfoMap = new Dictionary<string, DeployableInfo>();

    // �̺�Ʈ
    public event Action OnCurrentOperatorDeploymentCountChanged = delegate { };

     
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

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        OperatorIconHelper.OnIconDataInitialized += InitializeDeployableUI;
    }

    public void Initialize(
        List<SquadOperatorInfo> squadData,
        List<MapDeployableData> stageDeployables,
        int maxOperatorDeploymentCount
        )
    {
        allDeployables.Clear();
        deployableInfoMap.Clear();

        // OwnedOperator -> DeployableInfo�� ��ȯ�ؼ� �߰�
        foreach (SquadOperatorInfo opInfo in squadData.Where(op => op != null))
        {
            OwnedOperator op = opInfo.op;

            var info = new DeployableInfo
            {
                prefab = op.OperatorProgressData.prefab,
                poolTag = op.OperatorProgressData.GetUnitTag(),
                maxDeployCount = 1,
                redeployTime = op.OperatorProgressData.stats.RedeployTime,
                ownedOperator = op,
                skillIndex = opInfo.skillIndex,
                operatorData = op.OperatorProgressData,
            };

            allDeployables.Add(info);

            InstanceValidator.ValidateInstance(op.OperatorProgressData);
            // (���۷����� ��ƼƼ �̸� - ��ġ ����) ����
            deployableInfoMap[op.OperatorProgressData!.entityName!] = info;
        }

        // �������� ���� ��� -> DeployableInfo�� ��ȯ
        foreach (var deployable in stageDeployables)
        {
            var info = deployable.ToDeployableInfo();
            allDeployables.Add(info);

            InstanceValidator.ValidateInstance(deployable.DeployableData);

            // (��ġ ��� �̸� - ��ġ ����) ����
            deployableInfoMap[deployable.DeployableData!.entityName!] = info; 
        }

        MaxOperatorDeploymentCount = maxOperatorDeploymentCount;

        InitializeDeployableUI();
    }

    // DeployableBox�� Deployable ��� ������ �Ҵ�
    private void InitializeDeployableUI()
    {
        if (DeployableBoxPrefab == null) throw new InvalidOperationException("deployableBoxPrefab�� �Ҵ���� ����");

        foreach (var deployableInfo in allDeployables)
        {
            // deployableInfo�� ���� �� ���� ���� ����
            unitStates[deployableInfo] = new DeployableUnitState(deployableInfo);

            GameObject boxObject = Instantiate(DeployableBoxPrefab, bottomPanel);
            DeployableBox box = boxObject.GetComponent<DeployableBox>();

            // �̸� ����
            if (deployableInfo.operatorData != null)
            {
                box.gameObject.name = $"DeployableBox({deployableInfo.operatorData.entityName})";
            }
            else
            {
                box.gameObject.name = $"DeployableBox({deployableInfo.deployableUnitData.entityName})";
            }


            if (box != null)
            {
                box.Initialize(deployableInfo);
                deployableUIBoxes[deployableInfo] = box;
                box.UpdateVisuals();
            }
        }
    }

    private void Update()
    {
        if (StageManager.Instance == null) throw new InvalidOperationException("StageManager.Instance�� null��");

        if (StageManager.Instance.currentState != GameState.Battle) { return; }

        UpdateDeployableStateCooldown();
    }

    private void UpdateDeployableStateCooldown()
    {
        foreach (DeployableUnitState unitState in unitStates.Values)
        {
            unitState.UpdateCooldown();
        }
    }

    // ��ġ ������ Ÿ���� ���̶���Ʈ
    public void HighlightAvailableTiles()
    {
        ResetHighlights();
        if (MapManager.Instance == null)
        {
            throw new InvalidOperationException("�� �Ŵ��� �ν��Ͻ��� �ʱ�ȭ���� �ʾ���");
        }


        foreach (Tile tile in MapManager.Instance.GetAllTiles())
        {
            if (tile != null && tile.CanPlaceDeployable())
            {
                if (CheckTileCondition(tile))
                {
                    highlightedTiles.Add(tile);
                    tile.Highlight();
                }
            }
        }
    }


    // Ÿ�� ���� üũ
    private bool CheckTileCondition(Tile tile)
    {
        if (CurrentDeployableInfo?.operatorData != null)
        {
            return (tile.data.terrain == TileData.TerrainType.Ground && CurrentDeployableInfo.operatorData.canDeployOnGround) ||
                    (tile.data.terrain == TileData.TerrainType.Hill && CurrentDeployableInfo.operatorData.canDeployOnHill);
        }
        else if (CurrentDeployableInfo?.deployableUnitData != null)
        {
            return (tile.data.terrain == TileData.TerrainType.Ground && CurrentDeployableInfo.deployableUnitData.canDeployOnGround) ||
                    (tile.data.terrain == TileData.TerrainType.Hill && CurrentDeployableInfo.deployableUnitData.canDeployOnHill);
        }
        else
            return false;
    }

    // BottomPanelOperatorBox ���콺��ư �ٿ� �� �۵�, ��ġ�Ϸ��� ���۷������� ������ ������ �ִ´�.
    public bool StartDeployableSelection(DeployableInfo deployableInfo)
    {
        ResetPlacement();
        SetCurrentDeployableInfo(deployableInfo);
        StageUIManager.Instance!.ShowUndeployedInfo(CurrentDeployableInfo);

        // �ڽ� ���� ����
        CurrentDeployableBox.Select();

        // ���̶���Ʈ ó��
        HighlightAvailableTiles();

        return true;
    }
    
    private void SetCurrentDeployableInfo(DeployableInfo deployableInfo)
    {
        if (deployableInfo != null)
        {
            CurrentDeployableInfo = deployableInfo;

            CurrentDeployableEntity = CurrentDeployableInfo.deployedOperator != null ?
                CurrentDeployableInfo.deployedOperator :
                CurrentDeployableInfo.deployedDeployable;
            if (CurrentDeployableInfo == null) throw new InvalidOperationException("CurrentDeployableInfo�� null��");

            CurrentDeployableBox = deployableUIBoxes[CurrentDeployableInfo];
            if (CurrentDeployableBox == null) throw new InvalidOperationException("CurrentDeployableBox�� null��");
        }
    }

    public void CreatePreviewDeployable()
    {
        InstanceValidator.ValidateInstance(CurrentDeployableInfo);

        CurrentDeployableObject = ObjectPoolManager.Instance.SpawnFromPool(CurrentDeployableInfo.poolTag, Vector3.zero, Quaternion.identity);
        CurrentDeployableEntity = CurrentDeployableObject.GetComponent<DeployableUnitEntity>();

        if (CurrentDeployableEntity is Operator op)
        {
            op.Initialize(CurrentDeployableInfo!);
        }
        else
        {
            CurrentDeployableEntity!.Initialize(CurrentDeployableInfo!);
        }

    }

    public void ShowActionUI(DeployableUnitEntity deployable)
    {
        HideOperatorUIs();

        // ��ġ ���� �� Ȱ��ȭ
        Vector3 ActionUIPosition = new Vector3(deployable.transform.position.x, 1f, deployable.transform.position.z);
        // currentActionUI = Instantiate(actionUIPrefab, ActionUIPosition, Quaternion.identity);
        currentActionUI.transform.position = ActionUIPosition;
        currentActionUI.gameObject.SetActive(true);
        currentActionUI.Initialize(deployable);

        // currentUIState = UIState.OperatorAction;
    }

    public void ShowDeployingUI(Vector3 position)
    {
        InstanceValidator.ValidateInstance(CurrentDeployableEntity);

        HideOperatorUIs();

        currentDeployingUI.transform.position = position;
        currentDeployingUI.gameObject.SetActive(true);
        currentDeployingUI.Initialize(CurrentDeployableEntity);
    }


    // ���۷����� ������ ��Ÿ�� UI ����
    private void HideOperatorUIs()
    {
        // null�̾ ��� ����

        if (currentActionUI != null)
        {
            currentActionUI.gameObject.SetActive(false);
        }

        if (currentDeployingUI != null)
        {
            currentDeployingUI.gameObject.SetActive(false);
        }
    }

    private void HideOperatorUIsOnCondition(DeployableUnitEntity deployable)
    {
        if  (currentActionUI != null && currentActionUI.Deployable == deployable)
        {
            currentActionUI.gameObject.SetActive(false);
        }

        if (currentDeployingUI != null && currentDeployingUI.Deployable == deployable)
        {
            currentDeployingUI.gameObject.SetActive(false);
        }
    }

    public void UpdatePreviewDeployable()
    {
        if (CurrentDeployableEntity == null) throw new InvalidOperationException("currentDeployable�� null��");

        Tile? hoveredTile = GetHoveredTile();

        // ��ġ ������ Ÿ�� ����� Ÿ�� ��ġ�� ����
        if (hoveredTile != null && highlightedTiles.Contains(hoveredTile))
        {
            SetAboveTilePosition(CurrentDeployableEntity, hoveredTile);
        }
        // ��ġ �Ұ����� Ÿ���̶�� Ŀ�� ��ġ�� ǥ��
        else
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
            Vector3 cursorWorldPosition;

            if (groundPlane.Raycast(ray, out float distance))
            {
                cursorWorldPosition = ray.GetPoint(distance) + Vector3.up * 0.5f;
            }
            else
            {
                cursorWorldPosition = mainCamera.transform.position + mainCamera.transform.forward * 10f + Vector3.up * 0.5f;
            }

            CurrentDeployableEntity.transform.position = cursorWorldPosition;
        }
    }

    public Tile? GetHoveredTile()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Tile")))
        {
            // return hit.collider.GetComponentInParent<Tile>();
            return hit.collider.GetComponent<Tile>();

        }

        return null;
    }

    public void HighlightAttackRanges(List<Tile> tiles, bool isMedic)
    {
        ResetHighlights();
        foreach (Tile tile in tiles)
        {
            tile.ShowAttackRange(isMedic);
            highlightedTiles.Add(tile);
        }
    }

    public Vector3 DetermineDirection(Vector3 dragVector)
    {
        float angle = Mathf.Atan2(dragVector.y, dragVector.x) * Mathf.Rad2Deg;
        if (angle < 45 && angle >= -45) return Vector3.right;
        if (angle < 135 && angle >= 45) return Vector3.forward;
        if (angle >= 135 || angle < -135) return Vector3.left;
        return Vector3.back;
    }


    // ��ġ�Ǵ� ��� �����ϴ� �޼���
    public void DeployDeployable(Tile tile, Vector3 placementDirection)
    {
        // �⺻���� Vector3.zero�� ����, �� ���� ������ ��ġ���� ����
        if (placementDirection == Vector3.zero) return;

        InstanceValidator.ValidateInstance(CurrentDeployableEntity);
        InstanceValidator.ValidateInstance(CurrentDeployableInfo);
        InstanceValidator.ValidateInstance(StageManager.Instance);

        DeployableUnitState gameState = unitStates[CurrentDeployableInfo!];
        int cost = gameState.CurrentDeploymentCost;

        // �ڽ�Ʈ ���� ���� & ��ġ ���� ����
        if (StageManager.Instance!.TryUseDeploymentCost(cost) && gameState.OnDeploy(CurrentDeployableEntity!))
        {
            if (CurrentDeployableEntity is Operator op)
            {
                op.Deploy(tile.transform.position);
                op.SetDirection(placementDirection);
                
                CurrentOperatorDeploymentCount++;
                if (CurrentDeployableBox != null)
                {
                    CurrentDeployableBox.Deselect();
                    CurrentDeployableBox = null;
                }
            }
            else
            {
                CurrentDeployableEntity!.Deploy(tile.transform.position);
            }
        }

        // ��ġ�� ���� ��Ͽ� �߰�
        deployedItems.Add(CurrentDeployableEntity!);
        UpdateDeployableUI(CurrentDeployableInfo!);
        ResetPlacement();
        GameManagement.Instance.TimeManager.UpdateTimeScale();
    }
    
    private void UpdateDeployableUI(DeployableInfo info)
    {
        if (deployableUIBoxes.TryGetValue(info, out DeployableBox box))
        {
            box.UpdateVisuals();
        }
    }


    // ��ġ ���� ������ ���¸� �ǵ���
    private void ResetPlacement()
    {
        InstanceValidator.ValidateInstance(StageManager.Instance);
        InstanceValidator.ValidateInstance(StageUIManager.Instance);

        DeploymentInputHandler.Instance!.ResetState();

        // CurrentDeployableInfo ���� ������
        if (CurrentDeployableEntity != null)
        {
            // �̸����� ���� ���� �ش� ������Ʈ ��ȯ
            if (CurrentDeployableEntity.IsPreviewMode)
            {
                ObjectPoolManager.Instance.ReturnToPool(CurrentDeployableInfo.poolTag, CurrentDeployableObject);
                Debug.Log($"[ResetPlacement] - {CurrentDeployableObject} Ǯ�� ��ȯ��");
            }
            CurrentDeployableEntity = null;
        }
        if (CurrentDeployableBox != null)
        {
            CurrentDeployableBox.Deselect();
            CurrentDeployableBox = null;
        }

        CurrentDeployableObject = null;
        CurrentDeployableInfo = null;

        StageUIManager.Instance!.HideDeployableInfo();
        GameManagement.Instance!.TimeManager.UpdateTimeScale();

        ResetHighlights();
        HideOperatorUIs();
    }

    public void CancelPlacement()
    {
        ResetPlacement();
    }

    public void ResetHighlights()
    {
        foreach (Tile tile in highlightedTiles)
        {
            tile.HideAttackRange();
            tile.ResetHighlight();
        }
        highlightedTiles.Clear();
    }

    public void UpdatePreviewRotation(Vector3 placementDirection)
    {
        if (CurrentDeployableEntity != null)
        {
            CurrentDeployableEntity.transform.rotation = Quaternion.LookRotation(placementDirection);
        }
    }

    // ��ġ�� ��Ұ� ���ŵǾ��� �� ������
    public void OnDeployableRemoved(DeployableUnitEntity deployable)
    {
        deployedItems.Remove(deployable);

        // ���� deployable�� ���� ������ �����ְ� �ִٸ� ����
        StageUIManager.Instance.HideInfoPanelIfDisplaying(deployable);
        HideOperatorUIsOnCondition(deployable);

        ResetHighlights();

        // �ڽ� �����. ���� ���� ������ ����
        if (StageManager.Instance != null && StageManager.Instance.currentState == GameState.Battle)
        {
            DeployableInfo? info;
            if (deployable is Operator op)
            {
                CurrentOperatorDeploymentCount--;
                InstanceValidator.ValidateInstance(op.OperatorData);
                info = GetDeployableInfoByName(op.OperatorData?.entityName!);
            }
            else
            {
                InstanceValidator.ValidateInstance(deployable.DeployableUnitData);
                info = GetDeployableInfoByName(deployable.DeployableUnitData.entityName!);
            }

            if (info != null && unitStates.TryGetValue(info, out var unitState))
            {
                unitState.OnRemoved(); // ���� ������ ���� ������Ʈ

                if (deployableUIBoxes.TryGetValue(info, out DeployableBox box))
                {
                    box.UpdateVisuals();
                    box.gameObject.SetActive(true);
                }
            }
        }
    }

    public void SetActiveActionUI(DeployableActionUI ui)
    {
        // ���� ���õ� ui�� ���� ���õ� actionUI�� �ٸ� ����� ����
        if (currentActionUI != null && currentActionUI != ui)
        {
            // Destroy(currentActionUI);
            currentActionUI.gameObject.SetActive(false);
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

    // ��ġ ���̰ų�, ��ġ�� ���۷����͸� Ŭ���� ���¸� ����ϴ� ����
    public void CancelCurrentAction()
    {
        HideOperatorUIs();
        ResetPlacement();
        ResetHighlights();

        // �ڽ����� �ִϸ��̼� �ʱ�ȭ
        foreach (var box in deployableUIBoxes.Values)
        {
            box.ResetAnimation();
        }
    }

    public bool CanPlaceOnTile(Tile tile)
    {
        return tile.CanPlaceDeployable() && // ��ġ ������ Ÿ���ΰ�
            highlightedTiles.Contains(tile); // ���̶���Ʈ�� Ÿ���ΰ�
    }

    // Ÿ�� ���� ��ġ�Ǵ� ��ġ ������ ����� ��ġ ����
    public void SetAboveTilePosition(DeployableUnitEntity deployable, Tile tile)
    {
        if (CurrentDeployableEntity is Operator op)
        {
            CurrentDeployableEntity.transform.position = tile.transform.position + Vector3.up * 0.5f;
            op.SetGridPosition();
        }
        else
        {
            deployable.transform.position = tile.transform.position + Vector3.up * 0.1f;
        }
    }

    public void UpdateDeploymentOrder()
    {
        CurrentDeploymentOrder += 1;
    }

    public DeployableInfo? GetDeployableInfoByName(string entityName)
    {
        return deployableInfoMap.TryGetValue(entityName, out var info) ? info : null;
    }


    private void OnDestroy()
    {
        OperatorIconHelper.OnIconDataInitialized -= InitializeDeployableUI;
    }
}