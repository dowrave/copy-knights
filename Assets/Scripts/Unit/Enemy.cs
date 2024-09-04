using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemy : Unit
{
    private PathData pathData;
    private int currentNodeIndex = 0;


    // ������Ƽ�� ���� �ٰŸ��� ���ݹ��� 0, ���Ÿ��� ���� ����ŭ ����
    public float AttackRange
    {
        get
        {
            return data.stats.attackRangeType == AttackRangeType.Melee ? 0f : data.attackRange;
        }
        private set
        {
            data.attackRange = value;
        }
    }
    private List<Operator> operatorsInRange = new List<Operator>();

    private Operator blockingOperator; // �ڽ��� ���� ���� ���۷�����
    private List<Operator> attackingOperators = new List<Operator>(); // �ڽ��� ���� ���� ���۷�����

    // ���� ü�� ������
    public float CurrentHealth => stats.health;
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
    protected override void Update()
    {
        base.Update(); // ���� ��ٿ� ����

        // ������ ��ΰ� �ִ�
        if (pathData != null && currentNodeIndex < pathData.nodes.Count)
        {
            // 1. ���� ���� ���۷����Ͱ� ���� ��
            if (blockingOperator != null) 
            {
                // ���� ������ ���¶��
                if (IsAttackCooldownComplete)
                {
                    PerformMeleeAttack(blockingOperator); // ������ ���ϴ� ���¶��, ���� ���� ������ ���� ���� �ٰŸ� ������ ��
                    StartAttackCooldown();
                }
            }

            // 2. ���� ���� �ƴ϶��
            else 
            {
                if (HasTargetInRange()) // ���� ���� ���� ���� �ִٸ�
                {
                    Debug.Log("���� ���� ���� ���� ����");
                    if (IsAttackCooldownComplete) // ���� ��Ÿ���� �ƴ϶��
                    {
                        AttackLastDeployedOperator(); // ���� �������� ��ġ�� ���۷����� ����
                    }
                }

                else
                {
                    Debug.Log("���� ���� ���� ���� ����");
                    UpdateOperatorsInRange(); // ���� ���� ���� �ִ� ���۷����� ����Ʈ�� ����
                }


                // �̵� ���� ����. ���� ���� �ƴ� ������ �����ؾ� �Ѵ�. 
                if (!isWaiting) // ��� �̵� �� ��ٸ��� ��Ȳ�� �ƴ϶��
                {
                    MoveAlongPath(); // �̵�
                    CheckAndAddBlockingOperator(); // ���� Ÿ�Ͽ� �ִ� ���۷������� ���� ���� ���� üũ
                }
            }
        }

        attackCooldownTimer -= Time.deltaTime;
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

        base.Initialize(data.stats);
    
        MovementSpeed = data.movementSpeed;
        BlockCount = data.blockCount;
        DamageMultiplier = data.damageMultiplier;
        DefenseMultiplier = data.defenseMultiplier;

        maxHealth = stats.health; // �ʱ� ü�� ����, ���Ŀ� ���� ü�°� ���ؼ� ü�¹� ǥ�� ���� ����
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

    public override void Attack(Unit target)
    {
        if (target is not Operator op) return;

        base.Attack(target); // ���� ������ ��, PerformAttack�� �����Ű�� ��ٿ��� ����
    }

    protected override void PerformAttack(Unit target)
    {
        switch (attackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target);
                break;
        }
    }

    private void PerformMeleeAttack(Unit target)
    {
        float damage = Stats.attackPower * DamageMultiplier;
        target.TakeDamage(damage);
    }

    private void PerformRangedAttack(Unit target)
    {
        if (data.projectilePrefab != null)
        {
            float damage = Stats.attackPower * DamageMultiplier;

            // ����ü ���� ��ġ
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;

            GameObject projectileObj = Instantiate(data.projectilePrefab, spawnPosition, Quaternion.identity);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(target, damage);
            }
        }
    }


    protected override bool IsTargetInRange(Unit target)
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
}
