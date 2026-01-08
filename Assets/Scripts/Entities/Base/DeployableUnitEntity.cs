#nullable enable
using UnityEngine;
using System;

public enum DeployableDespawnReason
{
    Null, // 디폴트
    Defeated, // 처치됨
    Retreat // 퇴각
}

public abstract class DeployableUnitEntity : UnitEntity
{
    public DeployableInfo DeployableInfo { get; protected set; } = default!;

    protected DeploymentController _deployment;
    public IReadableDeploymentController Deployment => _deployment;

    [SerializeField] protected DeployableUnitData _deployableData;
    public DeployableUnitData DeployableData => _deployableData;

    public bool IsDeployed => Deployment.IsDeployed;
    public bool IsPreviewMode => Deployment.IsPreviewMode;
    public int DeploymentOrder => Deployment.DeploymentOrder; 
    public Tile? CurrentTile => Deployment.CurrentTile;

    public int InitialDeploymentCost { get => (int)Stat.GetStat(StatType.DeploymentCost); }
    public Vector3? FacingDirection { get; protected set; } // 컨테이너에서 관리

    public static event Action<DeployableUnitEntity> OnDeployed = delegate { };
    public static event Action<DeployableUnitEntity> OnUndeployed = delegate { };
    public static event Action<DeployableUnitEntity> OnDeployableSelected = delegate { };
    public static event Action<DeployableUnitEntity> OnRetreat = delegate { };

    protected override void Awake()
    {
        base.Awake();

        Faction = Faction.Ally;
        _deployment = new DeploymentController(this);
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
    }

    public virtual void Deploy(Vector3 position, Vector3? facingDirection = null)
    {
        if (!IsDeployed)
        {
            // 배치 시도 및 성공
            if (_deployment.Deploy(position, facingDirection))
            {
                _collider.SetColliderState(true);
                DeployAdditionalProcess(); // 자식 클래스에서 구현할 추가 로직들
                OnDeployed?.Invoke(this);
            }
        }
    }

    protected virtual void DeployAdditionalProcess() { }

    protected virtual void Undeploy()
    {
        if (_deployment.Undeploy()) // IsDeployed = false & 점유한 타일 비우기
        {
            DeployableInfo.deployedDeployable = null;

            UndeployAdditionalProcess();

            OnUndeployed?.Invoke(this);
        }
    }

    protected virtual void UndeployAdditionalProcess() { }


    // 체력이 0 이하가 된 상황일 때 실행됨
    protected override void HandleOnDeath()
    {
        Despawn(DeployableDespawnReason.Defeated);
    }

    public void Despawn(DeployableDespawnReason reason)
    {
        if (reason == DeployableDespawnReason.Null) return;
        if (!IsDeployed) return; // 현재 배치되지 않았으면 Despawn되지 않음

        Logger.Log($"{gameObject.name} Despawn 동작");

        Undeploy();
        HandleBeforeDisabled(); 

        if (reason == DeployableDespawnReason.Retreat)
        {
            // Operator 퇴각 시에 이 이벤트를 구독하는 메서드가 동작하지 않는 이슈가 있음
            OnRetreat?.Invoke(this); 
            Logger.Log($"{gameObject.name} OnRetreat 이벤트 동작");
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

    public void SetDeploymentOrder(int order)
    {
        _deployment.SetDeploymentOrder(order);
    }

    public void SetDirection(Vector3 direction)
    {
        FacingDirection = direction;
    }


    public virtual void OnClick()
    {
        _deployment.OnClick();
    }

    // _deployment.OnClick()에 의해 클릭 동작이 가능한 경우 이벤트를 발생시킴
    public void NotifySelected()
    {
        OnDeployableSelected?.Invoke(this);
    }
}

#nullable restore
