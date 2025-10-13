#nullable enable
using UnityEngine;
using System;

public abstract class DeployableUnitEntity : UnitEntity, IDeployable
{
    public DeployableManager.DeployableInfo DeployableInfo { get; protected set; } = default!;
    public DeployableUnitData DeployableUnitData { get; private set; } = default!;

    [HideInInspector]
    public DeployableUnitStats currentDeployableStats;


    public bool IsDeployed { get; protected set; }
    public int InitialDeploymentCost { get; protected set; } // ???? ©ö??¢® ??¨ö¨¬¨¡¢ç - DeploymentCost¢¥? ¡Æ??? ?©¬ ??¡Æ¢®?? ¨ù? ???¨ö

    // [SerializeField] private Collider _mainCollider;

    // ©ö?¢¬¢ç¨¬¢¬¡¾? ¡Æ?¡¤?
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

    // ©ö??¢® ¢¯?¡¤? ?? ?¢¯¨ù¡©¢¬? ¢Ò¨ú ?¡×?¢®¡Æ¢® ¢¯?¨¡?¡¤©ö???? ?¡×?¢®?? ¢Ò¡× ActionUI¡Æ¢® ©ø¨£?¢¬©ø©÷?? ©ö©¡????¡¾? ?¡×?? ¨¬?¨ù???
    private float preventInteractingTime = 0.1f;
    private float lastDeployTime;

    public Tile? CurrentTile { get; protected set; } // "©ö??¢® ?©¬"??¢Ò?¢¥? ¡Æ??¢´?? ??¡¾? ??©ö¢ç¢¯¢® nullable

    public static event Action<DeployableUnitEntity> OnDeployed = delegate { };

    protected override void Awake()
    {
        Faction = Faction.Ally; // ©ö??¢® ¡Æ¢®¢¥??? ¢¯?¨ù?¢¥? ¢¬©£?? ¨ú¨¡¡¾¨¬?¢¬¡¤? ¡Æ???
        base.Awake();
    }

    protected virtual void Start()
    {
        AssignColorToRenderers(DeployableUnitData.PrimaryColor, DeployableUnitData.SecondaryColor);
    }

    public void Initialize(DeployableManager.DeployableInfo deployableInfo)
    {
        DeployableInfo = deployableInfo;
        if (deployableInfo.deployableUnitData != null)
        {
            DeployableUnitData = deployableInfo.deployableUnitData!;

            currentDeployableStats = DeployableUnitData.stats;
            SetPrefab();

            InitializeDeployableProperties();
            UpdateCurrentTile();
        }
        else
        {
            Debug.LogError("BaseData¢¯¢® ??¢¥??? ¡Æ¨£?? ¨ú©ª?¨ö!");
            return;
        }
    }

    public override void SetPrefab()
    {
        prefab = DeployableUnitData.prefab;
    }


    // ??¨ö? ¢¯?¨¬??¡×¨¡¢ç¢¯¢® ¨ö?¡Æ??¡©¢¬? ¢¥?¢¥???¢¥? Model?? ??¢¥?¢¥? ????
    protected virtual void InitializeDeployableProperties()
    {
        SetDeployState(false);
        InitialDeploymentCost = currentDeployableStats.DeploymentCost; // ??¡¾? ©ö??¢® ??¨ö¨¬¨¡¢ç ¨ù©ø?¢´
    }

    public virtual void Deploy(Vector3 position)
    {
        if (!IsDeployed)
        {
            SetDeployState(true);
            SetColliderState(true); // ??¢Ò???¢¥? ??
            UpdateCurrentTile();
            if (CurrentTile != null)
            {
                CurrentTile.SetOccupied(this);
            }
            SetPosition(position);
            InitializeHP();
            lastDeployTime = Time.time;

            OnDeployed?.Invoke(this);
            Debug.Log("OnDeployed ??¨¬?¨¡¢ç ©ö©¬??");
        }
    }

    // ?¢¬?? ?¡×¢¯¢®¨ù¡©?? ¨ö??? ©ö??¢® ?¡×?¢® ?¢Ò?¢´
    protected void SetPosition(Vector3 worldPosition)
    {
        if (CurrentTile != null)
        {
            // ¢¯?¨¡?¡¤©ö????¢¥? ???? ¢Ò?¢¯?¨ù¡© ©ö??¢®
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
        Die();
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
                CurrentTile.ClearOccupied(); // ?¢¬??¢¯¢® ©ö??¢®?? ¢¯?¨ù? ??¡Æ?
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
        // tile.IsOccupied ||
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

    // ©ö??¢®?? ??¢¥? ??¢¬? ¨ö? ?¢¯??
    public virtual void OnClick()
    {
        // ©ö??¢® ?¡À?? ??¢¬? ©ö©¡??
        if (Time.time - lastDeployTime < preventInteractingTime)
        {
            DeployableManager.Instance!.CancelPlacement();
            return;
        }

        // ©ö??¢®?? ¢¯?¨¡?¡¤©ö???? ??¢¬? ?¢¯??
        if (IsDeployed &&
            !IsPreviewMode &&
            StageManager.Instance!.currentState == GameState.Battle // ?¡¿¨ö¨¬¨¡¢ç ?©¬)
            )
        {
            DeployableManager.Instance!.CancelPlacement();

            // ©ö?¢¬¢ç¨¬¢¬¡¾? ????¢¯¢®¨ù¡¾ ?¢¯?? X
            if (IsPreviewMode == false)
            {
                //DebugDeployableInfo();
                StageUIManager.Instance!.ShowDeployedInfo(this);
            }

            ShowActionUI();
        }
    }

    protected virtual void DebugDeployableInfo()
    {
        Debug.Log($"©ö??¢® ¢¯?¨ù? ??¢¬?, deployableInfo : {DeployableInfo}");
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

    // ???? ?¡×?¢®?? ?¢¬?? ¨ù©ø?¢´
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
