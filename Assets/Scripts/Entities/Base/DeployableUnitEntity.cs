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
    public int InitialDeploymentCost { get; protected set; } // ���� ��ġ �ڽ�Ʈ - DeploymentCost�� ���� �� ������ �� ����

    // [SerializeField] private Collider _mainCollider;

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
    protected Material originalMaterial = default!;
    protected Material previewMaterial = default!;

    // ��ġ �Ϸ� �� Ŀ���� �� ��ġ�� ���۷����� ��ġ�� �� ActionUI�� ��Ÿ���� �����ϱ� ���� ������
    private float preventInteractingTime = 0.1f;
    private float lastDeployTime;

    public Tile? CurrentTile { get; protected set; } // "��ġ ��"�̶�� ������ �ֱ� ������ nullable

    public static event Action<DeployableUnitEntity> OnDeployed = delegate { };

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
            DeployableUnitData = deployableInfo.deployableUnitData!;

            currentDeployableStats = DeployableUnitData.stats;
            Prefab = DeployableUnitData.prefab;

            InitializeDeployableProperties();
            UpdateCurrentTile();
        }
        else
        {
            Debug.LogError("BaseData�� �Ҵ�� ���� ����!");
            return;
        }
    }



    // �ڽ� ������Ʈ�� �ð�ȭ�� ����ϴ� Model�� �ִٴ� ����
    protected virtual void InitializeDeployableProperties()
    {
        SetDeployState(false);
        InitialDeploymentCost = currentDeployableStats.DeploymentCost; // �ʱ� ��ġ �ڽ�Ʈ ����
    }

    public virtual void Deploy(Vector3 position)
    {
        if (!IsDeployed)
        {
            SetDeployState(true);
            SetColliderState(true); // �ݶ��̴� ��
            UpdateCurrentTile();
            if (CurrentTile != null)
            {
                CurrentTile.SetOccupied(this);
            }
            SetPosition(position);
            InitializeHP();
            lastDeployTime = Time.time;

            OnDeployed?.Invoke(this);
            Debug.Log("OnDeployed �̺�Ʈ �߻�");
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
            DeployableManager.Instance!.OnDeployableRemoved(this);
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

    // ��ġ�� ���� Ŭ�� �� ����
    public virtual void OnClick()
    {
        // ��ġ ���� Ŭ�� ����
        if (Time.time - lastDeployTime < preventInteractingTime)
        {
            DeployableManager.Instance!.CancelPlacement();
            return;
        }

        // ��ġ�� ���۷����� Ŭ�� ����
        if (IsDeployed &&
            !IsPreviewMode &&
            StageManager.Instance!.currentState == GameState.Battle // �׽�Ʈ ��)
            )
        {
            DeployableManager.Instance!.CancelPlacement();

            // �̸����� ���¿��� ���� X
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
        Debug.Log($"��ġ ��� Ŭ��, deployableInfo : {DeployableInfo}");
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

    // ���� ��ġ�� Ÿ�� ����
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
