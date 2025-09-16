using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static ICombatEntity;
using System;

public enum DespawnReason
{
    Null, // 디폴트
    Defeated, // 처치됨
    ReachedGoal // 목적지 도달
}

public class Enemy : UnitEntity, IMovable, ICombatEntity
{
    [SerializeField] protected EnemyData enemyData = default!;
    public virtual EnemyData BaseData => enemyData;

    protected EnemyStats currentStats;

    public override AttackType AttackType => BaseData.attackType;
    public override float AttackPower { get => currentStats.AttackPower; set => currentStats.AttackPower = value; }
    public override float AttackSpeed { get => currentStats.AttackSpeed; set => currentStats.AttackSpeed = value; }
    public AttackRangeType AttackRangeType => BaseData.attackRangeType;
    public override float MovementSpeed { get => currentStats.MovementSpeed; }
    public int BlockCount { get => BaseData.blockCount; protected set => BaseData.blockCount = value; } // Enemy가 차지하는 저지 수
    public float AttackCooldown { get; set; } // 다음 공격까지의 대기 시간
    public float AttackDuration { get; set; } // 공격 모션 시간. Animator가 추가될 때 수정 필요할 듯. 항상 Cooldown보다 짧아야 함.

    public float AttackRange
    {
        get
        {
            return BaseData.attackRangeType == AttackRangeType.Melee ? 0f : currentStats.AttackRange;
        }
        protected set
        {
            currentStats.AttackRange = value;
        }
    }

    public string EntityName => BaseData.entityName;

    // 경로 관련
    protected PathData? pathData;
    protected List<Vector3> currentPath = new List<Vector3>();
    protected List<UnitEntity> targetsInRange = new List<UnitEntity>();
    protected PathNode nextNode = default!;
    protected int nextNodeIndex = 0; // 시작하자마자 1이 됨
    protected Vector3 nextNodeWorldPosition; // 다음 노드의 좌표
    protected Vector3 destinationPosition; // 목적지
    protected bool isWaiting = false; // 단순히 위치에서 기다리는 상태
    protected bool stopAttacking = false; // 인위적으로 넣은 공격 가능 / 불가능 상태
    protected Barricade? targetBarricade;

