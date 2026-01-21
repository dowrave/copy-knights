using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public enum EnemyDespawnReason
{
    Null, // 디폴트
    Defeated, // 처치됨
    ReachedGoal // 목적지 도달
}

public class Enemy : UnitEntity, IMovable, ICombatEntity
{
    [SerializeField] protected EnemyData _enemyData = default!;
    public virtual EnemyData BaseData => _enemyData;

    protected EnemyStats currentStats;

    protected EnemyAttackController _attack;
    public EnemyAttackController Attack => _attack;

    // 수정 필요할지도?
    public new AttackType AttackType;
    public AttackRangeType AttackRangeType => BaseData.AttackRangeType;
    public float ActionCooldown { get; set; } // 다음 공격까지의 대기 시간
    public float ActionDuration { get; set; } // 공격 모션 시간. Animator가 추가될 때 수정 필요할 듯. 항상 Cooldown보다 짧아야 함.

    public override float AttackPower { get => Stat.GetStat(StatType.AttackPower);}
    public override float AttackSpeed { get => Stat.GetStat(StatType.AttackSpeed);}
    public float MovementSpeed { get => Stat.GetStat(StatType.MovementSpeed); }
    public int BlockSize { get => (int)Stat.GetStat(StatType.BlockSize); } // Enemy가 차지하는 저지 수, 저지 수 자체가 변하는 로직은 없으니 게터만 구현
    public float AttackRange { get => BaseData.AttackRangeType == AttackRangeType.Melee ? 0f : Stat.GetStat(StatType.AttackRange); }

    public float AttackCooldown => _attack.AttackCooldown;
    public float AttackDuration => _attack.AttackDuration;
    public UnitEntity CurrentTarget => _attack.CurrentTarget;
    public IReadOnlyList<UnitEntity> TargetsInRange => _attack.TargetsInRange;
    public Operator BlockingOperator => _attack.BlockingOperator;

    // 경로 관련
    protected PathNavigator navigator;
    protected Barricade? targetBarricade;
    protected List<PathNode> currentPathNodes = new List<PathNode>();
    protected List<Vector3> currentPathPositions = new List<Vector3>();
    protected Vector3 currentDestination; // 현재 향하는 위치
    protected int _currentPathIndex;
    protected bool isWaiting = false; // 단순히 위치에서 기다리는 상태
    // protected bool stopAttacking = false; // 인위적으로 넣은 공격 가능 / 불가능 상태
    protected PathData _pathData;

    public PathNavigator Navigator => navigator;
    public IReadOnlyList<Vector3> CurrentPathPositions => currentPathPositions;
    public IReadOnlyList<PathNode> CurrentPathNodes => currentPathNodes;
    public int CurrentPathIndex
    {
        get => _currentPathIndex;
        protected set
        {
            _currentPathIndex = value;
            if (navigator != null)
            {
                navigator.SetCurrentPathIndex(_currentPathIndex);
            }
            else
            {
                Logger.LogWarning($"navigator가 null이라 navigator의 _currentPathIndex가 업데이트되지 않음");
            }
        }
    }

    protected int initialPoolSize = 5;

    [SerializeField] protected GameObject enemyBarUIPrefab = default!;
    protected EnemyBarUI? enemyBarUI;

    // 접촉 중인 타일 관리
    protected List<Tile> contactedTiles = new List<Tile>();

    // 메쉬의 회전 관련해서 모델 관리
    [Header("Model Components")]
    [SerializeField] protected GameObject modelContainer = default!;

    [Header("Attack Range Controller")]
    [SerializeField] protected EnemyAttackRangeController attackRangeController = default!;

    // ICrowdControlTarget
    public Vector3 Position => transform.position;

    protected EnemyDespawnReason currentEnemyDespawnReason = EnemyDespawnReason.Null;
    protected bool isInitialized = false;

    // 스태틱 이벤트 테스트
    // public static event Action<Enemy> OnEnemyDestroyed; // 죽는 상황 + 목적지에 도달해서 사라지는 상황 모두 포함
    public static event Action<Enemy, EnemyDespawnReason> OnEnemyDespawned = delegate { };

