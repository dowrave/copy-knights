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


    public int BlockCount { get => enemyData.blockCount; private set => enemyData.blockCount = value; } // Enemy�� �����ϴ� ���� ��

    public float AttackCooldown { get; private set; } // ���� ���ݱ����� ��� �ð�
    public float AttackDuration { get; private set; } // ���� ��� �ð�. Animator�� �߰��� �� ���� �ʿ��� ��. �׻� Cooldown���� ª�ƾ� ��.

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
    private List<Vector3> currentPath = new List<Vector3>();
    private List<UnitEntity> targetsInRange = new List<UnitEntity>();
    private PathNode nextNode;
    private Vector3 nextPosition; // ���� ����� ��ǥ
    private Vector3 destinationPosition; // ������
    private bool isWaiting = false;
    private Barricade targetBarricade;

    private Operator blockingOperator; // �ڽ��� ���� ���� ���۷�����
    public bool IsBlocked { get { return blockingOperator != null; } }
    public UnitEntity CurrentTarget { get; private set; } // ���� �����!!

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

        // ���� ����Ʈ Ǯ ����
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

        // ���ʿ� ������ ��ΰ� ���� ��Ȳ�� �� ����
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
            // �������� ��ġ�� �����ص� ������µ� pathData�� �� ������ �־ �Žñ��ϴ�.
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

        // ������ ��ΰ� �ִ�
        if (pathData != null && currentNodeIndex < pathData.nodes.Count)
        {
            if (AttackDuration > 0) { return; } // ���� ��� ���� ���� �̵��� ����

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

            // 2. ���� ���� �ƴ� ��
            else
            {
                if (targetBarricade != null && Vector3.Distance(transform.position, targetBarricade.transform.position) < 0.5f)
                {
                    PerformMeleeAttack(targetBarricade, AttackType, AttackPower); // ������ �ٰŸ� ����
                }

                // Ÿ���� �ְ�, ������ ������ ����
                if (CanAttack())
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
        // Ÿ�Ͽ��� ����
        CurrentTile.EnemyExited(this);

        if (AttackRangeType == AttackRangeType.Ranged)
        {
            CleanupProjectilePool();
        }
    }

    /// <summary>
    /// pathData.nodes�� �̿��� currentPath �ʱ�ȭ
    /// </summary>
    private void InitializeCurrentPath()
    {
        foreach (var node in pathData.nodes)
        {
            currentPath.Add(MapManager.Instance.ConvertToWorldPosition(node.gridPosition) + Vector3.up * 0.5f);
        }
        destinationPosition = currentPath[currentPath.Count - 1]; // ������ ����
    }
    private void CheckAndAddBlockingOperator()
    {
        if (CurrentTile != null)
        {
            IDeployable tileDeployable = CurrentTile.OccupyingDeployable;

            if (tileDeployable is Operator op)
            {
                // �ڽ��� �����ϴ� ���۷����Ͱ� ���� and ���� Ÿ�Ͽ� ���۷����Ͱ� ���� and �� ���۷����Ͱ� ���� ������ ����
                if (op != null && blockingOperator == null)
                {
                    blockingOperator = op;
                    blockingOperator.TryBlockEnemy(this); // ���۷����Ϳ����� ���� ���� Enemy�� �߰�
                }
            }
        }
    }

    /// <summary>
    /// ��θ� ���� �̵�.
    /// </summary>
    private void MoveAlongPath()
    {
        if (CheckIfReachedDestination())
        {
            ReachDestination();
            return;
        }

        Move(nextPosition);

        // Ÿ�� ����
        UpdateCurrentTile();

        // ��� ���� Ȯ��
        if (Vector3.Distance(transform.position, nextPosition) < 0.05f)
        {
            // ������ ����
            if (nextPosition == destinationPosition)
            {
                ReachDestination();
            }
            // ��ٷ��� �ϴ� ���
            else if (nextNode.waitTime > 0)
            {
                StartCoroutine(WaitAtNode(nextNode.waitTime));
            }
            // ��� ������Ʈ
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

    // ��� ���� �� ����
    private IEnumerator WaitAtNode(float waitTime)
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;

        UpdateNextNode();
    }

    /// <summary>
    /// ���� ��� �ε����� �����ϰ� ���� �������� ������
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
        SetAttackTimings(); // �̰� ���� ȣ���ϴ� ��찡 �־ ���⼭ �ٽ� ����
        target.TakeDamage(AttackType, damage, this, BaseData.hitEffectPrefab);
    }

    private void PerformRangedAttack(UnitEntity target, AttackType attackType, float damage)
    {
        SetAttackTimings();
        if (BaseData.projectilePrefab != null)
        {
            // ����ü ���� ��ġ
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

        // ���� ����Ʈ ������ ����
        if (BaseData.hitEffectPrefab != null)
        {
            ObjectPoolManager.Instance.RemovePool("Effect_" + BaseData.entityName);
        }

        // UI ����
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

        // attacker�� null�� ������ �� �����մϴ�
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

        // UI ������Ʈ
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

    // ������ Ÿ���� ���� ��ǥ ����
    private bool CheckIfReachedDestination()
    {
        if (pathData.nodes.Count == 0) return false;

        Vector2Int lastNodeGridPos = pathData.nodes[pathData.nodes.Count - 1].gridPosition;
        Vector3 lastNodePosition = MapManager.Instance.ConvertToWorldPosition(lastNodeGridPos) + Vector3.up * 0.5f;

        return Vector3.Distance(transform.position, lastNodePosition) < 0.05f;
    }

    // �׸��� -> ���� ��ǥ ��ȯ ��, ���� ��ǥ�� y��ǥ�� 0.5�� ����
    private Vector3 GetAdjustedNextPosition()
    {
        Vector3 adjustedPosition = nextPosition;
        adjustedPosition.y = 0.5f;
        return adjustedPosition;
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

        Prefab = BaseData.prefab;
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
            CurrentTarget = targetsInRange
                .OfType<Operator>()
                .OrderByDescending(o => o.DeploymentOrder)
                .FirstOrDefault();

            NotifyTarget();
            return;
        }

        // �������ϰ� ���� �ʰ�, ���� ���� ������ Ÿ���� ���ٸ� 
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
    /// CurrentTarget���� �ڽ��� �����ϰ� ������ �˸�
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

    // ��ο� ���� �Ÿ� ����
    /// <summary>
    /// ���� ��λ󿡼� ���������� ���� �Ÿ� ���
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
            // ù Ÿ�Ͽ� ���ؼ��� ���� ��ġ�� ������� ���(���� Enemy�� ���� Ÿ�Ͽ� ���� �� �ֱ� ����)
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
    /// Barricade ��ġ �� ���� ��ΰ� �����ٸ� ����
    /// </summary>
    private void OnBarricadePlaced(Barricade barricade)
    {
        // �� Ÿ�ϰ� ���� Ÿ�Ͽ� �ٸ����̵尡 ��ġ�� ���
        if (barricade.CurrentTile.enemiesOnTile.Contains(this))
        {
            targetBarricade = barricade;
        }

        // ���� ��� ���� ��ΰ� ���� ���
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
    /// ��ΰ� �ٽ� ���ȴ��� targetBarricade�� �����ϴ� �� �Ұ����ϰ� ����
    /// </summary>
    private IEnumerator OnBarricadeRemoved(Barricade barricade)
    {
        // �ٸ����̵尡 �ı��� �ð� Ȯ��
        yield return new WaitForSeconds(0.1f); 
        
        // �ٸ����̵�� ���� ���� Enemy�� ��θ� �ٽ� Ž��
        if (targetBarricade == null)  
        {
            FindPathToDestinationOrBarricade();
        }

        // �ش� �ٸ����̵尡 ��ǥ���� Enemy���� targetBarricade ����
        else if (targetBarricade == barricade)
        {
            Debug.Log($"{gameObject.name} : TargetBarricade = barricade ���ǹ��� ����");
            targetBarricade = null;
            FindPathToDestinationOrBarricade();
            Debug.Log($"{gameObject.name} : {targetBarricade}");
        }

        // �ٸ� �ٸ����̵尡 targetBarricade�� �����Ǿ� �ִ� Enemy���� �װ� �ı��Ϸ� �̵��ϹǷ� �� ���� X
    }

    /// <summary>
    /// ���� pathData�� ����ϴ� ��ΰ� ���������� �����Ѵ�
    /// �����ٸ� pathData, currentPath�� null�� �����
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
    /// ���� ��ġ -> Ÿ�� ��ġ���� ���ϴ� ��θ� üũ
    /// </summary>
    private List<PathNode> CalculatePath(Vector3 currentPosition, Vector3 targetPosition)
    {
        return PathfindingManager.Instance.FindPathAsNodes(currentPosition, targetPosition);
    }

    /// <summary>
    /// targetBarricade ����
    /// </summary>
    private void SetTargetBarricade()
    {
        targetBarricade = PathfindingManager.Instance.GetNearestBarricade(transform.position); 
    }

    /// <summary>
    /// CalculatePath�� Ž���� ��θ� �޾ƿ�
    /// pathData, currentPath, currentNodeIndex �ʱ�ȭ
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
    /// targetPosition���� ���ϴ� ��θ� ����ϰ�, ��ΰ� �ִٸ� ���ο� pathData�� currentPath�� ������
    /// </summary>
    private bool CalculateAndSetPath(Vector3 currentPosition, Vector3 targetPosition)
    {
        List<PathNode> tempPathNodes = CalculatePath(currentPosition, targetPosition);

        if (tempPathNodes == null || tempPathNodes.Count == 0) return false; // �������� ���ϴ� ��ΰ� ����

        SetNewPath(tempPathNodes);
        return true;
    }

    /// <summary>
    /// ���� ��ġ���� ���� ����� �ٸ����̵带 �����ϰ�, �ٸ����̵�� ���ϴ� ��θ� ������
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
    /// �������� ���ϴ� ��θ� ã��, ���ٸ� ���� ����� �ٸ����̵�� ���ϴ� ��θ� ������
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

    // �������̽� ������ ����
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


    // ���� ��� �ð�, ���� ��Ÿ�� �ð� ����
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
