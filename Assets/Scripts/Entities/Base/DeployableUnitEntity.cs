#nullable enable
using UnityEngine;

public abstract class DeployableUnitEntity: UnitEntity, IDeployable
{
    public new DeployableUnitData BaseData { get; private set; }

    [HideInInspector]
    public DeployableUnitStats currentStats;

    // IDeployable 인터페이스 관련
    public bool IsDeployed { get; protected set; }
    public int InitialDeploymentCost { get; protected set; } // 최초 배치 코스트 - DeploymentCost는 게임 중 증가할 수 있음

    private DeployableManager.DeployableInfo? deployableInfo;

    public Sprite? Icon => BaseData.Icon;

    // 미리보기 관련
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

    // 배치 완료 후 커서를 뗀 위치가 오퍼레이터 위치일 때 ActionUI가 나타남을 방지하기 위한 변수들
    private float preventInteractingTime = 0.1f; 
    private float lastDeployTime;

    protected override void Awake()
    {
        Faction = Faction.Ally; // 배치 가능한 요소는 모두 아군으로 간주
        base.Awake();
    }

    public void Initialize(DeployableUnitData deployableUnitData)
    {
        InitializeDeployableData(deployableUnitData);

        base.UpdateCurrentTile();
        Prefab = BaseData.prefab;

        InitializeDeployableProperties(); 
    }

    private void InitializeDeployableData(DeployableUnitData deployableData)
    {
        BaseData = deployableData;
        currentStats = BaseData.stats;
        deployableInfo = DeployableManager.Instance.GetDeployableInfoByName(BaseData.entityName);
    }


    // 자식 오브젝트에 시각화를 담당하는 Model이 있다는 전제
    protected virtual void InitializeDeployableProperties()
    {
        IsDeployed = false; // 배치 비활성화
        IsPreviewMode = true; // 미리보기 활성화

        // A ?? B : A가 null일 경우 B를 사용
        CanDeployGround = BaseData?.canDeployOnGround ?? false; 
        CanDeployHill = BaseData?.canDeployOnHill ?? false;

        InitialDeploymentCost = currentStats.DeploymentCost; // 초기 배치 코스트 설정
    }

    public virtual void Deploy(Vector3 position)
    {
        if (!IsDeployed)
        {
            IsDeployed = true;
            IsPreviewMode = false;
            base.UpdateCurrentTile();
            CurrentTile.SetOccupied(this);
            SetPosition(position);
            InitializeHP();
            lastDeployTime = Time.time;
        }
    }

    // 타일 위에서의 실제 배치 위치 조정
    protected void SetPosition(Vector3 worldPosition)
    {
        if (CurrentTile != null)
        {
            // 오퍼레이터는 살짝 띄워서 배치
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
            DeployableManager.Instance.OnDeployableRemoved(this);
            if (CurrentTile != null)
            {
                CurrentTile.ClearOccupied(); // 타일에 배치된 요소 제거
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

    // 배치된 유닛 클릭 시 동작
    public virtual void OnClick()
    {
        // 배치 직후 클릭 방지
        if (Time.time - lastDeployTime < preventInteractingTime)
        {
            DeployableManager.Instance.CancelPlacement();
            return;
        }

        // 배치된 오퍼레이터 클릭 동작
        if (IsDeployed && !IsPreviewMode && StageManager.Instance.currentState == GameState.Battle)
        {
            DeployableManager.Instance.CancelPlacement();

            // 미리보기 상태에선 동작 X
            if (IsPreviewMode == false)
            {
                UIManager.Instance.ShowDeployedInfo(this);
            }

            ShowActionUI();
        }
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
}

#nullable restore
