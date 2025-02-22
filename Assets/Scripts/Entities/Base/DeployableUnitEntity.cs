#nullable enable
using UnityEngine;

public abstract class DeployableUnitEntity: UnitEntity, IDeployable
{
    public new DeployableUnitData BaseData { get; private set; }

    [HideInInspector]
    public DeployableUnitStats currentStats;

    // IDeployable �������̽� ����
    public bool IsDeployed { get; protected set; }
    public int InitialDeploymentCost { get; protected set; } // ���� ��ġ �ڽ�Ʈ - DeploymentCost�� ���� �� ������ �� ����

    public DeployableManager.DeployableInfo? DeployableInfo { get; protected set; }

    public Sprite? Icon => BaseData.Icon;

    // �̸����� ����
    protected bool isPreviewMode = false;
    public bool IsPreviewMode
    {
        get { return isPreviewMode; }
        protected set
        {
            isPreviewMode = value;
        }
    }
    protected Material originalMaterial;
    protected Material previewMaterial;

    public virtual bool CanDeployGround { get; set; }
    public virtual bool CanDeployHill { get; set; }

    // ��ġ �Ϸ� �� Ŀ���� �� ��ġ�� ���۷����� ��ġ�� �� ActionUI�� ��Ÿ���� �����ϱ� ���� ������
    private float preventInteractingTime = 0.1f; 
    private float lastDeployTime;
    public Tile CurrentTile { get; protected set; }

    protected override void Awake()
    {
        Faction = Faction.Ally; // ��ġ ������ ��Ҵ� ��� �Ʊ����� ����
        base.Awake();
    }

    public void Initialize(DeployableManager.DeployableInfo deployableInfo)
    {
        DeployableInfo = deployableInfo;
        if (deployableInfo.deployableUnitData != null)
        {
            BaseData = deployableInfo.deployableUnitData;
        }
        else
        {
            Debug.LogError("BaseData�� �Ҵ�� ���� ����!");
            return;
        }

        currentStats = BaseData.stats;
        Prefab = BaseData.prefab;

        InitializeDeployableProperties();

        UpdateCurrentTile();
    }



    // �ڽ� ������Ʈ�� �ð�ȭ�� ����ϴ� Model�� �ִٴ� ����
    protected virtual void InitializeDeployableProperties()
    {
        SetDeployState(false);

        // A ?? B : A�� null�� ��� B�� ���
        CanDeployGround = BaseData?.canDeployOnGround ?? false; 
        CanDeployHill = BaseData?.canDeployOnHill ?? false;

        InitialDeploymentCost = currentStats.DeploymentCost; // �ʱ� ��ġ �ڽ�Ʈ ����
    }

    public virtual void Deploy(Vector3 position)
    {
        if (!IsDeployed)
        {
            SetDeployState(true);
            UpdateCurrentTile();
            CurrentTile.SetOccupied(this);
            SetPosition(position);
            InitializeHP();
            lastDeployTime = Time.time;
        }
    }

    // Ÿ�� �������� ���� ��ġ ��ġ ����
    protected void SetPosition(Vector3 worldPosition)
    {
        if (CurrentTile != null)
        {
            // ���۷����ʹ� ��¦ ����� ��ġ
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
            DeployableManager.Instance.OnDeployableRemoved(this);
            if (CurrentTile != null)
            {
                CurrentTile.ClearOccupied(); // Ÿ�Ͽ� ��ġ�� ��� ����
            }

            base.Die();
        }
    }

    public virtual bool CanDeployOnTile(Tile tile)
    {
        if (IsInvalidTile(tile)) return false;

        if (tile.data.terrain == TileData.TerrainType.Ground && BaseData.canDeployOnGround) return true;
        if (tile.data.terrain == TileData.TerrainType.Hill && BaseData.canDeployOnHill) return true;

        return false;
    }

    private bool IsInvalidTile(Tile tile)
    {
        return tile == null || tile.IsOccupied || tile.data.isStartPoint || tile.data.isEndPoint;
    }

    public void UpdatePreviewPosition(Vector3 position)
    {
        if (IsPreviewMode)
        {
            transform.position = position;
        }
    }

    // ��ġ�� ���� Ŭ�� �� ����
    public virtual void OnClick()
    {
        // ��ġ ���� Ŭ�� ����
        if (Time.time - lastDeployTime < preventInteractingTime)
        {
            DeployableManager.Instance.CancelPlacement();
            return;
        }

        // ��ġ�� ���۷����� Ŭ�� ����
        if (IsDeployed && !IsPreviewMode && StageManager.Instance.currentState == GameState.Battle)
        {
            DeployableManager.Instance.CancelPlacement();

            // �̸����� ���¿��� ���� X
            if (IsPreviewMode == false)
            {
                DebugDeployableInfo();
                UIManager.Instance.ShowDeployedInfo(this);
            }

            ShowActionUI();
        }
    }

    protected virtual void DebugDeployableInfo()
    {
        Debug.Log($"��ġ ��� Ŭ��, deployableInfo : {DeployableInfo}");
    }

    protected virtual void ShowActionUI()
    {
        DeployableManager.Instance.ShowActionUI(this);
        UIManager.Instance.ShowDeployedInfo(this);
    }

    protected override void InitializeHP()
    {
        MaxHealth = currentStats.Health;
        CurrentHealth = MaxHealth;
    }

    protected void SetDeployState(bool isDeployed)
    {
        IsDeployed = isDeployed;
        IsPreviewMode = !isDeployed;
    }

    // ���� ��ġ�� Ÿ�� ����
    protected virtual void UpdateCurrentTile()
    {
        Vector3 position = transform.position;
        Tile newTile = MapManager.Instance.GetTileAtWorldPosition(position);

        if (newTile != CurrentTile)
        {
            CurrentTile = newTile;
        }
    }

    //protected virtual void SetDeployedEntity()
    //{
    //    DeployableInfo.deployedDeployable = this;
    //}

    //protected virtual void DeleteDeployedEntity()
    //{
    //    DeployableInfo.deployedDeployable = null;
    //}

}

#nullable restore
