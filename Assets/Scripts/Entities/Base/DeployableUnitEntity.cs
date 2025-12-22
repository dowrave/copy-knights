#nullable enable
using UnityEngine;
using System;

public abstract class DeployableUnitEntity : UnitEntity, IDeployable
{
    public DeployableInfo DeployableInfo { get; protected set; } = default!;

    [SerializeField] protected DeployableUnitData _deployableData;
    public DeployableUnitData DeployableUnitData => _deployableData;

    [HideInInspector]
    public DeployableUnitStats currentDeployableStats;

    public bool IsDeployed { get; protected set; }
    public int InitialDeploymentCost { get; protected set; } 

    protected bool isPreviewMode = false;
    public bool IsPreviewMode
    {
        get { return isPreviewMode; }
        protected set
        {
            isPreviewMode = value;
        }
    }
    protected Material originalMaterial = default!;
    protected Material previewMaterial = default!;

    private float preventInteractingTime = 0.1f;
    private float lastDeployTime;

    public Tile? CurrentTile { get; protected set; }

    public static event Action<DeployableUnitEntity> OnDeployed = delegate { };
    public static event Action<DeployableUnitEntity> OnDeployableDied = delegate { };

    protected override void Awake()
    {
        Faction = Faction.Ally;

        base.Awake();
    }

    protected virtual void Start()
    {
        AssignColorToRenderers(DeployableUnitData.PrimaryColor, DeployableUnitData.SecondaryColor);
    }

    public void Initialize(DeployableInfo deployableInfo)
    {
        DeployableInfo = deployableInfo;
        if (_deployableData != null)
        {
            _deployableData = deployableInfo.deployableUnitData!;

            currentDeployableStats = DeployableUnitData.Stats;
            SetPoolTag();
            SetPrefab();

            InitializeVisuals();
            InitializeDeployableProperties();
            UpdateCurrentTile();
        }
        else
        {
            return;
        }
    }

    protected override void SetPoolTag()
    {
        PoolTag = _deployableData.UnitTag;
    }

    public override void SetPrefab()
    {
        prefab = DeployableUnitData.Prefab;
    }


    protected virtual void InitializeDeployableProperties()
    {
        SetDeployState(false);
        InitialDeploymentCost = currentDeployableStats.DeploymentCost;
    }

    public virtual void Deploy(Vector3 position)
    {
        if (!IsDeployed)
        {
            SetDeployState(true);
            SetColliderState(true); 
            UpdateCurrentTile();
            if (CurrentTile != null)
            {
                CurrentTile.SetOccupied(this);
            }
            SetPosition(position);
            InitializeHP();
            lastDeployTime = Time.time;

            OnDeployed?.Invoke(this);
        }
    }

    protected void SetPosition(Vector3 worldPosition)
    {
        if (CurrentTile != null)
        {
            if (this is Operator)
            {
                transform.position = worldPosition + Vector3.up * (CurrentTile.GetHeightScale() / 2 + 0.5f);
            }
            else
            {
                transform.position = worldPosition + Vector3.up * (CurrentTile.GetHeightScale() / 2);
            }
        }
    }

    protected virtual void Undeploy()
    {
        IsDeployed = false;
        DeployableInfo.deployedDeployable = null;
        OnDeployableDied?.Invoke(this);
        DeployableManager.Instance!.OnDeployableRemoved(this);
        if (CurrentTile != null)
        {
            CurrentTile.ClearOccupied();
        }
    }

    public virtual void Retreat()
    {
        Debug.Log("DeployableUnitEntity의 Retreat 동작");

        if (IsDeployed)
        {
            Debug.Log("DeployableUnitEntity의 Retreat 동작, IsDeployed : true");
            Undeploy();
            base.DieInstantly();
        }
    }

    protected override void Die()
    {
        if (IsDeployed)
        {
            Undeploy();
            base.Die();
        }
    }

    public virtual bool CanDeployOnTile(Tile tile)
    {
        if (IsInvalidTile(tile)) return false;

        if (tile.TileData.Terrain == TileData.TerrainType.Ground && DeployableUnitData.CanDeployOnGround) return true;
        if (tile.TileData.Terrain == TileData.TerrainType.Hill && DeployableUnitData.CanDeployOnHill) return true;

        return false;
    }

    private bool IsInvalidTile(Tile tile)
    {
        return tile == null ||
        tile.TileData.IsStartPoint ||
        tile.TileData.IsEndPoint;
    }

    public void UpdatePreviewPosition(Vector3 position)
    {
        if (IsPreviewMode)
        {
            transform.position = position;
        }
    }

    public virtual void OnClick()
    {
        // 커서를 뗀 시점에 다시 클릭되는 현상 방지
        if (Time.time - lastDeployTime < preventInteractingTime)
        {
            DeployableManager.Instance!.CancelPlacement();
            return;
        }

        // 배치된 유닛 클릭
        if (IsDeployed &&
            !IsPreviewMode &&
            StageManager.Instance!.CurrentGameState == GameState.Battle
            )
        {
            DeployableManager.Instance!.CancelPlacement();

            if (IsPreviewMode == false)
            {
                DeploymentInputHandler.Instance!.SetIsSelectingDeployedUnit(true);
                StageManager.Instance!.SlowState = true;
                StageUIManager.Instance!.ShowDeployedInfo(this);
                ShowActionUI();
            }

        }
    }

    protected virtual void ShowActionUI()
    {
        DeployableManager.Instance!.ShowActionUI(this);
        StageUIManager.Instance!.ShowDeployedInfo(this);
    }

    protected override void InitializeHP()
    {
        MaxHealth = Mathf.Floor(currentDeployableStats.Health);
        CurrentHealth = Mathf.Floor(MaxHealth);
    }

    protected void SetDeployState(bool isDeployed)
    {
        IsDeployed = isDeployed;
        IsPreviewMode = !isDeployed;
    }

    protected virtual void UpdateCurrentTile()
    {
        Vector3 position = transform.position;
        Tile? newTile = MapManager.Instance!.GetTileAtWorldPosition(position);

        if (newTile != null && newTile != CurrentTile)
        {
            CurrentTile = newTile;
        }
    }
}

#nullable restore
