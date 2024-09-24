using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

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
    private PathNode nextNode;
    private Vector3 nextPosition; // 다음 노드의 좌표
    private Vector3 destinationPosition; // 목적지
    private bool isWaiting = false;
    private Barricade targetBarricade;

    private Operator blockingOperator; // 자신을 저지 중인 오퍼레이터
    public UnitEntity CurrentTarget { get; private set; } // 공격 대상임!!

    private EnemyUI enemyUI;

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
        Barricade.OnBarricadeDeployed += OnBarricadePlaced;
        Barricade.OnBarricadeRemoved += OnBarricadePlaced;
    }

    private void OnDisable()
    {
        Barricade.OnBarricadeDeployed -= OnBarricadePlaced;
        Barricade.OnBarricadeRemoved -= OnBarricadePlaced;
    }

    private void InitializeEnemyProperties()
    {
        SetupInitialPosition();
        CreateEnemyUI();
        UpdateTargetNode();
        InitializeCurrentPath();

        // 최초에 설정한 경로가 막힌 상황일 때 동작
        if (PathfindingManager.Instance.IsBarricadeDeployed && IsPathBlocked())
        {
            RecalculatePath(transform.position, destinationPosition);
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

            // 2. 저지 중이 아닐 때
            else 
            {
                // 길이 막힌 상태 & targetBarricade에 근접했을 때
                if (targetBarricade != null && Vector3.Distance(transform.position, targetBarricade.transform.position) < 0.1f)
                {
                    PerformMeleeAttack(targetBarricade, AttackType, AttackPower); // 무조건 근거리 공격
                }

                // 타겟이 있고, 공격이 가능한 상태
                else if (CanAttack()) 
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

    /// <summary>
    /// pathData.nodes를 이용해 currentPath 초기화
    /// </summary>
    private void InitializeCurrentPath()
    {
        currentPath = new List<Vector3>();
        foreach (var node in pathData.nodes)
        {
            currentPath.Add(MapManager.Instance.ConvertToWorldPosition(node.gridPosition) + Vector3.up * 0.5f);
        }
        destinationPosition = currentPath[currentPath.Count - 1]; // 목적지 설정
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

  
    private void MoveAlongPath()
    {
        if (CheckIfReachedDestination())
        {
            ReachDestination();
            return;
        }

        Move(nextPosition);

        // 타일 갱신
        Tile newTile = MapManager.Instance.GetTileAtPosition(transform.position);
        if (newTile != CurrentTile)
        {
            ExitCurrentTile();
            EnterNewTile(newTile);
        }

        // 노드 도달 확인
        if (Vector3.Distance(transform.position, nextPosition) < 0.05f)
        {
            if (nextNode.waitTime > 0)
            {
                StartCoroutine(WaitAtNode(nextNode.waitTime));
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
        UpdateTargetNode();

        if (CheckIfReachedDestination())
        {
            ReachDestination();
        }
    }

    /// <summary>
    /// 다음 노드 인덱스를 설정하고 현재 목적지로 지정함
    /// </summary>
    private void UpdateTargetNode()
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

    // 인터페이스 때문에 구현
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
        CurrentTarget?.AddAttackingEntity(this);
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


    /// <summary>
    /// Barricade 설치 시 현재 경로가 막혔다면 재계산
    /// </summary>
    public void OnBarricadePlaced(Barricade barricade)
    {
        if (IsPathBlocked())
        {
            RecalculatePath(transform.position, destinationPosition);
        }
    }

    private bool IsPathBlocked()
    {
        for (int i= currentNodeIndex; i < currentPath.Count - 1; i++)
        {
            if (IsPathSegmentValid(currentPath[i], currentPath[i+1]) == false)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 경로의 노드 사이가 막혔는지를 검사
    /// </summary>
    private bool IsPathSegmentValid(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        float distance = direction.magnitude;
        RaycastHit hit;

        if (Physics.Raycast(start, direction.normalized, out hit, distance, LayerMask.GetMask("Deployable")))
        {
            // 레이캐스트 위치의 타일 확인
            Vector2Int tilePos = MapManager.Instance.ConvertToGridPosition(hit.point);
            Tile tile = MapManager.Instance.GetTile(tilePos.x, tilePos.y);

            if (tile != null && tile.IsWalkable == false)
            {
                return false;
            }
        }

        // 타일 기반 검사 추가
        List<Vector2Int> tilesOnPath = GetTilesOnPath(start, end);
        foreach (Vector2Int tilePos in tilesOnPath)
        {
            if (IsTileWalkable(tilePos) == false)
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

    /// <summary>
    /// 기존 경로가 막힌 상황이라고 가정.
    /// </summary>
    private void RecalculatePath(Vector3 currentPosition, Vector3 targetPosition)
    {
        SetNewPathData(currentPosition, targetPosition);

        // 목적지로 향하는 경로가 없다
        if (currentPath == null)
        {
            Debug.LogError($"{targetPosition}으로 향하는 경로가 없음!!");
        }

        SetTargetBarricade();

    }

    /// <summary>
    /// targetBarricade 설정
    /// </summary>
    private void SetTargetBarricade()
    {
        targetBarricade = PathfindingManager.Instance.GetNearestBarricade(transform.position); // 가장 가까운 바리케이드 설정
        SetNewPathData(transform.position, targetBarricade.transform.position); // 새로 경로 설정
    }

    /// <summary>
    /// 현위치에서 targetPosition까지의 경로 데이터 설정, currentPath 초기화 등등
    /// 경로 관련 수치들을 다시 초기화함
    /// </summary>
    private void SetNewPathData(Vector3 currentPosition, Vector3 targetPosition)
    {
        List<PathNode> newPathNodes = PathfindingManager.Instance.FindPathAsNodes(currentPosition, targetPosition);

        if (newPathNodes != null && newPathNodes.Count > 0)
        {
            pathData = new PathData { nodes = newPathNodes };
            currentPath = newPathNodes.Select(node => MapManager.Instance.ConvertToWorldPosition(node.gridPosition) + Vector3.up * 0.5f).ToList();
            currentNodeIndex = 0;
            UpdateTargetNode();
        }
    }

    /// <summary>
    /// Bresenham's Line Algorithm을 이용, 경로 상의 모든 타일 좌표를 반환한다.
    /// </summary>
    private List<Vector2Int> GetTilesOnPath(Vector3 start, Vector3 end)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        // 3D => 2D 그리드 좌표 변환
        Vector2Int startTile = MapManager.Instance.ConvertToGridPosition(start);
        Vector2Int endTile = MapManager.Instance.ConvertToGridPosition(end);

        int x0 = startTile.x;
        int y0 = startTile.y;
        int x1 = endTile.x;
        int y1 = endTile.y;

        // 방향으로의 거리
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);

        // 이동 방향
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;

        // 오차 누적 변수
        int err = dx - dy;

        // 시작점 ~ 끝점까지 이동 
        while (true)
        {

            // 각 단계에서 현위치를 tiles 리스트에 추가
            tiles.Add(new Vector2Int(x0, y0)); 

            // 현재 위치 = 끝점이면 종료
            if (x0 == x1 && y0 == y1) break;

            // 오차를 사용해 방향 결정
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
