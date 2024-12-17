using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class Enemy : UnitEntity, IMovable, ICombatEntity
{
    [SerializeField]
    private EnemyData enemyData;
    public new EnemyData BaseData => enemyData;

    private EnemyStats currentStats;

    public AttackType AttackType => enemyData.attackType;
    public AttackRangeType AttackRangeType => enemyData.attackRangeType;
    public float AttackPower { get => currentStats.AttackPower; private set => currentStats.AttackPower = value; }
    public float AttackSpeed { get => currentStats.AttackSpeed; private set => currentStats.AttackSpeed = value; }
    public float MovementSpeed { get => currentStats.MovementSpeed; private set => currentStats.MovementSpeed = value; }


    public int BlockCount { get => enemyData.blockCount; private set => enemyData.blockCount = value; } // Enemy가 차지하는 저지 수

    public float AttackCooldown { get; private set; } // 다음 공격까지의 대기 시간
    public float AttackDuration { get; private set; } // 공격 모션 시간. Animator가 추가될 때 수정 필요할 듯. 항상 Cooldown보다 짧아야 함.

    public float AttackRange
    {
        get
        {
            return enemyData.attackRangeType == AttackRangeType.Melee ? 0f : currentStats.AttackRange;
        }
        private set
        {
            currentStats.AttackRange = value;
        }
    }

    // 경로 관련
    private PathData pathData;
    private int currentNodeIndex = 0;
    private List<Vector3> currentPath = new List<Vector3>();
    private List<UnitEntity> targetsInRange = new List<UnitEntity>();
    private PathNode nextNode;
    private Vector3 nextPosition; // 다음 노드의 좌표
    private Vector3 destinationPosition; // 목적지
    private bool isWaiting = false;
    private Barricade targetBarricade;

    private Operator blockingOperator; // 자신을 저지 중인 오퍼레이터
    public bool IsBlocked { get { return blockingOperator != null; } }
    public UnitEntity CurrentTarget { get; private set; } // 공격 대상임!!

    protected int initialPoolSize = 5;
    protected string? projectileTag;

    [SerializeField] private GameObject enemyBarUIPrefab;
    private EnemyBarUI enemyBarUI;

    private void Awake()
    {
        Faction = Faction.Enemy;
    }

    public void Initialize(EnemyData enemyData, PathData pathData)
    {
        this.enemyData = enemyData;
        currentStats = enemyData.stats;
        this.pathData = pathData;

        InitializeUnitProperties();
        InitializeEnemyProperties();

        // 공격 이펙트 풀 생성
        if (BaseData.hitEffectPrefab != null)
        {
            ObjectPoolManager.Instance.CreateEffectPool(
                BaseData.entityName,
                BaseData.hitEffectPrefab
            );
        }
    }

    private void OnEnable()
    {
        Barricade.OnBarricadeDeployed += OnBarricadePlaced;
        Barricade.OnBarricadeRemoved += OnBarricadeRemovedWithDelay;
    }

    private void OnDisable()
    {
        Barricade.OnBarricadeDeployed -= OnBarricadePlaced;
        Barricade.OnBarricadeRemoved -= OnBarricadeRemovedWithDelay;
    }

    private void InitializeEnemyProperties()
    {
        SetupInitialPosition();
        CreateEnemyBarUI();
        UpdateNextNode();
        InitializeCurrentPath();

        // 최초에 설정한 경로가 막힌 상황일 때 동작
        if (PathfindingManager.Instance.IsBarricadeDeployed && IsPathBlocked())
        {
            FindPathToDestinationOrBarricade();
        }

        if (AttackRangeType == AttackRangeType.Ranged)
        {
            InitializeProjectilePool();
        }
    }

    private void SetupInitialPosition()
    {
        if (pathData != null && pathData.nodes.Count > 0)
        {
            // 스포너의 위치랑 동일해도 상관없는데 pathData가 또 별도로 있어서 거시기하다.
            transform.position = MapManager.Instance.ConvertToWorldPosition(pathData.nodes[0].gridPosition) + Vector3.up * 0.5f;
        }
        else
        {
            Debug.LogWarning("PathData is not set or empty. Initial position not set.");
        }
    }

    private void Update()
    {
        UpdateAttackTimings();

        // 진행할 경로가 있다
        if (pathData != null && currentNodeIndex < pathData.nodes.Count)
        {
            if (AttackDuration > 0) { return; } // 공격 모션 중일 때는 이동도 막음

            // 공격 범위 내의 적 리스트 & 현재 공격 대상 갱신
            SetCurrentTarget();

            // 1. 저지 중인 오퍼레이터가 있을 때
            if (blockingOperator != null)
            {
                // 공격 가능한 상태라면
                if (CurrentTarget != null && AttackCooldown <= 0)
                {
                    PerformMeleeAttack(CurrentTarget, AttackType, AttackPower); // 저지를 당하는 상태라면, 적의 공격 범위에 관계 없이 근거리 공격을 함
                }
            }

            // 2. 저지 중이 아닐 때
            else
            {
                if (targetBarricade != null && Vector3.Distance(transform.position, targetBarricade.transform.position) < 0.5f)
                {
                    PerformMeleeAttack(targetBarricade, AttackType, AttackPower); // 무조건 근거리 공격
                }

                // 타겟이 있고, 공격이 가능한 상태
                if (CanAttack())
                {
                    Attack(CurrentTarget, AttackType, AttackPower);
                }

                // 이동 관련 로직. 저지 중이 아닐 때에만 동작해야 한다. 
                else if (!isWaiting) // 경로 이동 중 기다리는 상황이 아니라면
                {
                    MoveAlongPath(); // 이동
                    CheckAndAddBlockingOperator(); // 같은 타일에 있는 오퍼레이터의 저지 가능 여부 체크
                }
            }
        }
    }

    protected void OnDestroy()
    {
        // 타일에서 제거
        CurrentTile.EnemyExited(this);

        if (AttackRangeType == AttackRangeType.Ranged)
        {
            CleanupProjectilePool();
        }
    }

    /// <summary>
    /// pathData.nodes를 이용해 currentPath 초기화
    /// </summary>
    private void InitializeCurrentPath()
    {
        foreach (var node in pathData.nodes)
        {
            currentPath.Add(MapManager.Instance.ConvertToWorldPosition(node.gridPosition) + Vector3.up * 0.5f);
        }
        destinationPosition = currentPath[currentPath.Count - 1]; // 목적지 설정
    }
    private void CheckAndAddBlockingOperator()
    {
        if (CurrentTile != null)
        {
            IDeployable tileDeployable = CurrentTile.OccupyingDeployable;

            if (tileDeployable is Operator op)
            {
                // 자신을 저지하는 오퍼레이터가 없음 and 현재 타일에 오퍼레이터가 있음 and 그 오퍼레이터가 저지 가능한 상태
                if (op != null && blockingOperator == null)
                {
                    blockingOperator = op;
                    blockingOperator.TryBlockEnemy(this); // 오퍼레이터에서도 저지 중인 Enemy를 추가
                }
            }
        }
    }

    /// <summary>
    /// 경로를 따라 이동.
    /// </summary>
    private void MoveAlongPath()
    {
        if (CheckIfReachedDestination())
        {
            ReachDestination();
            return;
        }

        Move(nextPosition);

        // 타일 갱신
        UpdateCurrentTile();

        // 노드 도달 확인
        if (Vector3.Distance(transform.position, nextPosition) < 0.05f)
        {
            // 목적지 도달
            if (nextPosition == destinationPosition)
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

    protected override void UpdateCurrentTile()
    {
        Vector3 position = transform.position;
        Tile newTile = MapManager.Instance.GetTileAtPosition(position);

        if (newTile != CurrentTile)
        {
            ExitTile();
            EnterNewTile(newTile);
        }
    }

    private IEnumerator RemoveFromPreviousTileDelay(Tile previousTile)
    {
        yield return new WaitForSeconds(0f);

        if (previousTile != null && previousTile != CurrentTile)
        {
            previousTile.EnemyExited(this);
        }
    }

    // 대기 중일 때 실행
    private IEnumerator WaitAtNode(float waitTime)
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;

        UpdateNextNode();
    }

    /// <summary>
    /// 다음 노드 인덱스를 설정하고 현재 목적지로 지정함
    /// </summary>
    private void UpdateNextNode()
    {
        currentNodeIndex++;
        if (currentNodeIndex < pathData.nodes.Count)
        {
            nextNode = pathData.nodes[currentNodeIndex];
            nextPosition = MapManager.Instance.ConvertToWorldPosition(nextNode.gridPosition) + Vector3.up * 0.5f;
        }
    }

    private void ReachDestination()
    {
        StageManager.Instance.OnEnemyReachDestination();
        Destroy(gameObject);
    }

    /// <summary>
    ///  공격 범위 내의 오퍼레이터 리스트를 업데이트한다
    /// </summary>
    public void UpdateTargetsInRange()
    {
        targetsInRange.Clear();
        Collider[] colliders = Physics.OverlapSphere(transform.position, AttackRange); // 콜라이더는 후보군을 추리는 역할을 하고, 실제 거리 계산은 따로 수행한다.(콜라이더가 거리값보다 더 크기 때문에)

        foreach (var collider in colliders)
        {
            DeployableUnitEntity target = collider.transform.parent?.GetComponent<DeployableUnitEntity>(); // GetComponent : 해당 오브젝트부터 시작, 부모 오브젝트로 올라가며 컴포넌트를 찾는다.
            if (target != null && target.IsDeployed && Faction.Ally == target.Faction)
            {
                // 실제 거리 계산
                float actualDistance = Vector3.Distance(transform.position, target.transform.position);
                if (actualDistance <= AttackRange)
                {
                    targetsInRange.Add(target);
                }
            }
        }
    }

    public void Attack(UnitEntity target, AttackType attackType, float damage)
    {
        PerformAttack(target, attackType, damage);
    }

    private void PerformAttack(UnitEntity target, AttackType attackType, float damage)
    {
        switch (AttackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target, attackType, damage);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target, attackType, damage);
                break;
        }
    }

    private void PerformMeleeAttack(UnitEntity target, AttackType attackType, float damage)
    {
        SetAttackTimings(); // 이걸 따로 호출하는 경우가 있어서 여기서 다시 설정
        target.TakeDamage(AttackType, damage, this, BaseData.hitEffectPrefab);
    }

    private void PerformRangedAttack(UnitEntity target, AttackType attackType, float damage)
    {
        SetAttackTimings();
        if (BaseData.projectilePrefab != null)
        {
            // 투사체 생성 위치
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
            GameObject projectileObj = ObjectPoolManager.Instance.SpawnFromPool(projectileTag, spawnPosition, Quaternion.identity);

            if (projectileObj != null)
            {
                Projectile projectile = projectileObj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(this, target, attackType, damage, false, projectileTag, BaseData.hitEffectPrefab);
                }
            }
        }
    }

    protected bool IsTargetInRange(UnitEntity target)
    {
        return Vector3.Distance(target.transform.position, transform.position) <= AttackRange;
    }

    private bool HasTargetInRange()
    {
        return targetsInRange.Count > 0;
    }

    protected override void Die()
    {
        // 저지 중인 오퍼레이터에게 저지를 해제시킴
        if (blockingOperator != null)
        {
            blockingOperator.UnblockEnemy(this);
        }

        // 공격 중인 개체의 현재 타겟 제거
        foreach (Operator op in attackingEntities.ToList())
        {
            op.OnTargetLost(this);
        }

        StageManager.Instance.OnEnemyDefeated(); // 사망한 적 수 +1

        // 공격 이펙트 프리팹 제거
        if (BaseData.hitEffectPrefab != null)
        {
            ObjectPoolManager.Instance.RemovePool("Effect_" + BaseData.entityName);
        }

        // UI 제거
        if (enemyBarUI != null)
        {
            Destroy(enemyBarUI.gameObject);
        }

        base.Die();
    }

    public override void TakeDamage(AttackType attackType, float damage, UnitEntity attacker = null, GameObject hitEffectPrefab = null)
    {
        float actualDamage = CalculateActualDamage(attackType, damage);
        CurrentHealth = Mathf.Max(0, CurrentHealth - actualDamage);

        // attacker가 null일 때에도 잘 동작합니다
        if (attacker is Operator op)
        {
            StatisticsManager.Instance.UpdateDamageDealt(op, actualDamage);
        }

        if (hitEffectPrefab != null)
        {
            PlayGetHitEffect(hitEffectPrefab);
        }

        if (CurrentHealth <= 0)
        {
            Die();
        }

        // UI 업데이트
        if (enemyBarUI != null)
        {
            enemyBarUI.UpdateUI();
        }
    }

    private void ExitTile()
    {
        if (CurrentTile != null)
        {
            CurrentTile.EnemyExited(this);
            CurrentTile = null;
        }
    }

    public void UnblockFrom(Operator op)
    {
        if (blockingOperator == op)
        {
            blockingOperator = null;
        }
    }

    private void EnterNewTile(Tile newTile)
    {
        CurrentTile = newTile;
        CurrentTile.EnemyEntered(this);
        CheckAndAddBlockingOperator();
    }

    // 마지막 타일의 월드 좌표 기준
    private bool CheckIfReachedDestination()
    {
        if (pathData.nodes.Count == 0) return false;

        Vector2Int lastNodeGridPos = pathData.nodes[pathData.nodes.Count - 1].gridPosition;
        Vector3 lastNodePosition = MapManager.Instance.ConvertToWorldPosition(lastNodeGridPos) + Vector3.up * 0.5f;

        return Vector3.Distance(transform.position, lastNodePosition) < 0.05f;
    }

    // 그리드 -> 월드 좌표 변환 후, 월드 좌표의 y좌표를 0.5로 넣음
    private Vector3 GetAdjustedNextPosition()
    {
        Vector3 adjustedPosition = nextPosition;
        adjustedPosition.y = 0.5f;
        return adjustedPosition;
    }

    /// <summary>
    /// Data, Stat이 엔티티마다 다르기 때문에 자식 메서드에서 재정의가 항상 필요
    /// </summary>
    protected override void InitializeUnitProperties()
    {
        // 현재 체력, 최대 체력 설정
        MaxHealth = currentStats.Health;
        CurrentHealth = MaxHealth;

        // 현재 위치를 기반으로 한 타일 설정
        UpdateCurrentTile();

        Prefab = BaseData.prefab;
    }

    /// <summary>
    /// Enemy가 공격할 대상 지정
    /// </summary>
    public void SetCurrentTarget()
    {
        // 저지를 당할 때는 자신을 저지하는 객체를 타겟으로 지정
        if (blockingOperator != null)
        {
            CurrentTarget = blockingOperator;
            NotifyTarget();
            return;
        }

        // 저지가 아니라면 공격 범위 내의 가장 마지막에 배치된 적 공격
        UpdateTargetsInRange(); // 공격 범위 내의 Operator 리스트 갱신

        if (targetsInRange.Count > 0)
        {
            // 다른 오브젝트를 공격해야 할 수도 있는데 지금은 일단 오퍼레이터에 한정해서 구현함
            CurrentTarget = targetsInRange
                .OfType<Operator>()
                .OrderByDescending(o => o.DeploymentOrder)
                .FirstOrDefault();

            NotifyTarget();
            return;
        }

        // 저지당하고 있지 않고, 공격 범위 내에도 타겟이 없다면 
        RemoveCurrentTarget();
    }

    public void RemoveCurrentTarget()
    {
        if (CurrentTarget != null)
        {
            CurrentTarget = null;
        }
    }

    /// <summary>
    /// CurrentTarget에게 자신이 공격하고 있음을 알림
    /// </summary>
    public void NotifyTarget()
    {
        CurrentTarget?.AddAttackingEntity(this);
    }

    public void InitializeProjectilePool()
    {
        projectileTag = $"{BaseData.entityName}_Projectile";
        ObjectPoolManager.Instance.CreatePool(projectileTag, BaseData.projectilePrefab, initialPoolSize);
    }

    private void CleanupProjectilePool()
    {
        if (!string.IsNullOrEmpty(projectileTag))
        {
            ObjectPoolManager.Instance.RemovePool(projectileTag);
        }
    }

    // 경로와 남은 거리 관련
    /// <summary>
    /// 현재 경로상에서 목적지까지 남은 거리 계산
    /// </summary>
    public float GetRemainingPathDistance()
    {
        if (currentPath == null || currentNodeIndex > +currentPath.Count)
        {
            return float.MaxValue;
        }

        float distance = 0f;
        for (int i = currentNodeIndex; i < currentPath.Count - 1; i++)
        {
            // 첫 타일에 한해서만 현재 위치를 기반으로 계산(여러 Enemy가 같은 타일에 있을 수 있기 때문)
            if (i == currentNodeIndex)
            {
                Vector3 nowPosition = new Vector3(transform.position.x, 0f, transform.position.z);
                distance += Vector3.Distance(nowPosition, currentPath[i + 1]);
            }

            distance += Vector3.Distance(currentPath[i], currentPath[i + 1]);
        }

        return distance;
    }


    /// <summary>
    /// Barricade 설치 시 현재 경로가 막혔다면 재계산
    /// </summary>
    private void OnBarricadePlaced(Barricade barricade)
    {
        // 내 타일과 같은 타일에 바리케이드가 배치된 경우
        if (barricade.CurrentTile.enemiesOnTile.Contains(this))
        {
            targetBarricade = barricade;
        }

        // 현재 사용 중인 경로가 막힌 경우
        else if (IsPathBlocked())
        {
            FindPathToDestinationOrBarricade();
        }
    }

    private void OnBarricadeRemovedWithDelay(Barricade barricade)
    {
        StartCoroutine(OnBarricadeRemoved(barricade));
    }

    /// <summary>
    /// 경로가 다시 열렸더라도 targetBarricade를 제거하는 건 불가능하게 설정
    /// </summary>
    private IEnumerator OnBarricadeRemoved(Barricade barricade)
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
            Debug.Log($"{gameObject.name} : TargetBarricade = barricade 조건문을 탔음");
            targetBarricade = null;
            FindPathToDestinationOrBarricade();
            Debug.Log($"{gameObject.name} : {targetBarricade}");
        }

        // 다른 바리케이드가 targetBarricade로 설정되어 있는 Enemy들은 그걸 파괴하러 이동하므로 별 동작 X
    }

    /// <summary>
    /// 현재 pathData를 사용하는 경로가 막혔는지를 점검한다
    /// 막혔다면 pathData, currentPath를 null로 만든다
    /// </summary>
    private bool IsPathBlocked()
    {
        for (int i= currentNodeIndex; i < currentPath.Count - 1; i++)
        {
            if (PathfindingManager.Instance.IsPathSegmentValid(currentPath[i], currentPath[i+1]) == false)
            {
                pathData = null;
                currentPath = null;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 현재 위치 -> 타겟 위치까지 향하는 경로를 체크
    /// </summary>
    private List<PathNode> CalculatePath(Vector3 currentPosition, Vector3 targetPosition)
    {
        return PathfindingManager.Instance.FindPathAsNodes(currentPosition, targetPosition);
    }

    /// <summary>
    /// targetBarricade 설정
    /// </summary>
    private void SetTargetBarricade()
    {
        targetBarricade = PathfindingManager.Instance.GetNearestBarricade(transform.position); 
    }

    /// <summary>
    /// CalculatePath로 탐색된 경로를 받아옴
    /// pathData, currentPath, currentNodeIndex 초기화
    /// </summary>
    private void SetNewPath(List<PathNode> newPathNodes)
    {
        if (newPathNodes != null && newPathNodes.Count > 0)
        {
            PathData newPathData = ScriptableObject.CreateInstance<PathData>();
            newPathData.nodes = newPathNodes;
            pathData = newPathData;
            currentPath = newPathNodes.Select(node => MapManager.Instance.ConvertToWorldPosition(node.gridPosition) + Vector3.up * 0.5f).ToList();

            currentNodeIndex = 0;

            UpdateNextNode();
        }
    }

    /// <summary>
    /// targetPosition으로 향하는 경로를 계산하고, 경로가 있다면 새로운 pathData와 currentPath로 설정함
    /// </summary>
    private bool CalculateAndSetPath(Vector3 currentPosition, Vector3 targetPosition)
    {
        List<PathNode> tempPathNodes = CalculatePath(currentPosition, targetPosition);

        if (tempPathNodes == null || tempPathNodes.Count == 0) return false; // 목적지로 향하는 경로가 없음

        SetNewPath(tempPathNodes);
        return true;
    }

    /// <summary>
    /// 현재 위치에서 가장 가까운 바리케이드를 설정하고, 바리케이드로 향하는 경로를 설정함
    /// </summary>

    private void SetBarricadePath()
    {
        SetTargetBarricade();
        if (targetBarricade != null)
        {
            CalculateAndSetPath(transform.position, targetBarricade.transform.position);
        }
    }

    /// <summary>
    /// 목적지로 향하는 경로를 찾고, 없다면 가장 가까운 바리케이드로 향하는 경로를 설정함
    /// </summary>
    private void FindPathToDestinationOrBarricade()
    {
        if (!CalculateAndSetPath(transform.position, destinationPosition))
        {
            SetBarricadePath();
        }
    }


    public void UpdateAttackTimings()
    {
        UpdateAttackDuration();
        UpdateAttackCooldown();
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

    public void SetAttackDuration()
    {
        AttackDuration = 0.3f / AttackSpeed;
    }

    public void SetAttackCooldown()
    {
        AttackCooldown = 1 / AttackSpeed;
    }

    public bool CanAttack()
    {
        return CurrentTarget != null &&
            AttackCooldown <= 0 &&
            AttackDuration <= 0;
    }

    private void CreateEnemyBarUI()
    {
        if (enemyBarUIPrefab != null)
        {
            GameObject uiObject = Instantiate(enemyBarUIPrefab, transform);
            enemyBarUI = uiObject.GetComponentInChildren<EnemyBarUI>();
            enemyBarUI.Initialize(this);
        }
    }
}
