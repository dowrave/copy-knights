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
    protected PathData _pathData;

    protected EnemyStats currentStats;

    protected EnemyAttackController _attack;
    protected PathController _path;
    protected MovementController _movement;

    public EnemyAttackController Attack => _attack;
    public PathController Path => _path;
    public MovementController Movement => _movement;


    // 수정 필요할지도?
    public override AttackType AttackType => _attack.CurrentAttackType;
    public AttackRangeType AttackRangeType => BaseData.AttackRangeType;

    // ===== Stat 관련 ======
    public override float AttackPower { get => _stat.GetStat(StatType.AttackPower);}
    public override float AttackSpeed { get => _stat.GetStat(StatType.AttackSpeed);}
    public float MovementSpeed { get => _stat.GetStat(StatType.MovementSpeed); }
    public int BlockSize { get => (int)_stat.GetStat(StatType.BlockSize); } // Enemy가 차지하는 저지 수, 저지 수 자체가 변하는 로직은 없으니 게터만 구현
    public float AttackRange { get => BaseData.AttackRangeType == AttackRangeType.Melee ? 0f : _stat.GetStat(StatType.AttackRange); }

    // ===== attack 관련 ======
    public float AttackCooldown => _attack.AttackCooldown;
    public float AttackDuration => _attack.AttackDuration;
    public UnitEntity CurrentTarget => _attack.CurrentTarget;
    public IReadOnlyList<UnitEntity> TargetsInRange => _attack.TargetsInRange;
    public Operator BlockingOperator => _attack.BlockingOperator;

    // ===== path 관련 ======
    public Vector3 CurrentDestination => _path.CurrentDestination;
    public Vector3 FinalDestination => _path.FinalDestination;
    public IReadOnlyList<PathNode> CurrentPathNodes => _path.CurrentPathNodes;
    public IReadOnlyList<Vector3> CurrentPathPositions => _path.CurrentPathPositions;
    public int CurrentPathIndex => _path.CurrentPathIndex;
    public Barricade? TargetBarricade => _path.TargetBarricade;
    
    // ==== movement 관련 ======
    public bool IsWaiting => _movement.IsWaiting;

    protected int initialPoolSize = 5;

    [SerializeField] protected GameObject enemyBarUIPrefab = default!;
    protected EnemyBarUI? enemyBarUI;

    // 접촉 중인 타일 관리
    protected List<Tile> contactedTiles = new List<Tile>();

    protected Coroutine _adjustmentCoroutine;

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
        _movement = new MovementController(this);
        _path = new PathController(this);
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
        _attack.Initialize();
        _path.Initialize(_pathData.Nodes);

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
        
        // 경로의 0번 위치에 이 객체가 오도록 설정
        SetupInitialPosition();

        // 재사용할 일이 없어보이긴 하지만 일단 초기화에서도 구현 
        currentEnemyDespawnReason = EnemyDespawnReason.Null;

        isInitialized = true;
    }


    protected void OnEnable()
    {
        DeployableUnitEntity.OnUndeployed += HandleDeployableDied;
    }

    protected void SetupInitialPosition()
    {
        if (CurrentPathPositions != null && CurrentPathPositions.Count > 0)
        {
            transform.position = CurrentPathPositions[0];
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

        if (HasRestriction(ActionRestriction.CannotAction)) return; // 제약 중에는 행동 X(효과만 갱신)
        if (AttackDuration > 0) return; // 공격 모션 중에는 별도 행동 X

        // 스킬 사용, 공격, 행동 등 처리
        OnUpdateAction();
    }

    protected bool IsUpdateValid()
    {
        return StageManager.Instance!.CurrentGameState == GameState.Battle && 
            currentEnemyDespawnReason == EnemyDespawnReason.Null && 
            isInitialized;
    }

    protected virtual void UpdateAllCooldowns() => _attack.UpdateAllCooldowns();

    protected virtual void OnUpdateAction()
    {
        // 공격을 시도함
        bool attacked = _attack.OnUpdate();
        if (attacked) return;

        // 공격을 하지 않았다면 이동
        _movement.OnUpdate();
    }

    // 대기 중일 때 실행
    public IEnumerator WaitAtNode(float waitTime)
    {
        SetIsWaiting(true);
        yield return new WaitForSeconds(waitTime);
        SetIsWaiting(false);
        UpdateNextNode();
    }

    // 노드를 갱신해야 하는 상황에 호출
    // 다음 노드 인덱스를 설정하고 현재 목적지로 지정함
    // 스킬에서 접근할 수 있게 public으로 변경
    public void UpdateNextNode() => _path.UpdateNextNode();

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

    // 현재 경로상에서 목적지까지 남은 거리 계산
    public float GetRemainingPathDistance() => _path.GetRemainingPathDistance();

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

    // 저지 및 저지 해제 시에 동작
    public void UpdateBlockingOperator(Operator op) => _attack.UpdateBlockingOperator(op);

    public void SmoothAvoidance(Operator op)
    {
        // 저지 시에만 동작
        if (op != null)
        {
            // 겹쳤을 때를 고려한 위치 이동
            if (_adjustmentCoroutine != null)
            {
                StopCoroutine(_adjustmentCoroutine);
            }

            _adjustmentCoroutine = StartCoroutine(SmoothAvoidanceCoroutine(op.transform.position));
        }
    }

    // Enemy 끼리 겹쳤을 때 살짝의 이동
    protected IEnumerator SmoothAvoidanceCoroutine(Vector3 targetPos)
    {
        float duration = 0.1f;
        float elapsed = 0f;   

        Vector3 startPos = transform.position;
        
        // 방향, 수직 벡터
        Vector3 direction = startPos - targetPos;
        direction.y = 0f;

        // 수직 벡터 : (x, z)에 수직인 벡터는 (-z, x) 또는 (z, -x)
        Vector3 perpendicular = new Vector3(-direction.z, 0, direction.x).normalized;

        // 최종 목적지
        float randomSide = UnityEngine.Random.Range(0, 2) == 0 ? 1f : -1f;
        float randomDistance = UnityEngine.Random.Range(0.03f, 0.05f);
        Vector3 targetOffset = perpendicular * randomSide * randomDistance;
        Vector3 finalDestination = startPos + targetOffset;

        // 시간 동안 부드럽게 이동
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, finalDestination, t);
            yield return null;
        }

        transform.position = finalDestination;
        _adjustmentCoroutine = null;
    }
        

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
    public void RotateModel()
    {
        if (modelContainer == null) return;

        Vector3 direction = CurrentDestination - transform.position;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            // 핵심 : LookRoation은 +z 방향을 바라보게 만든다
            // forward : 바라볼 방향 / up : 윗 방향
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            modelContainer.transform.rotation = targetRotation;
        }
    }

    public void SetIsWaiting(bool isWaiting) => _movement.SetIsWaiting(isWaiting);
    public void OnReachDestination() => Despawn(EnemyDespawnReason.ReachedGoal);

    protected virtual void SetSkills() { }
    
    // 보스에서 사용
    protected virtual bool TryUseSkill() { return false; }

    protected override void OnDisable()
    {
        DeployableUnitEntity.OnUndeployed -= HandleDeployableDied;

        if (_path != null)
        {
            _path.Cleanup();
        }

        base.OnDisable();
    }
}

