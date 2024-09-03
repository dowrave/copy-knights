using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class Enemy : Unit
{
    private PathData pathData;
    private int currentNodeIndex = 0;
    private float attackCooldown = 0f;

    private float _attackRange; // ���� ����
    // ������Ƽ�� ���� �ٰŸ��� ���ݹ��� 0, ���Ÿ��� ���� ����ŭ ����
    public float AttackRange
    {
        get
        {
            return data.attackRangeType == AttackRangeType.Melee ? 0f : _attackRange;
        }
        private set
        {
            _attackRange = value;
        }
    }
    private List<Operator> operatorsInRange = new List<Operator>();

    private Operator blockingOperator; // �ڽ��� ���� ���� ���۷�����
    private List<Operator> attackingOperators = new List<Operator>(); // �ڽ��� ���� ���� ���۷�����

    // ���� ü�� ������
    public float CurrentHealth => stats.Health;
    // �ִ� ü�� ������
    private float maxHealth;
    public float MaxHealth => maxHealth;
    private EnemyUI enemyUI;
    public EnemyData data;

    public float MovementSpeed { get; private set; } // �̵� �ӵ�
    public int BlockCount { get; private set; }

    public float DamageMultiplier { get; private set; }
    public float DefenseMultiplier { get; private set; }

    private Tile currentTile;
    private PathNode targetNode;
    private Vector3 targetPosition;
    private bool isWaiting = false;


    public void Initialize(EnemyData enemyData, PathData pathData)
    {
        InitializeStats(enemyData);
        this.pathData = pathData;
        AttackRange = data.attackRange;

        // ������ ��ġ ����(��� ���� �� �������� ��ġ�� �����ϰ� �����ϸ� ����)
        if (pathData.nodes.Count > 0)
        {
            transform.position = MapManager.Instance.GetWorldPosition(pathData.nodes[0].gridPosition) + Vector3.up * 0.5f;
        }

        CreateEnemyUI();

        UpdateTargetNode();
        // stats�� ����ϴ� ������ ���Ŀ� �߰�(?)
    }
    private void Update()
    {
        
        // ������ ��ΰ� �ִ�
        if (pathData != null && currentNodeIndex < pathData.nodes.Count)
        {
            // 1. ���� ���� ���۷����Ͱ� ���� ��
            if (blockingOperator != null) 
            {
                // ���� ������ ���¶��
                if (attackCooldown <= 0)
                {
                    Attack(blockingOperator);
                }
            }

            // 2. ���� ���� �ƴ϶��
            else 
            {
                UpdateOperatorsInRange(); // ���� ���� ���� �ִ� ���۷����� ����Ʈ�� ����

                if (attackCooldown <= 0)
                {
                    AttackLastDeployedOperator(); // ���� �������� ��ġ�� ���۷����� ����
                }

                // �̵� ���� ����. ���� ���� �ƴ� ������ �����ؾ� �Ѵ�. 
                if (!isWaiting) // ��� �̵� ��, ��Ⱑ �ƴ϶��
                {
                    MoveAlongPath(); // �̵�
                    CheckAndAddBlockingOperator(); // ���� Ÿ�Ͽ� �ִ� ���۷������� ���� ���� ���� üũ
                }
            }
        }

        attackCooldown -= Time.deltaTime;
    }

    private void CreateEnemyUI()
    {
        enemyUI = GetComponentInChildren<EnemyUI>();
        if (enemyUI != null)
        {
            enemyUI.Initialize(this);
        }
    }

    private void InitializeStats(EnemyData enemyData)
    {
        data = enemyData;

        base.Initialize(data.baseStats);
    
        MovementSpeed = data.movementSpeed;
        BlockCount = data.blockCount;
        DamageMultiplier = data.damageMultiplier;
        DefenseMultiplier = data.defenseMultiplier;

        maxHealth = stats.Health; // �ʱ� ü�� ����, ���Ŀ� ���� ü�°� ���ؼ� ü�¹� ǥ�� ���� ����
    }

    private void CheckAndAddBlockingOperator()
    {
        Tile currentTile = FindCurrentTile();

        if (currentTile != null)
        {
            Operator tileOperator = currentTile.OccupyingOperator;

            // �ڽ��� �����ϴ� ���۷����Ͱ� ���� and ���� Ÿ�Ͽ� ���۷����Ͱ� ���� and �� ���۷����Ͱ� ���� ������ ����
            if (tileOperator != null && tileOperator.CanBlockEnemy() && blockingOperator == null)
            {
                blockingOperator = currentTile.OccupyingOperator;
                blockingOperator.TryBlockEnemy(this); // ���۷����Ϳ����� ���� ���� Enemy�� �߰�
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

        //PathNode targetNode = pathData.nodes[currentNodeIndex];
        //Vector3 targetPosition = MapManager.Instance.GetWorldPosition(targetNode.gridPosition);

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
        Debug.Log($"{currentNodeIndex}");
    }

    private void ReachDestination()
    {
        StageManager.Instance.OnEnemyReachDestination();
        Destroy(gameObject);
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
            Operator op = collider.GetComponent<Operator>();
            if (op != null)
            {
                operatorsInRange.Add(op);
            }
        }
    }

    public void Attack(Operator target)
    {
        if (!canAttack || !(target is Operator op)) return;

        switch (attackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(op);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(op);
                break;
        }

        // ���� �� ��ٿ� ����
        attackCooldown = 1f / stats.AttackSpeed;
    }

    private void PerformMeleeAttack(Operator op)
    {
        float damage = Stats.AttackPower * DamageMultiplier;
        op.TakeDamage(damage);
    }

    private void PerformRangedAttack(Operator op)
    {
        if (data.projectilePrefab != null)
        {
            float damage = Stats.AttackPower * DamageMultiplier;

            // ����ü ���� ��ġ
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;

            GameObject projectileObj = Instantiate(data.projectilePrefab, spawnPosition, Quaternion.identity);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(op, damage);
            }
        }
    }

    public override bool CanAttack(Vector3 targetPosition)
    {
        if (Vector3.Distance(transform.position, targetPosition) <= AttackRange)
        {
            return true;
        }
        return false;
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
        if (operatorsInRange.Count > 0 && canAttack)
        {
            Operator target = operatorsInRange.OrderByDescending(o => o.deploymentOrder).First();
            Attack(target);
        }
    }
}
