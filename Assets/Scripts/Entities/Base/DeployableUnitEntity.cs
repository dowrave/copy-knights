#nullable enable
using UnityEngine;
using System;

public abstract class DeployableUnitEntity : UnitEntity, IDeployable
{
    public DeployableManager.DeployableInfo DeployableInfo { get; protected set; } = default!;

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

    protected override void Awake()
    {
        Faction = Faction.Ally; 
        base.Awake();
    }

    protected virtual void Start()
    {
        AssignColorToRenderers(DeployableUnitData.PrimaryColor, DeployableUnitData.SecondaryColor);
    }

    public void Initialize(DeployableManager.DeployableInfo deployableInfo)
    {
        DeployableInfo = deployableInfo;
        if (_deployableData != null)
        {
            _deployableData = deployableInfo.deployableUnitData!;

            currentDeployableStats = DeployableUnitData.stats;
            SetPoolTag();
            SetPrefab();

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
        PoolTag = _deployableData.GetUnitTag();
    }

    public override void SetPrefab()
    {
        prefab = DeployableUnitData.prefab;
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

    public virtual void Retreat()
    {
        if (IsDeployed)
        {
            IsDeployed = false;
            DeployableInfo.deployedDeployable = null;
            DeployableManager.Instance!.OnDeployableRemoved(this);
            if (CurrentTile != null)
            {
                CurrentTile.ClearOccupied();
            }

            base.DieInstantly();
        }
    }

    protected override void Die()
    {
        if (IsDeployed)
        {
            IsDeployed = false;
            DeployableInfo.deployedDeployable = null;
            DeployableManager.Instance!.OnDeployableRemoved(this);
            if (CurrentTile != null)
            {
                CurrentTile.ClearOccupied();
            }
            base.Die();
        }
    }

    public virtual bool CanDeployOnTile(Tile tile)
    {
        if (IsInvalidTile(tile)) return false;

        if (tile.data.terrain == TileData.TerrainType.Ground && DeployableUnitData.canDeployOnGround) return true;
        if (tile.data.terrain == TileData.TerrainType.Hill && DeployableUnitData.canDeployOnHill) return true;

        return false;
    }

    private bool IsInvalidTile(Tile tile)
    {
        return tile == null ||
        tile.data.isStartPoint ||
        tile.data.isEndPoint;
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
        // ?? ??
        if (Time.time - lastDeployTime < preventInteractingTime)
        {
            DeployableManager.Instance!.CancelPlacement();
            return;
        }

        
        if (IsDeployed &&
            !IsPreviewMode &&
            StageManager.Instance!.currentState == GameState.Battle
            )
        {
            DeployableManager.Instance!.CancelPlacement();

            if (IsPreviewMode == false)
            {
                StageUIManager.Instance!.ShowDeployedInfo(this);
            }

            ShowActionUI();
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