    protected override void Awake()
    {
        Faction = Faction.Enemy;

        InitializeModelComponents();

        // 공격 범위 컨트롤러 추가
        if (attackRangeController == null)
        {
            attackRangeController = GetComponentInChildren<EnemyAttackRangeController>();
        }

        base.Awake();

        SetColliderState(true); // base.Awake에서 false로 지정되므로 바꿔줌

        // 세부 컨트롤러 생성자
        _attack = new EnemyAttackController(this);

        // OnDeathAnimationCompleted += HandleDeathAnimationCompleted;
    }

    protected virtual void Start()
    {
        _visual.AssignColorToRenderers(_enemyData.PrimaryColor, _enemyData.SecondaryColor);
    }

    // 모델 회전 관련 로직을 쓸 일이 Enemy 뿐이라 여기에 구현해놓음.
    protected void InitializeModelComponents()
    {
        if (modelContainer == null)
        {
            modelContainer = transform.Find("ModelContainer").gameObject;
        }
    }

    protected override void SetPoolTag()
    {
        PoolTag = _enemyData.UnitTag;
    }

    // Enemy를 위한 Initialize Wrapper
    public virtual void Initialize(EnemyData enemyData, PathData pathData)
    {
        if (_enemyData == null)
        {
            _enemyData = enemyData;
        }

        if (pathData == null) Logger.LogError("pathData가 전달되지 않음");

        _pathData = pathData; 

        // UnitEntity.Initialize
        base.Initialize();
    }

    // base.Initialize에서 실행되는 템플릿 메서드 1
    protected override void ApplyUnitData()
    {
        // 스탯 시스템 초기화
        _stat.Initialize(_enemyData); 
        _health.Initialize();

        // 공격 범위 콜라이더 설정
        attackRangeController.Initialize(this);
    }

    // InitializeVisual 관련 템플릿 메서드
    protected override void SpecificVisualLogic()
    {
        _visual.AssignColorToRenderers(_enemyData.PrimaryColor, _enemyData.SecondaryColor);
    }

    // base.Initialize 템플릿 메서드 3
    protected override void OnInitialized()
    {
        CreateEnemyBarUI();
        
        // navigator 초기화 및 경로 설정
        navigator = new PathNavigator(this, _pathData.Nodes);
        navigator.OnPathUpdated += HandlePathUpdated;
        navigator.Initialize(); // HandlePathUpdated에 의해 currentPath도 설정됨
        SetupInitialPosition();

        // 초기화는 데이터에 있는 타입을 가져옴
        AttackType = BaseData.AttackType; 

        // 스킬 설정 
        // SetSkills();

        // 재사용할 일이 없어보이긴 하지만 일단 초기화에서도 구현 
        currentEnemyDespawnReason = EnemyDespawnReason.Null;

        isInitialized = true;
    }


    protected void OnEnable()
    {
        DeployableUnitEntity.OnUndeployed += HandleDeployableDied;
    }


    // 새로운 경로 설정 시 설정됨
    protected void HandlePathUpdated(IReadOnlyList<PathNode> newPathNodes, IReadOnlyList<Vector3> newPathPositions)
    {
        // new List<>()는 리스트가 메모리에 계속 할당되어 GC 부하가 발생하므로 자주 실행되는 메서드는 이 방식이 더 좋다
        currentPathNodes.Clear();
        currentPathNodes.AddRange(newPathNodes);

        currentPathPositions.Clear();
        currentPathPositions.AddRange(newPathPositions);

        // 인덱스 할당
        // CurrentPathIndex = 0; 
        CurrentPathIndex = currentPathNodes.Count > 1 ? 1 : 0; // [테스트] 뒤로 가는 현상을 방지하기 위해 1로 놔 봄
        currentDestination = currentPathPositions[CurrentPathIndex];
    }

    protected void SetupInitialPosition()
    {
        if (currentPathPositions != null && currentPathPositions.Count > 0)
        {
            transform.position = currentPathPositions[0];
        }
    }

    protected override void Update()
    {
        // update 동작 조건 : 전투 중 & 디스폰되지 않음 && 초기화됨 
        if (!IsUpdateValid()) return; 


        base.Update(); // 버프 효과 갱신

        // 행동 제약에 관계 없이 업데이트되어야 하는 요소들 처리
        // 예시) 스킬 쿨다운, 공격 쿨다운
        UpdateAllCooldowns();

        // 공격 시도
        OnUpdateAction();
    }