    public PathNode NextNode => nextNode;
    public Vector3 NextNodeWorldPosition => nextNodeWorldPosition; 
    public Vector3 DestinationPosition => DestinationPosition; 
    

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
                _currentTarget.OnDeathAnimationCompleted -= OnCurrentTargetDied;
                _currentTarget.RemoveAttackingEntity(this);
            }

            _currentTarget = value;

            if (_currentTarget != null)
            {
                _currentTarget.OnDeathAnimationCompleted += OnCurrentTargetDied;
                NotifyTarget();
            }
        }
    } // 공격 대상

    protected int initialPoolSize = 5;
    protected string? projectileTag;

    [SerializeField] protected GameObject enemyBarUIPrefab = default!;
    protected EnemyBarUI? enemyBarUI;

    // 접촉 중인 타일 관리
    protected List<Tile> contactedTiles = new List<Tile>();

    // 메쉬의 회전 관련해서 모델 관리
    [Header("Model Components")]
    [SerializeField] protected GameObject modelContainer = default!;

    [SerializeField] protected EnemyAttackRangeController attackRangeController = default!;

    // ICrowdControlTarget
    public Vector3 Position => transform.position;

    protected DespawnReason currentDespawnReason = DespawnReason.Null;

    protected bool isInitialized = false;

    // 스태틱 이벤트 테스트
    // public static event Action<Enemy> OnEnemyDestroyed; // 죽는 상황 + 목적지에 도달해서 사라지는 상황 모두 포함
    public static event Action<Enemy, DespawnReason> OnEnemyDespawned = delegate { };

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

    // 모델 회전 관련 로직을 쓸 일이 Enemy 뿐이라 여기에 구현해놓음.
    protected void InitializeModelComponents()
    {
        if (modelContainer == null)
        {
            modelContainer = transform.Find("ModelContainer").gameObject;
        }
    }

    public virtual void Initialize(EnemyData enemyData, PathData pathData)
    {
        this.enemyData = enemyData;
        SetPrefab();
        currentStats = enemyData.stats;
        this.pathData = pathData;

        InitializeHP();

        SetupInitialPosition();
        CreateEnemyBarUI();
        UpdateNextNode();
        InitializeCurrentPath();


        // 공격 범위 콜라이더 설정
        attackRangeController.Initialize(this);

        // 최초에 설정한 경로가 막힌 상황일 때 동작
        if (PathfindingManager.Instance!.IsBarricadeDeployed && IsPathBlocked())
        {
            FindPathToDestinationOrBarricade();
        }

        // 스킬 설정 
        SetSkills();

        // 오브젝트 풀 생성
        CreateObjectPool();

        isInitialized = true;
    }

    public override void SetPrefab()
    {
        prefab = enemyData.prefab;
    }

    protected void OnEnable()
    {
        Barricade.OnBarricadeDeployed += OnBarricadePlaced;
        Barricade.OnBarricadeRemoved += OnBarricadeRemovedWithDelay;
    }

    protected void OnDisable()
    {
        Barricade.OnBarricadeDeployed -= OnBarricadePlaced;
        Barricade.OnBarricadeRemoved -= OnBarricadeRemovedWithDelay;
    }

    protected void SetupInitialPosition()
    {
        if (pathData == null || pathData.nodes == null) return;

        if (pathData!.nodes!.Count > 0)
        {
            transform.position = MapManager.Instance!.ConvertToWorldPosition(pathData.nodes![0].gridPosition) +
                Vector3.up * BaseData.defaultYPosition;
        }
    }

    protected override void Update()
    {
        if (StageManager.Instance!.currentState == GameState.Battle && // 전투 중이면서
            currentDespawnReason == DespawnReason.Null && // 디스폰되고 있지 않을 때
            isInitialized
            )
        {
            // 행동이 불가능해도 동작해야 하는 효과
            UpdateAllCooldowns();
            base.Update(); // 버프 효과 갱신

            if (HasRestriction(ActionRestriction.CannotAction)) return;

            // 판단하고 행동하는 로직을 가상 메서드로 분리, 자식 클래스에서 별개로 구현할 수 있도록 함
            // 이를 템플릿 메서드 패턴이라고 한다.
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
        if (nextNodeIndex < pathData.nodes.Count)
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

    // 보스에서 사용
    protected virtual bool TryUseSkill() { return false; }

    // pathData.nodes를 이용해 currentPath 초기화
    protected void InitializeCurrentPath()
    {
        if (pathData == null || pathData.nodes == null) throw new InvalidOperationException("null인 변수가 존재");

        foreach (var node in pathData.nodes)
        {
            currentPath.Add(MapManager.Instance!.ConvertToWorldPosition(node.gridPosition) + Vector3.up * BaseData.defaultYPosition);
        }

        destinationPosition = currentPath[currentPath.Count - 1]; // 목적지 설정
    }

    // 경로를 따라 이동
    protected void MoveAlongPath()
    {
        if (nextNodeWorldPosition == null || destinationPosition == null) throw new InvalidOperationException("다음/목적지 노드가 설정되어있지 않음");

        if (CheckIfReachedDestination())
        {
            ReachDestination();
            return;
        }

        Move(nextNodeWorldPosition);
        RotateModelTowardsMovementDirection();

        // 노드 도달 확인
        if (Vector3.Distance(transform.position, nextNodeWorldPosition) < 0.05f)
        {
            // 목적지 도달
            if (Vector3.Distance(transform.position, destinationPosition) < 0.05f)
            {
                ReachDestination();
            }
            // 기다려야 하는 경우
            else if (nextNode.waitTime > 0)
            {
                StartCoroutine(WaitAtNode(nextNode.waitTime));
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
        if (pathData == null || pathData.nodes == null || nextNodeIndex >= pathData.nodes.Count - 1) return;

        nextNodeIndex++;
        if (nextNodeIndex < pathData.nodes.Count)
        {
            nextNode = pathData.nodes[nextNodeIndex];
            nextNodeWorldPosition = MapManager.Instance!.ConvertToWorldPosition(nextNode.gridPosition) +
                Vector3.up * BaseData.defaultYPosition;
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

        foreach (var buff in activeBuffs)
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
        
        foreach (var buff in activeBuffs)
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
            hitEffectPrefab: BaseData.HitEffectPrefab,
            hitEffectTag: hitEffectTag,
            showDamagePopup: false
        );

        PlayMeleeAttackEffect(target, attackSource);
        target.TakeDamage(attackSource);
    }

    protected void PerformRangedAttack(UnitEntity target, float damage)
    {
        SetAttackTimings();
        
        if (BaseData.projectilePrefab != null && projectileTag != null)
        {
            // 투사체 생성 위치
            Vector3 spawnPosition = transform.position;
            GameObject? projectileObj = ObjectPoolManager.Instance!.SpawnFromPool(projectileTag, spawnPosition, Quaternion.identity);

            if (projectileObj != null)
            {
                Projectile? projectile = projectileObj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(this, target, damage, false, projectileTag, BaseData.hitEffectPrefab, hitEffectTag, AttackType);
                }
            }
        }
    }

    protected bool IsTargetInRange(UnitEntity target)
    {
        return Vector3.Distance(target.transform.position, transform.position) <= AttackRange;
    }

    // 사라지는 로직 관리
    protected void Despawn()
    {
        // 공격 이펙트 프리팹 제거
        if (BaseData.hitEffectPrefab != null)
        {
            ObjectPoolManager.Instance!.RemovePool("Effect_" + BaseData.entityName);
        }

        // UI 제거
        if (enemyBarUI != null)
        {
            Destroy(enemyBarUI.gameObject);
        }

        // 킬 카운트 / 도착 등은 바로 동작하게 수정
        OnEnemyDespawned?.Invoke(this, currentDespawnReason);

        PlayDeathAnimation(); // 내부 이벤트 발생으로 인해 HandleDeathAnimationCompleted도 실행됨.
    }

    protected override void Die()
    {
        // 이미 처리 중인지 확인
        if (currentDespawnReason != DespawnReason.Null) return;

        // 사망 이벤트 처리
        currentDespawnReason = DespawnReason.Defeated;
        Despawn();
    }

    protected void ReachDestination()
    {
        // 이미 처리 중인지 확인
        if (currentDespawnReason != DespawnReason.Null) return;

        currentDespawnReason = DespawnReason.ReachedGoal;
        Despawn();
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
        if (pathData == null || pathData.nodes == null) throw new InvalidOperationException("pathData, pathData.nodes가 없음");

        if (pathData.nodes.Count == 0) return false;

        Vector2Int lastNodeGridPos = pathData!.nodes![pathData!.nodes!.Count - 1].gridPosition;
        Vector3 lastNodePosition = MapManager.Instance!.ConvertToWorldPosition(lastNodeGridPos) + Vector3.up * 0.5f;

        return Vector3.Distance(transform.position, lastNodePosition) < 0.05f;
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
    public void OnCurrentTargetDied(UnitEntity destroyedTarget)
    {
        // 타겟이 파괴되었다는 의미이므로 CurrentTarget = null로만 설정해준다.
        if (CurrentTarget == destroyedTarget)
        {
            CurrentTarget = null;
        }
    }


    // CurrentTarget에게 자신이 공격하고 있음을 알림
    public void NotifyTarget()
    {
        CurrentTarget?.AddAttackingEntity(this);
    }

    public void InitializeProjectilePool()
    {
        if (AttackRangeType == AttackRangeType.Ranged && BaseData.projectilePrefab != null)
        {
            projectileTag = $"{BaseData.entityName}_Projectile";
            ObjectPoolManager.Instance!.CreatePool(projectileTag, BaseData.projectilePrefab, initialPoolSize);
        }
    }


    // 현재 경로상에서 목적지까지 남은 거리 계산
    public float GetRemainingPathDistance()
    {
        if (currentPath.Count == 0 || nextNodeIndex > +currentPath.Count)
        {
            return float.MaxValue;
        }

        float distance = 0f;
        for (int i = nextNodeIndex; i < currentPath.Count - 1; i++)
        {
            // 첫 타일에 한해서만 현재 위치를 기반으로 계산(여러 Enemy가 같은 타일에 있을 수 있기 때문)
            if (i == nextNodeIndex)
            {
                Vector3 nowPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                distance += Vector3.Distance(nowPosition, currentPath[i + 1]);
            }

            distance += Vector3.Distance(currentPath[i], currentPath[i + 1]);
        }

        return distance;
    }


    // Barricade 설치 시 현재 경로가 막혔다면 재계산
    protected void OnBarricadePlaced(Barricade barricade)
    {
        // 내 타일과 같은 타일에 바리케이드가 배치된 경우
        if (barricade.CurrentTile != null &&
            barricade.CurrentTile.EnemiesOnTile.Contains(this))
        {
            targetBarricade = barricade;
        }

        // 현재 사용 중인 경로가 막힌 경우
        else if (IsPathBlocked())
        {
            FindPathToDestinationOrBarricade();
        }
    }

    protected void OnBarricadeRemovedWithDelay(Barricade barricade)
    {
        StartCoroutine(OnBarricadeRemoved(barricade));
    }

    // 바리케이드 제거 시 동작
    protected IEnumerator OnBarricadeRemoved(Barricade barricade)
    {
        // 바리케이드가 파괴될 시간 확보
        yield return new WaitForSeconds(0.1f);

        // 바리케이드와 관계 없던 Enemy는 경로를 다시 탐색
        if (targetBarricade == null)
        {
            FindPathToDestinationOrBarricade();
        }

        // 해당 바리케이드가 목표였던 Enemy들의 targetBarricade 해제
        else if (targetBarricade == barricade)
        {
            targetBarricade = null;
            FindPathToDestinationOrBarricade();
        }

        // 다른 바리케이드를 목표로 하고 있다면 별도 동작 X
    }

    // 현재 pathData를 사용하는 경로가 막혔는지를 점검한다
    protected bool IsPathBlocked()
    {
        if (currentPath.Count == 0) throw new InvalidOperationException("currentPath가 비어 있음");

        for (int i = nextNodeIndex; i <= currentPath.Count - 1; i++)
        {
            // 경로가 막힌 상황 : 기존 경로 데이터들을 정리한다
            if ((i == nextNodeIndex && PathfindingManager.Instance!.IsPathSegmentValid(transform.position, currentPath[i]) == false) ||
                PathfindingManager.Instance!.IsPathSegmentValid(currentPath[i], currentPath[i + 1]) == false)
            {
                pathData = null;
                currentPath.Clear();
                return true;
            }
        }


        return false;
    }


    // CalculatePath로 탐색된 경로를 받아와 pathData와 currentPath 초기화
    protected void SetNewPath(List<PathNode> newPathNodes)
    {
        if (newPathNodes != null && newPathNodes.Count > 0)
        {
            PathData newPathData = ScriptableObject.CreateInstance<PathData>();
            newPathData.nodes = newPathNodes;
            pathData = newPathData;
            currentPath = newPathNodes.Select(node => MapManager.Instance!.ConvertToWorldPosition(node.gridPosition) + Vector3.up * 0.5f).ToList();

            nextNodeIndex = 0;

            UpdateNextNode();
        }
    }

    // targetPosition으로 향하는 경로를 계산하고, 경로가 있다면 새로운 pathData와 currentPath로 설정함
    protected bool CalculateAndSetPath(Vector3 currentPosition, Vector3 targetPosition)
    {
        List<PathNode>? tempPathNodes = PathfindingManager.Instance!.FindPathAsNodes(currentPosition, targetPosition);

        if (tempPathNodes == null || tempPathNodes.Count == 0) return false; // 목적지로 향하는 경로가 없음

        SetNewPath(tempPathNodes);
        return true;
    }

    // 현재 위치에서 가장 가까운 바리케이드를 설정하고, 바리케이드로 향하는 경로를 설정함
    protected void SetBarricadePath()
    {
        targetBarricade = PathfindingManager.Instance!.GetNearestBarricade(transform.position);

        if (targetBarricade != null)
        {
            CalculateAndSetPath(transform.position, targetBarricade.transform.position);
        }
    }

    // 목적지로 향하는 경로를 찾고, 없다면 가장 가까운 바리케이드로 향하는 경로를 설정함
    protected void FindPathToDestinationOrBarricade()
    {
        if (!CalculateAndSetPath(transform.position, destinationPosition))
        {
            Debug.Log("목적지로 향하는 경로 발견 및 설정");
            SetBarricadePath();
        }
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

    protected virtual void CreateObjectPool()
    {
        // 객체의 종류마다 풀을 공유함
        string baseTag = BaseData.entityName;

        // 근접 공격 이펙트 풀 생성
        if (BaseData.meleeAttackEffectPrefab != null)
        {
            meleeAttackEffectTag = baseTag + BaseData.meleeAttackEffectPrefab.name;
            ObjectPoolManager.Instance!.CreatePool(
                meleeAttackEffectTag,
                BaseData.meleeAttackEffectPrefab
            );
        }

        // 적 타격 이펙트 풀 생성
        if (BaseData.hitEffectPrefab != null)
        {
            hitEffectTag = baseTag + BaseData.hitEffectPrefab.name;
            ObjectPoolManager.Instance!.CreatePool(
                hitEffectTag,
                BaseData.hitEffectPrefab
            );
        }

        InitializeProjectilePool();
    }

    protected void PlayMeleeAttackEffect(UnitEntity target, AttackSource attackSource)
    {
        // 이펙트 처리
        if (meleeAttackEffectTag != null && BaseData.meleeAttackEffectPrefab != null)
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
            GameObject uiObject = Instantiate(enemyBarUIPrefab, transform);
            enemyBarUI = uiObject.GetComponentInChildren<EnemyBarUI>();
            if (enemyBarUI != null)
            {
                enemyBarUI.Initialize(this);
            }
        }
    }

    protected override void InitializeHP()
    {
        MaxHealth = Mathf.Floor(currentStats.Health);
        CurrentHealth = Mathf.Floor(MaxHealth);
    }

    public void UpdateBlockingOperator(Operator? op)
    {
        blockingOperator = op;
    }

    protected override float CalculateActualDamage(AttackType attacktype, float incomingDamage)
    {
        float actualDamage = 0; // 할당까지 필수

        switch (attacktype)
        {
            case AttackType.Physical:
                actualDamage = incomingDamage - currentStats.Defense;
                break;
            case AttackType.Magical:
                actualDamage = incomingDamage * (1 - currentStats.MagicResistance / 100);
                break;
            case AttackType.True:
                actualDamage = incomingDamage;
                break;
        }

        return Mathf.Max(actualDamage, 0.05f * incomingDamage); // 들어온 대미지의 5%는 들어가게끔 보장
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

    public override void SetMovementSpeed(float newSpeed)
    {
        currentStats.MovementSpeed = newSpeed;
    }

    // 모델을 이동 방향으로 회전시킴
    // 참고) 프리팹 기준 +z 방향으로 이동한다고 가정했을 때 작동함
    protected void RotateModelTowardsMovementDirection()
    {
        if (modelContainer == null) return;

        Vector3 direction = nextNodeWorldPosition - transform.position;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {

            // 핵심 : LookRoation은 +z 방향을 바라보게 만든다
            // forward : 바라볼 방향 / up : 윗 방향
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            modelContainer.transform.rotation = targetRotation;

            // 다른 방법
            //direction.Normalize();
            //float angle = Vector3.SignedAngle(modelContainer.transform.forward, direction, Vector3.up);
            //modelContainer.transform.eulerAngles = new Vector3(0, angle, 0);

            // 만약 부드러운 회전을 원한다면
            //model.transform.rotation = Quaternion.Slerp(
            //    model.transform.rotation, 
            //    targetRotation, 
            //    rotationSpeed * Time.deltaTime
            //    );
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

    protected virtual void SetSkills() { }

    protected void OnDestroy()
    {
    }

}

