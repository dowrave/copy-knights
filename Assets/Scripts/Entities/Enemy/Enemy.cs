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

    // 수정 필요할지도?
    public override AttackType AttackType => BaseData.AttackType; // 수정 필요
    public AttackRangeType AttackRangeType => BaseData.AttackRangeType;
    public float AttackCooldown { get; set; } // 다음 공격까지의 대기 시간
    public float AttackDuration { get; set; } // 공격 모션 시간. Animator가 추가될 때 수정 필요할 듯. 항상 Cooldown보다 짧아야 함.

    public override float AttackPower { get => Stat.GetStat(StatType.AttackPower);}
    public override float AttackSpeed { get => Stat.GetStat(StatType.AttackSpeed);}
    public float MovementSpeed { get => Stat.GetStat(StatType.MovementSpeed); }
    public int BlockSize { get => (int)Stat.GetStat(StatType.BlockSize); } // Enemy가 차지하는 저지 수, 저지 수 자체가 변하는 로직은 없으니 게터만 구현
    public float AttackRange { get => BaseData.AttackRangeType == AttackRangeType.Melee ? 0f : Stat.GetStat(StatType.AttackRange); }

    // 경로 관련
    protected PathNavigator navigator;
    protected Barricade? targetBarricade;
    protected List<PathNode> currentPathNodes = new List<PathNode>();
    protected List<Vector3> currentPathPositions = new List<Vector3>();
    protected Vector3 currentDestination; // 현재 향하는 위치
    protected int _currentPathIndex;
    protected bool isWaiting = false; // 단순히 위치에서 기다리는 상태
    protected bool stopAttacking = false; // 인위적으로 넣은 공격 가능 / 불가능 상태
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

    // 저지, 공격 대상 관련
    protected Operator? blockingOperator; // 자신을 저지 중인 오퍼레이터
    public Operator? BlockingOperator => blockingOperator;
    protected UnitEntity? _currentTarget;
    public UnitEntity? CurrentTarget
    {
        get => _currentTarget;
        protected set
        {
            // 타겟이 기존 값과 동일하다면 세터 실행 X
            if (_currentTarget == value) return;

            // 기존 타겟의 이벤트 구독 해제
            if (_currentTarget != null)
            {
                _currentTarget.RemoveAttackingEntity(this);
            }

            _currentTarget = value;

            if (_currentTarget != null)
            {
                NotifyTarget();
            }
        }
    } // 공격 대상
    
    protected List<UnitEntity> targetsInRange = new List<UnitEntity>();
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

        // 공격 범위 콜라이더 설정
        attackRangeController.Initialize(this);

        // 스킬 설정 
        SetSkills();

        // 재사용할 일이 없어보이긴 하지만 일단 초기화에서도 구현 
        currentEnemyDespawnReason = EnemyDespawnReason.Null;

        isInitialized = true;
    }


    protected void OnEnable()
    {
        DeployableUnitEntity.OnDeployableDied += HandleDeployableDied;
    }

    protected void OnDisable()
    {
        DeployableUnitEntity.OnDeployableDied -= HandleDeployableDied;

        if (navigator != null)
        {
            navigator.OnPathUpdated -= HandlePathUpdated;
            navigator.Cleanup();
        }
        // Barricade.OnBarricadeDeployed -= OnBarricadePlaced;
        // Barricade.OnBarricadeRemoved -= OnBarricadeRemovedWithDelay;
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
        bool updateCondition = StageManager.Instance!.CurrentGameState == GameState.Battle && 
                currentEnemyDespawnReason == EnemyDespawnReason.Null && 
                isInitialized;

        if (updateCondition)
        {
            // 행동이 불가능해도 동작해야 하는 효과
            UpdateAllCooldowns();
            base.Update(); // 버프 효과 갱신

            if (HasRestriction(ActionRestriction.CannotAction)) return;

            // 이동, 공격 등 상황에 따른 판단
            DecideAndPerformAction();
        }
    }

    protected virtual void UpdateAllCooldowns()
    {
        UpdateAttackDuration();
        UpdateAttackCooldown();
    }

    // 행동 규칙.
    protected virtual void DecideAndPerformAction()
    {
        if (CurrentPathIndex < currentPathPositions.Count)
        {
            if (AttackDuration > 0) return;  // 공격 모션 중

            // 공격 범위 내의 적 리스트 & 현재 공격 대상 갱신
            SetCurrentTarget();

            if (TryUseSkill()) return;

            // 저지당함 - 근거리 공격
            if (blockingOperator != null && CurrentTarget == blockingOperator)
            {
                if (CanAttack())
                {
                    PerformMeleeAttack(CurrentTarget!, AttackPower);
                }
            }
            else
            {
                // 바리케이트가 타겟일 경우
                if (targetBarricade != null && Vector3.Distance(transform.position, targetBarricade.transform.position) < 0.5f)
                {
                    PerformMeleeAttack(targetBarricade, AttackPower);
                }

                // 타겟이 있고, 공격이 가능한 상태
                if (CanAttack())
                {
                    Attack(CurrentTarget!, AttackPower);
                }

                // 이동 관련 로직.
                else if (!isWaiting)
                {
                    MoveAlongPath(); // 이동
                }
            }
        }
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

    public virtual void OnTargetEnteredRange(DeployableUnitEntity target)
    {
        if (target == null ||
        target.Faction != Faction.Ally || // Ally만 공격함
        !target.IsDeployed || // 배치된 요소만 공격함
        targetsInRange.Contains(target)) return; // 이미 지정한 요소는 공격하지 않음

        targetsInRange.Add(target);
    }

    public virtual void OnTargetExitedRange(DeployableUnitEntity target)
    {
        if (targetsInRange.Contains(target))
        {
            targetsInRange.Remove(target);
        }
    }

    public void Attack(UnitEntity target, float damage)
    {
        float polishedDamage = Mathf.Floor(damage);
        PerformAttack(target, polishedDamage);
    }

    protected void PerformAttack(UnitEntity target, float damage)
    {
        AttackType finalAttackType = AttackType;
        bool showDamagePopup = false;

        foreach (var buff in ActiveBuffs)
        {
            buff.OnBeforeAttack(this, ref damage, ref finalAttackType, ref showDamagePopup);
        }

        switch (AttackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target, damage);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target, damage);
                break;
        }
        
        foreach (var buff in ActiveBuffs)
        {
            buff.OnAfterAttack(this, target);
        }
    }

    protected void PerformMeleeAttack(UnitEntity target, float damage)
    {
        SetAttackTimings(); // 이걸 따로 호출하는 경우가 있어서 여기서 다시 설정

        AttackSource attackSource = new AttackSource(
            attacker: this,
            position: transform.position,
            damage: damage,
            type: AttackType,
            isProjectile: false,
            hitEffectTag: _enemyData.HitVFXTag,
            showDamagePopup: false
        );

        PlayMeleeAttackEffect(target, attackSource);
        target.TakeDamage(attackSource);
    }

    protected void PerformRangedAttack(UnitEntity target, float damage)
    {
        SetAttackTimings();
        
        if (_enemyData.ProjectilePrefab != null)
        {
            // 투사체 생성 위치
            Vector3 spawnPosition = transform.position;
            GameObject? projectileObj = ObjectPoolManager.Instance!.SpawnFromPool(_enemyData.ProjectileTag, spawnPosition, Quaternion.identity);

            if (projectileObj != null)
            {
                Projectile? projectile = projectileObj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(this, target, damage, false, _enemyData.ProjectileTag, _enemyData.HitVFXTag, AttackType);
                }
            }
        }
    }

    protected override void HandleOnDeath()
    {
        Despawn(EnemyDespawnReason.Defeated);
    }

    protected bool IsTargetInRange(UnitEntity target)
    {
        return Vector3.Distance(target.transform.position, transform.position) <= AttackRange;
    }

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

    // Enemy가 공격할 대상 지정. Update에서 계속 돌아갈 필요가 있다.
    public void SetCurrentTarget()
    {
        // 현재 타겟이 나를 저지 중인 오퍼레이터라면 변경할 필요 X
        if (blockingOperator != null && CurrentTarget == blockingOperator) return;

        // 1. 자신을 저지하는 오퍼레이터를 타겟으로 설정
        if (blockingOperator != null)
        {
            CurrentTarget = blockingOperator;
            return;
        }

        // 2. 공격 범위 내의 타겟
        if (targetsInRange.Count > 0)
        {
            // 타겟 선정
            UnitEntity? newTarget = targetsInRange
                .OfType<Operator>() // 오퍼레이터
                .OrderByDescending(o => o.DeploymentOrder) // 가장 나중에 배치된 오퍼레이터 
                .FirstOrDefault();

            // 타겟의 유효성 검사 : 공격 범위 내에 있고 null이 아닐 때
            if (newTarget != null || targetsInRange.Contains(newTarget))
            {
                CurrentTarget = newTarget;
            }

            return;
        }

        // 3. 위의 두 조건에 해당하지 않는다면 타겟을 제거한다.
        CurrentTarget = null;
    }

    // Enemy가 공격 대상으로 삼은 적이 죽었을 때 동작
    public void HandleDeployableDied(DeployableUnitEntity disabledEntity)
    {
        if (targetsInRange.Contains(disabledEntity))
        {
            targetsInRange.Remove(disabledEntity);

        }

        // 타겟이 범위에서 벗어났어도 현재 타겟으로 지정되는 상황이 있을 수 있으니 위 조건과는 별도로 구현했음
        if (CurrentTarget == disabledEntity)
        {
            CurrentTarget = null;
        }
    }


    // CurrentTarget에게 자신이 공격하고 있음을 알림
    public void NotifyTarget()
    {
        CurrentTarget?.AddAttackingEntity(this);
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

    // 인터페이스 때문에 구현
    public void UpdateAttackDuration()
    {
        if (AttackDuration > 0f)
        {
            AttackDuration -= Time.deltaTime;
        }
    }

    public void UpdateAttackCooldown()
    {
        if (AttackCooldown > 0f)
        {
            AttackCooldown -= Time.deltaTime;
        }
    }


    // 공격 모션 시간, 공격 쿨타임 시간 설정
    public void SetAttackTimings()
    {
        if (AttackDuration <= 0f)
        {
            SetAttackDuration();
        }
        if (AttackCooldown <= 0f)
        {
            SetAttackCooldown();
        }
    }

    public void SetAttackDuration(float? intentionalDuration = null)
    {
        AttackDuration = AttackSpeed / 3f;
    }

    public void SetAttackCooldown(float? intentionalCooldown = null)
    {
        AttackCooldown = AttackSpeed;
    }

    public bool CanAttack()
    {
        return CurrentTarget != null &&
            AttackCooldown <= 0 &&
            AttackDuration <= 0 &&
            !stopAttacking; 
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
    
    protected void RemoveTargetFromInRange(DeployableUnitEntity deployable)
    {
        if (targetsInRange.Contains(deployable))
        {
            targetsInRange.Remove(deployable);
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

    public void UpdateBlockingOperator(Operator? op)
    {
        blockingOperator = op;
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

    public void SetStopAttacking(bool isAttacking)
    {
        this.stopAttacking = isAttacking;
    }

    public void SetCurrentBarricade(Barricade? barricade)
    {
        targetBarricade = barricade;
    } 

    protected virtual void SetSkills() { }
    
    // 보스에서 사용
    protected virtual bool TryUseSkill() { return false; }
}