    protected bool IsUpdateValid()
    {
        return StageManager.Instance!.CurrentGameState == GameState.Battle && 
            currentEnemyDespawnReason == EnemyDespawnReason.Null && 
            isInitialized;
    }

    protected virtual void UpdateAllCooldowns()
    {
        _attack.UpdateAllCooldowns();
    }

    protected virtual void OnUpdateAction()
    {
        // 공격을 시도함
        bool attacked = _attack.OnUpdate();
        if (attacked) return;

        // 공격을 하지 않았다면 이동
        MoveAlongPath();
    }

    // 경로를 따라 이동
    protected void MoveAlongPath()
    {
        if (currentDestination == null) throw new InvalidOperationException("다음 노드가 설정되어있지 않음");
        if (navigator == null || navigator.FinalDestination == null) throw new InvalidOperationException("navigator나 최종 목적지가 설정되지 않음");

        if (CheckIfReachedDestination())
        {
            Despawn(EnemyDespawnReason.ReachedGoal);
            return;
        }

        Move(currentDestination);
        RotateModelTowardsMovementDirection();

        // 노드 도달 확인
        if (Vector3.Distance(transform.position, currentDestination) < 0.05f)
        {
            // 목적지 도달
            if (Vector3.Distance(transform.position, navigator.FinalDestination) < 0.05f)
            {
                Despawn(EnemyDespawnReason.ReachedGoal);
            }
            // 기다려야 하는 경우
            else if (currentPathNodes[CurrentPathIndex].waitTime > 0)
            {
                StartCoroutine(WaitAtNode(currentPathNodes[CurrentPathIndex].waitTime));
            }
            // 노드 업데이트
            else
            {
                UpdateNextNode();
            }
        }
    }

    public void Move(Vector3 destination)
    {
        transform.position = Vector3.MoveTowards(transform.position, destination, MovementSpeed * Time.deltaTime);
    }

    // 대기 중일 때 실행
    protected IEnumerator WaitAtNode(float waitTime)
    {
        SetIsWaiting(true);
        yield return new WaitForSeconds(waitTime);
        SetIsWaiting(false);

        UpdateNextNode();
    }

    // 노드를 갱신해야 하는 상황에 호출
    // 다음 노드 인덱스를 설정하고 현재 목적지로 지정함
    // 스킬에서 접근할 수 있게 public으로 변경
    public void UpdateNextNode()
    {
        // pathData 관련 데이터 항목이 없거나, 도달할 노드가 마지막 노드인 경우는 실행되지 않음
        if (currentPathPositions == null || CurrentPathIndex >= currentPathPositions.Count - 1)
        {
            Logger.LogError("오류 발생");
            return;
        }

        if (navigator == null)
        {
            Logger.LogError("navigator가 없음");
            return;
        }

        CurrentPathIndex++;

        if (CurrentPathIndex < currentPathPositions.Count)
        {
            currentDestination = currentPathPositions[CurrentPathIndex];
        }
    }

    // Enemy가 공격 대상으로 삼은 적이 죽었을 때 동작
    public void HandleDeployableDied(DeployableUnitEntity disabledEntity) => _attack.HandleDeployableDied(disabledEntity); 
    public void OnTargetEnteredAttackRange(DeployableUnitEntity target) => _attack.OnTargetEnteredAttackRange(target);
    public void OnTargetExitedAttackRange(DeployableUnitEntity target) => _attack.OnTargetExitedAttackRange(target);
    public void SetStopAttacking(bool isAttacking) => _attack.SetStopAttacking(isAttacking);
    public void SetCurrentBarricade(Barricade barricade) => _attack.SetCurrentBarricade(barricade);
    
    protected override void HandleOnDeath() => Despawn(EnemyDespawnReason.Defeated);

    // 사라지는 로직 관리
    protected void Despawn(EnemyDespawnReason reason)
    {        
        // 예외 처리
        if (currentEnemyDespawnReason != EnemyDespawnReason.Null) return;

        currentEnemyDespawnReason = reason;

        // UI 제거
        if (enemyBarUI != null)
        {
            enemyBarUI.gameObject.SetActive(false);
        }

        // 디스폰 이유 파라미터를 포함하는 이벤트 발생
        OnEnemyDespawned?.Invoke(this, currentEnemyDespawnReason);

        // 사망 처리
        DieWithAnimation(); 
    }

