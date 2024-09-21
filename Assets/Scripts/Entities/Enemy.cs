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
    public int BlockCount { get => enemyData.blockCount; private set => enemyData.blockCount = value; } // Enemy�� �����ϴ� ���� ��

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

    // ��� ����
    private PathData pathData;
    private int currentNodeIndex = 0;
    private List<Vector3> currentPath;
    private List<UnitEntity> targetsInRange = new List<UnitEntity>();

    private Operator blockingOperator; // �ڽ��� ���� ���� ���۷�����
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

        // ������ ��ΰ� �ִ�
        if (pathData != null && currentNodeIndex < pathData.nodes.Count)
        {
            // ���� ���� ���� �� ����Ʈ & ���� ���� ��� ����
            SetCurrentTarget(); 

            // 1. ���� ���� ���۷����Ͱ� ���� ��
            if (blockingOperator != null) 
            {
                // ���� ������ ���¶��
                if (CurrentTarget != null && AttackCooldown <= 0)
                {
                    PerformMeleeAttack(CurrentTarget, AttackType, AttackPower); // ������ ���ϴ� ���¶��, ���� ���� ������ ���� ���� �ٰŸ� ������ ��
                }
            }

            // 2. ���� ���� �ƴ϶��
            else 
            {
                if (CanAttack()) // Ÿ���� �ְ�, ������ ������ ����
                {
                    Attack(CurrentTarget, AttackType, AttackPower);
                }

                // �̵� ���� ����. ���� ���� �ƴ� ������ �����ؾ� �Ѵ�. 
                else if (!isWaiting) // ��� �̵� �� ��ٸ��� ��Ȳ�� �ƴ϶��
                {
                    MoveAlongPath(); // �̵�
                    CheckAndAddBlockingOperator(); // ���� Ÿ�Ͽ� �ִ� ���۷������� ���� ���� ���� üũ
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
                // �ڽ��� �����ϴ� ���۷����Ͱ� ���� and ���� Ÿ�Ͽ� ���۷����Ͱ� ���� and �� ���۷����Ͱ� ���� ������ ����
                if (op != null && op.CanBlockEnemy() && blockingOperator == null)
                {
                    blockingOperator = op;
                    blockingOperator.TryBlockEnemy(this); // ���۷����Ϳ����� ���� ���� Enemy�� �߰�
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

        // Ÿ�� ����
        Tile newTile = MapManager.Instance.GetTileAtPosition(transform.position);
        if (newTile != CurrentTile)
        {
            ExitCurrentTile();
            EnterNewTile(newTile);
        }

        // ��� ���� Ȯ��
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

    // ��� ���� �� ����
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
    ///  ���� ���� ���� ���۷����� ����Ʈ�� ������Ʈ�Ѵ�
    /// </summary>
    public void UpdateTargetsInRange()
    {
        targetsInRange.Clear();
        Collider[] colliders = Physics.OverlapSphere(transform.position, AttackRange); // �ݶ��̴��� �ĺ����� �߸��� ������ �ϰ�, ���� �Ÿ� ����� ���� �����Ѵ�.(�ݶ��̴��� �Ÿ������� �� ũ�� ������)

        foreach (var collider in colliders)
        {
            DeployableUnitEntity target = collider.transform.parent?.GetComponent<DeployableUnitEntity>(); // GetComponent : �ش� ������Ʈ���� ����, �θ� ������Ʈ�� �ö󰡸� ������Ʈ�� ã�´�.
            if (target != null && target.IsDeployed && Faction.Ally == target.Faction)
            { 
                // ���� �Ÿ� ���
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
            // ����ü ���� ��ġ
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
        // ���� ���� ���۷����Ϳ��� ������ ������Ŵ
        if (blockingOperator != null)
        {
            blockingOperator.UnblockEnemy(this);
        }

        // ���� ���� ��ü�� ���� Ÿ�� ����
        foreach (Operator op in attackingEntities.ToList())
        {
            op.OnTargetLost(this);
        }

        StageManager.Instance.OnEnemyDefeated(); // ����� �� �� +1

        // UI ����
        if (enemyUI != null)
        {
            Destroy(enemyUI.gameObject);
        }

        base.Die();
    }

    public override void TakeDamage(AttackType attackType, float damage)
    {
        base.TakeDamage(attackType, damage);

        // UI ������Ʈ
        if (enemyUI != null)
        {
            enemyUI.UpdateUI();
        }
    }

    /// <summary>
    /// �� ��ü�� �����ϴ� �� �߰�
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

    // ������ Ÿ���� ���� ��ǥ ����
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

    // �׸��� -> ���� ��ǥ ��ȯ ��, ���� ��ǥ�� y��ǥ�� 0.5�� ����
    private Vector3 GetAdjustedTargetPosition()
    {
        Vector3 adjustedPosition = targetPosition;
        adjustedPosition.y = 0.5f;
        return adjustedPosition;
    }

    // �ٸ�����Ʈ�� ��ġ�ǰų� ����� ������ ȣ��, ��θ� �����ؾ� �ϴ� ��� �����ϰų� ���� ��� ���� �ִ� Barricade �ı�
    private void OnBarricadeStateChanged(Barricade barricade)
    {
        // ���� ����� ��ȿ�� Ȯ��
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
    /// ���� ���� ����� �ְ�, ���� ��ٿ��� 0�� �ƴ� ��
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
    /// Data, Stat�� ��ƼƼ���� �ٸ��� ������ �ڽ� �޼��忡�� �����ǰ� �׻� �ʿ�
    /// </summary>
    protected override void InitializeUnitProperties()
    {
        // ���� ü��, �ִ� ü�� ����
        MaxHealth = currentStats.Health;
        CurrentHealth = MaxHealth;

        // ���� ��ġ�� ������� �� Ÿ�� ����
        UpdateCurrentTile();

        Prefab = Data.prefab;
    }

    /// <summary>
    /// Enemy�� ������ ��� ����
    /// </summary>
    public void SetCurrentTarget()
    {
        
        // ������ ���� ���� �ڽ��� �����ϴ� ��ü�� Ÿ������ ����
        if (blockingOperator != null)
        {
            CurrentTarget = blockingOperator;
            NotifyTarget();
            return; 
        }

        // ������ �ƴ϶�� ���� ���� ���� ���� �������� ��ġ�� �� ���� 
        UpdateTargetsInRange(); // ���� ���� ���� Operator ����Ʈ ����

        if (targetsInRange.Count > 0)
        {
            // �ٸ� ������Ʈ�� �����ؾ� �� ���� �ִµ� ������ �ϴ� ���۷����Ϳ� �����ؼ� ������
            // Linq
            CurrentTarget = targetsInRange
                .OfType<Operator>()
                .OrderByDescending(o => o.DeploymentOrder)
                .FirstOrDefault();

            NotifyTarget();
            return; 
        }

        // �������ϰ� ���� �ʰ�, ���� ���� ������ Ÿ���� ���ٸ� 
        CurrentTarget = null; 
    }

    public void DeleteCurrentTarget()
    {
        if (CurrentTarget == null) return;
        CurrentTarget.RemoveAttackingEntity(this);
        CurrentTarget = null;
    }

    /// <summary>
    /// CurrentTarget���� �ڽ��� �����ϰ� ������ �˸�
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

    // ��ο� ���� �Ÿ� ����
    /// <summary>
    /// ���� ��λ󿡼� ���������� ���� �Ÿ� ���
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
    /// Barricade ��ġ �� ���� ��ΰ� �����ٸ� ����
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
    /// ��ο� �ִ� ���(Ÿ��) �˻� �� ��� ���̸� �˻�
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

        // Ÿ�� ��� �˻� �߰�
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
        Vector3 targetPosition = currentPath[currentPath.Count - 1]; // ���� ������

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
            // �ٸ����̵�� �̵�
            Vector3 direction = (barricade.transform.position - transform.position).normalized;
            transform.position += direction * MovementSpeed * Time.deltaTime; 

            // ���� Ȯ��
            if (Vector3.Distance(transform.position, barricade.transform.position) <= 0.1f)
            {
                // ���� ���� ���� �ִٸ� ���� ����
                while (barricade != null && barricade.gameObject.activeInHierarchy)
                {
                    PerformMeleeAttack(barricade, AttackType, AttackPower);
                    yield return new WaitForSeconds(1f / AttackSpeed);
                }
                // �ı��ȴٸ� ��� ����
                RecalculatePath();
                yield break;
            }

            yield return null;
        }

        // �ٸ����̵尡 �߰��� �ı��� ��� ��� ����
        RecalculatePath();
    }

    /// <summary>
    /// Bresenham's Line Algorithm�� �̿�, ��� ���� ��� Ÿ�� ��ǥ�� ��ȯ�Ѵ�.
    /// </summary>
    private List<Vector2Int> GetTilesOnPath(Vector3 start, Vector3 end)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();

        // 3D => 2D �׸��� ��ǥ ��ȯ
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
