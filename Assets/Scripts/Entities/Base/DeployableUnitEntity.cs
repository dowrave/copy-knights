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
    public int InitialDeploymentCost { get; protected set; } // 최초 배치 코스트 - DeploymentCost는 게임 중 증가할 수 있음

    // [SerializeField] private Collider _mainCollider;

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
    protected Material originalMaterial = default!;
    protected Material previewMaterial = default!;

    // 배치 완료 후 커서를 뗀 위치가 오퍼레이터 위치일 때 ActionUI가 나타남을 방지하기 위한 변수들
    private float preventInteractingTime = 0.1f;
    private float lastDeployTime;

    public Tile? CurrentTile { get; protected set; } // "배치 중"이라는 과정이 있기 떄문에 nullable

    public static event Action<DeployableUnitEntity> OnDeployed = delegate { };

    protected override void Awake()
    {
        Faction = Faction.Ally; // 배치 가능한 요소는 모두 아군으로 간주
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
            Debug.LogError("BaseData에 할당된 값이 없음!");
            return;
        }
    }



    // 자식 오브젝트에 시각화를 담당하는 Model이 있다는 전제
    protected virtual void InitializeDeployableProperties()
    {
        SetDeployState(false);
        InitialDeploymentCost = currentDeployableStats.DeploymentCost; // 초기 배치 코스트 설정
    }

    public virtual void Deploy(Vector3 position)
    {
        if (!IsDeployed)
        {
            SetDeployState(true);
            SetColliderState(true); // 콜라이더 켬
            UpdateCurrentTile();
            if (CurrentTile != null)
            {
                CurrentTile.SetOccupied(this);
            }
            SetPosition(position);
            InitializeHP();
            lastDeployTime = Time.time;

            OnDeployed?.Invoke(this);
            Debug.Log("OnDeployed 이벤트 발생");
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
            DeployableInfo.deployedDeployable = null;
            DeployableManager.Instance!.OnDeployableRemoved(this);
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

    // 배치된 유닛 클릭 시 동작
    public virtual void OnClick()
    {
        // 배치 직후 클릭 방지
        if (Time.time - lastDeployTime < preventInteractingTime)
        {
            DeployableManager.Instance!.CancelPlacement();
            return;
        }

        // 배치된 오퍼레이터 클릭 동작
        if (IsDeployed &&
            !IsPreviewMode &&
            StageManager.Instance!.currentState == GameState.Battle // 테스트 중)
            )
        {
            DeployableManager.Instance!.CancelPlacement();

            // 미리보기 상태에선 동작 X
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
        Debug.Log($"배치 요소 클릭, deployableInfo : {DeployableInfo}");
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

    // 현재 위치한 타일 설정
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
