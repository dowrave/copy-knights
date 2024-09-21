using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemy : UnitEntity, IMovable, ICombatEntity
{
    [SerializeField]
    private EnemyData enemyData;
    public new EnemyData Data => enemyData;

    private EnemyStats currentStats;

    public AttackType AttackType => enemyData.attackType;
    public AttackRangeType AttackRangeType => enemyData.attackRangeType;
    public float AttackPower { get => currentStats.AttackPower; private set => currentStats.AttackPower = value; }
    public float AttackSpeed { get => currentStats.AttackSpeed; private set => currentStats.AttackSpeed = value; }
    public float MovementSpeed { get => currentStats.MovementSpeed; private set => currentStats.MovementSpeed = value; } 
    public int BlockCount { get => enemyData.blockCount; private set => enemyData.blockCount = value; } // Enemy가 차지하는 저지 수

    public float AttackCooldown { get; private set; }

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
    private List<Vector3> currentPath;
    private List<UnitEntity> targetsInRange = new List<UnitEntity>();

    private Operator blockingOperator; // 자신을 저지 중인 오퍼레이터
    public UnitEntity CurrentTarget { get; private set; }

    private EnemyUI enemyUI;

    private PathNode targetNode;
    private Vector3 targetPosition;
    private bool isWaiting = false;

    [SerializeField] protected int initialPoolSize = 5;
    protected string projectileTag;

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
        

    }

    private void OnEnable()
    {
        Barricade.OnBarricadeDeployed += OnBarricadeStateChanged;
        Barricade.OnBarricadeRemoved += OnBarricadeStateChanged;
    }

    private void OnDisable()
    {
        Barricade.OnBarricadeDeployed -= OnBarricadeStateChanged;
        Barricade.OnBarricadeRemoved -= OnBarricadeStateChanged;
    }

    private void InitializeEnemyProperties()
    {
        SetupInitialPosition();
        CreateEnemyUI();
        UpdateTargetNode();

        if (AttackRangeType == AttackRangeType.Ranged)
        {
            InitializeProjectilePool();
        }
    }

    private void SetupInitialPosition()
    {
        if (pathData != null && pathData.nodes.Count > 0)
        {
            transform.position = MapManager.Instance.GetWorldPosition(pathData.nodes[0].gridPosition) + Vector3.up * 0.5f;
        }
        else
        {
            Debug.LogWarning("PathData is not set or empty. Initial position not set.");
        }
    }

    private void Update()
    {
        UpdateAttackCooldown();

        // 진행할 경로가 있다
        if (pathData != null && currentNodeIndex < pathData.nodes.Count)
        {
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

            // 2. 저지 중이 아니라면
            else 
            {
                if (CanAttack()) // 타겟이 있고, 공격이 가능한 상태
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
        if (AttackRangeType == AttackRangeType.Ranged)
        {
            CleanupProjectilePool();
        }
    }

    private void CreateEnemyUI()
    {
        enemyUI = GetComponentInChildren<EnemyUI>();
        if (enemyUI != null)
        {
            enemyUI.Initialize(this);
        }
    }

    private void CheckAndAddBlockingOperator()
    {
        Tile currentTile = FindCurrentTile();

        if (currentTile != null)
        {
            IDeployable tileDeployable = currentTile.OccupyingDeployable;
            if (tileDeployable is Operator op)
            {
                // 자신을 저지하는 오퍼레이터가 없음 and 현재 타일에 오퍼레이터가 있음 and 그 오퍼레이터가 저지 가능한 상태
                if (op != null && op.CanBlockEnemy() && blockingOperator == null)
                {
                    blockingOperator = op;
                    blockingOperator.TryBlockEnemy(this); // 오퍼레이터에서도 저지 중인 Enemy를 추가
                }

            }
        }
    }

    private Tile FindCurrentTile()
    {
        foreach (Tile tile in MapManager.Instance.GetAllTiles())
        {
            if (tile.IsEnemyOnTile(this))
            {
                return tile;
            }
        }
        return null; 
    }

    // 
    private void MoveAlongPath()
    {
        if (CheckIfReachedDestination())
        {
            ReachDestination();
            return;
        }

        Move(targetPosition);

        // 타일 갱신
        Tile newTile = MapManager.Instance.GetTileAtPosition(transform.position);
        if (newTile != CurrentTile)
        {
            ExitCurrentTile();
            EnterNewTile(newTile);
        }

        // 노드 도달 확인
        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            if (targetNode.waitTime > 0)
            {
                StartCoroutine(WaitAtNode(targetNode.waitTime));
            }
            else
            {
                MoveToNextNode();
            }
        }
    }

    public void Move(Vector3 destination)
    {
        transform.position = Vector3.MoveTowards(transform.position, destination, MovementSpeed * Time.deltaTime);
    }

    // 대기 중일 때 실행
    private IEnumerator WaitAtNode(float waitTime)
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;
        MoveToNextNode();
    }

    private void MoveToNextNode()
    {
        //currentNodeIndex++;
        UpdateTargetNode();

        if (CheckIfReachedDestination())
        {
            ReachDestination();
        }
    }

    private void UpdateTargetNode()
    {
        currentNodeIndex++;
        if (currentNodeIndex < pathData.nodes.Count)
        {
            targetNode = pathData.nodes[currentNodeIndex];
            targetPosition = MapManager.Instance.GetWorldPosition(targetNode.gridPosition) + Vector3.up * 0.5f;
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
        target.TakeDamage(AttackType, damage);
        SetAttackCooldown();
    }

    private void PerformRangedAttack(UnitEntity target, AttackType attackType, float damage)
    {
        if (Data.projectilePrefab != null)
        {
            // 투사체 생성 위치
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
            GameObject projectileObj = ObjectPoolManager.Instance.SpawnFromPool(projectileTag, spawnPosition, Quaternion.identity);

            if (projectileObj != null)
            {
                Projectile projectile = projectileObj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(target, attackType, damage, projectileTag);
                }
                SetAttackCooldown();
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

        // UI 제거
        if (enemyUI != null)
        {
            Destroy(enemyUI.gameObject);
        }

        base.Die();
    }

    public override void TakeDamage(AttackType attackType, float damage)
    {
        base.TakeDamage(attackType, damage);

        // UI 업데이트
        if (enemyUI != null)
        {
            enemyUI.UpdateUI();
        }
    }

    /// <summary>
    /// 이 개체를 공격하는 적 추가
    /// </summary>
    public override void AddAttackingEntity(ICombatEntity combatEntity)
    {
        base.AddAttackingEntity(combatEntity);
    }

    public override void RemoveAttackingEntity(ICombatEntity combatEntity)
    {
        base.RemoveAttackingEntity(combatEntity);
    }

    private void ExitCurrentTile()
    {
        if (CurrentTile != null)
        {
            CurrentTile.EnemyExited(this);
            CurrentTile = null;
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
        int lastNodeIndex = pathData.nodes.Count - 1;
        Vector3 lastNodePosition = MapManager.Instance.GetWorldPosition(pathData.nodes[lastNodeIndex].gridPosition) + Vector3.up * 0.5f;
        if (Vector3.Distance(transform.position, lastNodePosition) < 0.05f)
        {
            return true;
        }
        return false; 
    }

    // 그리드 -> 월드 좌표 변환 후, 월드 좌표의 y좌표를 0.5로 넣음
    private Vector3 GetAdjustedTargetPosition()
    {
        Vector3 adjustedPosition = targetPosition;
        adjustedPosition.y = 0.5f;
        return adjustedPosition;
    }

    // 바리케이트가 배치되거나 사라질 때마다 호출, 경로를 수정해야 하는 경우 수정하거나 기존 경로 위에 있는 Barricade 파괴
    private void OnBarricadeStateChanged(Barricade barricade)
    {
        // 현재 경로의 유효성 확인
        if (!IsCurrentPathValid())
        {
            RecalculatePath();
        }
    }

    private bool IsCurrentPathValid()
    {
        if (currentNodeIndex < pathData.nodes.Count)
        {
            Vector2Int nextGridPos = pathData.nodes[currentNodeIndex].gridPosition;
            Tile nextTile = MapManager.Instance.GetTile(nextGridPos.x, nextGridPos.y);
            return nextTile != null && nextTile.data.isWalkable;
        }
        return false;
    }

    public void UpdateAttackCooldown()
    {
        if (AttackCooldown > 0f)
        {
            AttackCooldown -= Time.deltaTime;
        }
    }

    /// <summary>
    /// 현재 공격 대상이 있고, 공격 쿨다운이 0이 아닐 때
    /// </summary>
    public bool CanAttack()
    {
        return CurrentTarget != null && AttackCooldown <= 0;
    }

    public void SetAttackCooldown()
    {
        AttackCooldown = 1 / AttackSpeed;
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

        Prefab = Data.prefab;
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
            // Linq
            CurrentTarget = targetsInRange
                .OfType<Operator>()
                .OrderByDescending(o => o.DeploymentOrder)
                .FirstOrDefault();

            NotifyTarget();
            return; 
        }

        // 저지당하고 있지 않고, 공격 범위 내에도 타겟이 없다면 
        CurrentTarget = null; 
    }

    public void DeleteCurrentTarget()
    {
        if (CurrentTarget == null) return;
        CurrentTarget.RemoveAttackingEntity(this);
        CurrentTarget = null;
    }

    /// <summary>
    /// CurrentTarget에게 자신이 공격하고 있음을 알림
    /// </summary>
    public void NotifyTarget()
    {
        CurrentTarget.AddAttackingEntity(this);
    }

    public void InitializeProjectilePool()
    {
        projectileTag = $"{Data.entityName}_Projectile";
        ObjectPoolManager.Instance.CreatePool(projectileTag, Data.projectilePrefab, initialPoolSize);
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
        if (currentPath == null || currentNodeIndex >+ currentPath.Count)
        {
            return float.MaxValue;
        }

        float distance = 0f; 
        for (int i = currentNodeIndex; i < currentPath.Count - 1; i++) 
        {
            distance += Vector3.Distance(currentPath[i], currentPath[i + 1]);
        }

        return distance; 
    }

    public void UpdatePath(List<Vector3> newPath)
    {
        currentPath = newPath;
        currentNodeIndex = 0;
    }

    /// <summary>
    /// Barricade 설치 시 현재 경로가 막혔다면 재계산
    /// </summary>

    public void OnBarricadePlaced(Vector3 barricadePosition)
    {
        if (IsPathBlocked())
        {
            RecalculatePath();
        }
    }

    private bool IsPathBlocked()
    {
        for (int i= currentNodeIndex; i < currentPath.Count - 1; i++)
        {
            if (!IsPathSegmentValid(currentPath[i], currentPath[i+1]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 경로에 있는 노드(타일) 검사 및 노드 사이를 검사
    /// </summary>
    private bool IsPathSegmentValid(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        RaycastHit hit;

        if (Physics.Raycast(start, direction.normalized, out hit, distance, LayerMask.GetMask("Barricade", "Obstacle")))
        {
            return false;
        }

        // 타일 기반 검사 추가
        List<Vector2Int> tilesOnPath = GetTilesOnPath(start, end);
        foreach (Vector2Int tilePos in tilesOnPath)
        {
            if (!IsTileWalkable(tilePos))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsTileWalkable(Vector2Int tilePos)
    {
        return MapManager.Instance.GetTile(tilePos.x, tilePos.y).IsWalkable;
    }

    private void RecalculatePath()
    {
        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = currentPath[currentPath.Count - 1]; // 완전 목적지

        List<Vector3> newPath = PathFindingManager.Instance.FindPath(currentPosition, targetPosition);

        if (newPath != null && newPath.Count > 0)
        {
            currentPath = newPath;
            currentNodeIndex = 0;
        }
        else
        {
            AttackNearestBarricade();
        }
    }

    private void AttackNearestBarricade()
    {
        Barricade nearestBarricade = FindNearestBarricade();
        if (nearestBarricade != null)
        {
            CurrentTarget = nearestBarricade;
            StartCoroutine(MoveToBarricade(nearestBarricade));
        }
    }

    private Barricade FindNearestBarricade()
    {
        Barricade[] allBarricades = FindObjectsOfType<Barricade>();
        return allBarricades.OrderBy(b => Vector3.Distance(transform.position, b.transform.position)).FirstOrDefault();
    }

    private IEnumerator MoveToBarricade(Barricade barricade)
    {
        while (barricade != null && barricade.gameObject.activeInHierarchy)
        {
            // 바리케이드로 이동
            Vector3 direction = (barricade.transform.position - transform.position).normalized;
            transform.position += direction * MovementSpeed * Time.deltaTime; 

            // 도달 확인
            if (Vector3.Distance(transform.position, barricade.transform.position) <= 0.1f)
            {
                // 공격 범위 내에 있다면 공격 시작
                while (barricade != null && barricade.gameObject.activeInHierarchy)
                {
                    PerformMeleeAttack(barricade, AttackType, AttackPower);
                    yield return new WaitForSeconds(1f / AttackSpeed);
                }
                // 파괴된다면 경로 재계산
                RecalculatePath();
                yield break;
            }

            yield return null;
        }

        // 바리케이드가 중간에 파괴된 경우 경로 재계산
        RecalculatePath();
    }

    /// <summary>
    /// Bresenham's Line Algorithm을 이용, 경로 상의 모든 타일 좌표를 반환한다.
    /// </summary>
    private List<Vector2Int> GetTilesOnPath(Vector3 start, Vector3 end)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        // 3D => 2D 그리드 좌표 변환
        Vector2Int startTile = MapManager.Instance.GetGridPosition(start);
        Vector2Int endTile = MapManager.Instance.GetGridPosition(end);

        int x0 = startTile.x;
        int y0 = startTile.y;
        int x1 = endTile.x;
        int y1 = endTile.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            tiles.Add(new Vector2Int(x0, y0));

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }

        return tiles;

    }
}
