using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemy : UnitEntity, IMovable, ICombatEntity
{

    [SerializeField] // �ʵ� ����ȭ, Inspector���� �� �ʵ� �����
    private EnemyData data;
    public new EnemyData Data => data;
    private EnemyStats currentStats;


    public AttackType AttackType => data.attackType;
    public AttackRangeType AttackRangeType => data.attackRangeType;
    public float AttackPower { get => currentStats.attackPower; private set => currentStats.attackPower = value; }
    public float AttackSpeed { get => currentStats.attackSpeed; private set => currentStats.attackSpeed = value; }
    public float MovementSpeed { get => currentStats.movementSpeed; private set => currentStats.movementSpeed = value; } 
    public int BlockCount { get => data.blockCount; private set => data.blockCount = value; } // Enemy�� �����ϴ� ���� ��

    public float AttackCooldown { get; private set; }

    public float AttackRange
    {
        get
        {
            return data.attackRangeType == AttackRangeType.Melee ? 0f : currentStats.attackRange;
        }
        private set
        {
            currentStats.attackRange = value;
        }
    }

    // ��� ����
    private PathData pathData;
    private int currentNodeIndex = 0;
    private List<Operator> operatorsInRange = new List<Operator>();

    private Operator blockingOperator; // �ڽ��� ���� ���� ���۷�����
    private UnitEntity currentTarget;

    private EnemyUI enemyUI;

    private PathNode targetNode;
    private Vector3 targetPosition;
    private bool isWaiting = false;

    public override void Initialize(UnitData unitData)
    {
        Initialize(unitData, null);
    }

    public void Initialize(UnitData unitData, PathData pathData)
    {
        base.Initialize(unitData); // EnemyData�� �ʱ�ȭ��
        this.pathData = pathData;

        InitializeEnemyProperties();
        SetupInitialPosition();
        CreateEnemyUI();
        UpdateTargetNode();
        // stats�� ����ϴ� ������ ���Ŀ� �߰�(?)
    }

    protected override void InitializeData(UnitData unitData)
    {
        if (unitData is EnemyData enemyData)
        {
            data = enemyData;
            currentStats = data.stats;
        }
        else
        {
            Debug.LogError("���� �����Ͱ� EnemyData�� �ƴ�!");
        }
    }

    private void InitializeEnemyProperties()
    {
        
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
            // 1. ���� ���� ���۷����Ͱ� ���� ��
            if (blockingOperator != null) 
            {
                // ���� ������ ���¶��
                if (AttackCooldown <= 0)
                {
                    PerformMeleeAttack(blockingOperator); // ������ ���ϴ� ���¶��, ���� ���� ������ ���� ���� �ٰŸ� ������ ��
                    
                }
            }

            // 2. ���� ���� �ƴ϶��
            else 
            {
                UpdateOperatorsInRange();

                if (HasTargetInRange()) // ���� ���� ���� ���� �ִٸ�
                {
                    Debug.Log("���� ���� ���� ���� ����");
                    if (AttackCooldown <= 0) // ���� ��Ÿ���� �ƴ϶��
                    {
                        AttackLastDeployedOperator(); // ���� �������� ��ġ�� ���۷����� ����
                    }
                }

                // �̵� ���� ����. ���� ���� �ƴ� ������ �����ؾ� �Ѵ�. 
                if (!isWaiting) // ��� �̵� �� ��ٸ��� ��Ȳ�� �ƴ϶��
                {
                    MoveAlongPath(); // �̵�
                    CheckAndAddBlockingOperator(); // ���� Ÿ�Ͽ� �ִ� ���۷������� ���� ���� ���� üũ
                }
            }
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

    /// <summary>
    ///  ���� ���� ���� ���۷����� ����Ʈ�� ������Ʈ�Ѵ�
    /// </summary>
    public void UpdateOperatorsInRange()
    {
        operatorsInRange.Clear();
        Collider[] colliders = Physics.OverlapSphere(transform.position, AttackRange);

        foreach (var collider in colliders)
        {
            Operator op = collider.transform.parent?.GetComponent<Operator>(); // GetComponent : �ش� ������Ʈ���� ����, �θ� ������Ʈ�� �ö󰡸� ������Ʈ�� ã�´�.

            if (op != null && op.IsDeployed)
            {
                // �ݶ��̴��� ���� '��⸸ �ϸ�' ������ �ǹǷ�, ���� AttackRange���� ���� ������ �� Ŭ �� ����
                // �׷��� �ݶ��̴��� �ĺ����� �߸��� ������ �ϰ�, ���� �Ÿ� ����� ���� �����Ѵ�.

                // ���� �Ÿ� ���
                float actualDistance = Vector3.Distance(transform.position, op.transform.position);
                if (actualDistance <= AttackRange)
                {
                    operatorsInRange.Add(op);
                }
            }
        }
    }

    public void Attack(UnitEntity target, AttackType attackType, float damage)
    {
        PerformAttack(target);    
    }

    private void PerformAttack(UnitEntity target)
    {
        switch (AttackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target);
                break;
        }
    }

    private void PerformMeleeAttack(UnitEntity target)
    {
        float damage = AttackPower; 
        target.TakeDamage(AttackType, damage);
    }

    private void PerformRangedAttack(UnitEntity target)
    {
        if (data.projectilePrefab != null)
        {
            float damage = currentStats.attackPower;

            // ����ü ���� ��ġ
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;

            GameObject projectileObj = Instantiate(data.projectilePrefab, spawnPosition, Quaternion.identity);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(target, AttackType, damage);
            }
        }
    }

    protected bool IsTargetInRange(UnitEntity target)
    {
        return Vector3.Distance(target.transform.position, transform.position) <= AttackRange;
    }

    private bool HasTargetInRange()
    {
        return operatorsInRange.Count > 0;
    }

    protected override void Die()
    {
        Debug.LogWarning("�� ���");

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
        if (combatEntity is UnitEntity unitEntity)
        {
            Debug.Log($"Enemy�� �����ϴ� Operator �߰� : {unitEntity.Name}");
        }

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

    // ���� �������� ��ġ�� ���۷����͸� ������
    private void AttackLastDeployedOperator()
    {
        if (operatorsInRange.Count > 0 && AttackCooldown <= 0)
        {
            Operator target = operatorsInRange.OrderByDescending(o => o.deploymentOrder).First();
            Attack(target, AttackType, AttackPower);
        }
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

    private void RecalculatePath()
    {
        Vector3 currentPosition = transform.position;
        Vector3 endPosition = MapManager.Instance.GetEndPoint();
        pathData = PathFindingManager.Instance.FindPath(currentPosition, endPosition);
        currentNodeIndex = 0;
    }

    public void UpdateAttackCooldown()
    {
        if (AttackCooldown > 0f)
        {
            AttackCooldown -= Time.deltaTime;
        }
    }

    // ICombatEntity �޼��� - �������̽� ����� ��� public���� �����ؾ� ��
    public bool CanAttack()
    {
        return currentTarget != null && AttackCooldown <= 0;
    }

    public void SetAttackCooldown()
    {
        AttackCooldown = 1 / AttackSpeed;
    }
}
