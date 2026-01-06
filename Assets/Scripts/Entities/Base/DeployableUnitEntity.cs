#nullable enable
using UnityEngine;
using System;

public enum DeployableDespawnReason
{
    Null, // 디폴트
    Defeated, // 처치됨
    Retreat // 퇴각
}

public abstract class DeployableUnitEntity : UnitEntity, IDeployable
{
    public DeployableInfo DeployableInfo { get; protected set; } = default!;

    protected DeploymentController _deployment;
    public IReadableDeploymentController Deployment => _deployment;

    [SerializeField] protected DeployableUnitData _deployableData;
    public DeployableUnitData DeployableData => _deployableData;

    public bool IsDeployed => Deployment.IsDeployed;
    public bool IsPreviewMode => Deployment.IsPreviewMode;
    public Tile? CurrentTile => Deployment.CurrentTile;

    // [HideInInspector]
    // public DeployableUnitStats currentDeployableStats;

    // public bool IsDeployed { get; protected set; }
    public int InitialDeploymentCost { get => (int)Stat.GetStat(StatType.DeploymentCost); }

    // protected bool isPreviewMode = false;
    // public bool IsPreviewMode
    // {
    //     get { return isPreviewMode; }
    //     protected set
    //     {
    //         isPreviewMode = value;
    //     }
    // }

    // protected Material originalMaterial = default!;
    // protected Material previewMaterial = default!;

    private float preventInteractingTime = 0.1f;
    private float lastDeployTime;

    // public Tile? CurrentTile { get; protected set; }

    public static event Action<DeployableUnitEntity> OnDeployed = delegate { };
    public static event Action<DeployableUnitEntity> OnUndeployed = delegate { };
    public event Action<DeployableUnitEntity> OnRetreat = delegate { };

    protected override void Awake()
    {
        Faction = Faction.Ally;

        _deployment = new DeploymentController(this);

        base.Awake();
    }

    public virtual void Initialize(DeployableInfo deployableInfo)
    {
        DeployableInfo = deployableInfo;
        if (_deployableData == null)
        {
            if (DeployableInfo.deployableUnitData == null)
            {
                Logger.Log("_deployableData가 null임");
                return;
            }
            else
            {
                _deployableData = DeployableInfo.deployableUnitData;
            }
        }

        base.Initialize();
    }

    // base.Initialize 템플릿 메서드 1
    protected override void ApplyUnitData()
    {
        // 데이터를 이용해 스탯 초기화
        _stat.Initialize(_deployableData);
        _health.Initialize();

        _deployment.Initialize(_deployableData);
        _deployment.OnDeployed += HandleDeploymentInternal;
    }

    // InitializeVisual 관련 템플릿 메서드
    protected override void SpecificVisualLogic()
    {
        _visual.AssignColorToRenderers(DeployableData.PrimaryColor, DeployableData.SecondaryColor);
    }

    // base.Initialize 템플릿 메서드 3
    protected override void OnInitialized()
    {
        PoolTag = _deployableData.UnitTag;

        // 배치 상태 관련
        // SetDeployState(false); // Initialize에서는 배치되지 않은 상태로 초기화됨
        // InitialDeploymentCost = currentDeployableStats.DeploymentCost;
        // UpdateCurrentTile();
    }

    // DeploymentController.OnDeployed 이벤트를 받아서 처리함
    // 여기의 OnDeployed가 static이므로 이렇게 구현해두면 외부 코드를 수정할 필요 없음
    private void HandleDeploymentInternal(DeployableUnitEntity deployable)
    {
        if (deployable == this)
        {
            OnDeployed?.Invoke(this);
        }
    }



    public virtual void Deploy(Vector3 position)
    {
        if (!IsDeployed)
        {
            _deployment.Deploy(position);
            _collider.SetColliderState(true);
        }
    }

   
    protected void Undeploy()
    {
        _deployment.Undeploy();
        DeployableInfo.deployedDeployable = null;
        OnUndeployed?.Invoke(this);
    } 

    // protected void SetPosition(Vector3 worldPosition)
    // {
    //     if (CurrentTile != null)
    //     {
    //         if (this is Operator)
    //         {
    //             transform.position = worldPosition + Vector3.up * (CurrentTile.GetHeightScale() / 2 + 0.5f);
    //         }
    //         else
    //         {
    //             transform.position = worldPosition + Vector3.up * (CurrentTile.GetHeightScale() / 2);
    //         }
    //     }
    // }


    // 체력이 0 이하가 된 상황일 때 실행됨
    protected override void HandleOnDeath()
    {
        Despawn(DeployableDespawnReason.Defeated);
    }

    public void Despawn(DeployableDespawnReason reason)
    {
        if (reason == DeployableDespawnReason.Null) return;
        if (!IsDeployed) return; // 현재 배치되지 않았으면 Despawn되지 않음

        HandleBeforeDisabled(); 
        Undeploy();

        if (reason == DeployableDespawnReason.Retreat)
        {
            OnRetreat?.Invoke(this);
            DieInstantly();
        }
        else if (reason == DeployableDespawnReason.Defeated)
        {
            DieWithAnimation();
        }
    }

    protected virtual void HandleBeforeDisabled() { }


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

    // protected void SetDeployState(bool isDeployed)
    // {
    //     IsDeployed = isDeployed;
    //     IsPreviewMode = !isDeployed;
    // }

    // protected virtual void UpdateCurrentTile()
    // {
    //     Vector3 position = transform.position;
    //     Tile? newTile = MapManager.Instance!.GetTileAtWorldPosition(position);

    //     if (newTile != null && newTile != CurrentTile)
    //     {
    //         CurrentTile = newTile;
    //     }
    // }
}

#nullable restore
