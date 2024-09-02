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

    public float attackRange; // ���� ����

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

        //RequestPath(startPoint, endPoint);
        //transform.position = startPoint; // ���� ��ġ ����
        this.pathData = pathData;

        // �̰Ŵ� �����ؾ��� ���� ���� - Spawner�� ��ġ�� ���������� �����ϸ�.
        if (pathData.nodes.Count > 0)
        {
            transform.position = MapManager.Instance.GetWorldPosition(pathData.nodes[0].gridPosition); 
        }

        CreateEnemyUI();

        UpdateTargetNode();
        // stats�� ����ϴ� ������ ���Ŀ� �߰�(?)
    }
    private void Update()
    {
        // ��� ������ ���� ������ �����ϰ�, �װ� �ƴ϶�� �̵��ϴ� ������ ����
        // �ٰŸ� ���� ������ ���� ������ ���۷����͸� �����ϰ�, ���Ÿ� ���� ���� ���� ���� �ִ� ���۷����͸� �����ϴ� ���.

        // ������ ��ΰ� �ִ�
        if (pathData != null && currentNodeIndex < pathData.nodes.Count)
        {
            if (blockingOperator != null) // ���� ���� ���۷����Ͱ� �ִٸ�
            {
                AttackBlockingOperator(); // �ش� ���۷����͸� ����
            }
            else // ���� ���� ���۷����Ͱ� ���ٸ�
            {
                CheckAndAddBlockingOperator(); // ���۷����Ͱ� �����ϴ��� üũ 
                if (!isWaiting) // ��� ���� �ƴ϶��
                {
                    MoveAlongPath(); // �̵�
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
            targetPosition = MapManager.Instance.GetWorldPosition(targetNode.gridPosition);
        }
        Debug.Log($"{currentNodeIndex}");
    }

    private void AttackBlockingOperator()
    {
        if (blockingOperator != null)
        {
            Attack(blockingOperator);
        }
    }

    private void ReachDestination()
    {
        StageManager.Instance.OnEnemyReachDestination();
        Destroy(gameObject);
    }


    // ���۷����Ͱ� ���� ���� ���� ���� �ִ°�?
    public bool OperatorInEnemyAttackRange(Vector3 targetPosition)
    {
        // ���� ��� ���� ���� ���� �ִ����� �Ǻ��Ѵ�
        return Vector3.Distance(transform.position, targetPosition) <= attackRange;
    }

    private void FindAndAttackTarget()
    {
        Unit target = null;

        // 1. �ڽ��� ���� ���� ��븦 ���� �����Ѵ�
        if (blockingOperator != null) 
        {
            target = blockingOperator;
        }

        // 2. ���� ���� �ƴ� ���, ���� ���� ���� ���� �������� ��ġ�� ����� �����Ѵ�
        else
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, attackRange); // ������ ���� �׷� ���� ���� �ִ� ���� ����
            target = colliders
                .Select(c => c.GetComponent<Operator>()) // ���۷����͵鸸 �߷�����
                .Where(o => o != null && CanAttack(o.transform.position)) // ���� ������
                .OrderByDescending(o => o.deploymentOrder)
                .FirstOrDefault();
        }

        if (target != null)
        {
            Attack(target);
        }
    }


    public override void Attack(Unit target)
    {
        float damage = Stats.AttackPower * DamageMultiplier;
        base.Attack(target);
        // �� Ư���� ���� ȿ���� ������ �� �ִ�
    }

    public override bool CanAttack(Vector3 targetPosition)
    {
        if (Vector3.Distance(transform.position, targetPosition) <= attackRange)
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

    // ������ Ÿ���� ���� ��ǥ ���� 0.05 �̳��� ��
    private bool CheckIfReachedDestination()
    {
        int lastNodeIndex = pathData.nodes.Count - 1;
        Vector3 lastNodePosition = MapManager.Instance.GetWorldPosition(pathData.nodes[lastNodeIndex].gridPosition);
        if (Vector3.Distance(transform.position, lastNodePosition) < 0.05f)
        {
            return true;
        }
        return false; 
    }
}