    protected override void OnDamageTaken(UnitEntity attacker, float actualDamage)
    {
        // 공격자가 Operator일 때 통계 패널 업데이트
        if (attacker is Operator op)
        {
            OperatorData opData = op.OperatorData;
            if (opData != null)
            {
                StatisticsManager.Instance!.UpdateDamageDealt(op.OperatorData, actualDamage);
            }
        }
    }
    // 마지막 타일의 월드 좌표 기준
    protected bool CheckIfReachedDestination()
    {
        if (currentPathPositions == null) throw new InvalidOperationException("currentPathPositions가 할당되지 않음");

        if (currentPathPositions.Count == 0) return false;

        Vector3 lastPathPosition = currentPathPositions[currentPathPositions.Count - 1];

        return Vector3.Distance(transform.position, lastPathPosition) < 0.05f;
    }



    // 현재 경로상에서 목적지까지 남은 거리 계산
    public float GetRemainingPathDistance()
    {
        if (currentPathPositions.Count == 0 || CurrentPathIndex > currentPathPositions.Count - 1)
        {
            return float.MaxValue;
        }

        float distance = 0f;
        for (int i = CurrentPathIndex; i < currentPathPositions.Count - 1; i++)
        {
            // 첫 타일에 한해서만 현재 위치를 기반으로 계산(여러 Enemy가 같은 타일에 있을 수 있기 때문)
            if (i == CurrentPathIndex)
            {
                Vector3 nowPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                distance += Vector3.Distance(nowPosition, currentPathPositions[i + 1]);
            }

            distance += Vector3.Distance(currentPathPositions[i], currentPathPositions[i + 1]);
        }

        return distance;
    }

    protected void PlayMeleeAttackEffect(UnitEntity target, AttackSource attackSource)
    {
        // 이펙트 처리
        if (meleeAttackEffectTag != null && BaseData.MeleeAttackEffectPrefab != null)
        {

            GameObject? effectObj = ObjectPoolManager.Instance!.SpawnFromPool(
                   meleeAttackEffectTag,
                   transform.position,
                   Quaternion.identity
            );

            if (effectObj != null)
            {
                CombatVFXController? combatVFXController = effectObj.GetComponent<CombatVFXController>();
                if (combatVFXController != null)
                {
                    combatVFXController.Initialize(attackSource, target, meleeAttackEffectTag);
                }
            }
        }
    }
    
    protected void CreateEnemyBarUI()
    {
        if (enemyBarUIPrefab != null)
        {
            GameObject enemyBarInstance = Instantiate(enemyBarUIPrefab, transform);
            enemyBarUI = enemyBarInstance.GetComponentInChildren<EnemyBarUI>();
            if (enemyBarUI != null)
            {
                enemyBarUI.Initialize(this);
            }
        }
    }

    public void UpdateBlockingOperator(Operator op) => _attack.UpdateBlockingOperator(op);

    public override void OnBodyTriggerEnter(Collider other)
    {
        Tile tile = other.GetComponent<Tile>();
        if (tile != null)
        {
            contactedTiles.Add(tile);
            tile.EnemyEntered(this);
        }
    }

    public override void OnBodyTriggerExit(Collider other)
    {
        Tile tile = other.GetComponent<Tile>();
        if (tile != null && contactedTiles.Contains(tile))
        {
            tile.EnemyExited(this);
            contactedTiles.Remove(tile);
        }
    }

    // 모델을 이동 방향으로 회전시킴
    // 참고) 프리팹 기준 +z 방향으로 이동한다고 가정했을 때 작동함
    protected void RotateModelTowardsMovementDirection()
    {
        if (modelContainer == null) return;

        Vector3 direction = currentDestination - transform.position;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            // 핵심 : LookRoation은 +z 방향을 바라보게 만든다
            // forward : 바라볼 방향 / up : 윗 방향
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            modelContainer.transform.rotation = targetRotation;
        }
    }

    public void SetIsWaiting(bool isWaiting)
    {
        this.isWaiting = isWaiting;
    }

    protected virtual void SetSkills() { }
    
    // 보스에서 사용
    protected virtual bool TryUseSkill() { return false; }

    protected override void OnDisable()
    {
        DeployableUnitEntity.OnUndeployed -= HandleDeployableDied;

        if (navigator != null)
        {
            navigator.OnPathUpdated -= HandlePathUpdated;
            navigator.Cleanup();
        }

        base.OnDisable();
    }

}

