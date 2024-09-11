using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemy : UnitEntity, IMovable, ICombatEntity
{

    [SerializeField] // �ʵ� ����ȭ, Inspector���� �� �ʵ� �����
    private EnemyData data;
    private EnemyStats currentStats;

    // ICombatEntity �ʵ�
    public AttackType AttackType => data.attackType;
    public AttackRangeType AttackRangeType => data.attackRangeType;
    public float AttackPower { get => currentStats.attackPower; private set => currentStats.attackPower = value; }
    public float AttackSpeed { get => currentStats.attackSpeed; private set => currentStats.attackSpeed = value; }
    public float MovementSpeed { get => currentStats.movementSpeed; private set => currentStats.movementSpeed = value; } 
    public int BlockCount { get => data.blockCount; private set => data.blockCount = value; }

    public float AttackCooldown { get; private set; }

    // ��� ����
    private PathData pathData;
    private int currentNodeIndex = 0;

    // ���� ���� - �ٰŸ��� 0, ���Ÿ��� Data�� ���� ����ŭ
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
    private List<Operator> operatorsInRange = new List<Operator>();

    private Operator blockingOperator; // �ڽ��� ���� ���� ���۷�����
    private List<Operator> attackingOperators = new List<Operator>(); // �ڽ��� ���� ���� ���۷�����

    private EnemyUI enemyUI;




    public float DamageMultiplier { get; private set; }
    public float DefenseMultiplier { get; private set; }

    private Tile currentTile;
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

    // PathData ��� ����
    private void MoveAlongPath()
    {
        if (CheckIfReachedDestination())
        {
            ReachDestination();
            return;
        }

        // �� ��ġ ���(���� �̵� X)
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, MovementSpeed * Time.deltaTime);

        // ���� �̵�
        transform.position = newPosition;

        // Ÿ�� ����
        Tile newTile = MapManager.Instance.GetTileAtPosition(newPosition);
        if (newTile != currentTile)
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

    public override void Attack(UnitEntity target)
    {
        if (target is not Operator op) return;

        base.Attack(target); // ���� ������ ��, PerformAttack�� �����Ű�� ��ٿ��� ����
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
            float damage = currentStats.attackPower * DamageMultiplier;

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


    protected override bool IsTargetInRange(UnitEntity target)
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

        foreach (Operator op in attackingOperators.ToList())
        {
            op.OnTargetLost(this);
        }

        StageManager.Instance.OnEnemyDefeated(); // ����� �� �� +1

        if (enemyUI != null)
        {
            Destroy(enemyUI.gameObject);
        }

        base.Die();
    }

    public override void TakeDamage(float damage)
    {
        //float actualDamage = damage / (Stats.Defense * DefenseMultiplier);
        base.TakeDamage(damage);

        if (enemyUI != null)
        {
            enemyUI.UpdateUI();
        }
    }

    public void AddAttackingOperator(Operator op)
    {
        if (!attackingOperators.Contains(op))
        {
            Debug.Log($"Enemy�� �����ϴ� Operator �߰� : {op.name}");
            attackingOperators.Add(op);
        }
    }

    public void RemoveAttackingOperator(Operator op)
    {
        attackingOperators.Remove(op);
    }

    private void ExitCurrentTile()
    {
        if (currentTile != null)
        {
            currentTile.EnemyExited(this);
            currentTile = null;
        }
    }

    private void EnterNewTile(Tile newTile)
    {
        currentTile = newTile;
        currentTile.EnemyEntered(this);
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
        if (operatorsInRange.Count > 0 && IsAttackCooldownComplete)
        {
            Operator target = operatorsInRange.OrderByDescending(o => o.deploymentOrder).First();
            Attack(target);
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
}
