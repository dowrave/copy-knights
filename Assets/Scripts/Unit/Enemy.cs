using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class Enemy : Unit
{
    private int currentPathIndex = 0; // ��� �� ���� �ε���
    private List<Vector3> path;
    private List<float> waitTimes; 

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



    public void Initialize(EnemyData enemyData, Vector3 startPoint, Vector3 endPoint)
    {
        InitializeStats(enemyData);

        RequestPath(startPoint, endPoint);
        transform.position = startPoint; // ���� ��ġ ����
        CreateEnemyUI();

        // stats�� ����ϴ� ������ ���Ŀ� �߰�(?)
    }
    private void Update()
    {
        // ��� ������ ���� ������ �����ϰ�, �װ� �ƴ϶�� �̵��ϴ� ������ ����
        // �ٰŸ� ���� ������ ���� ������ ���۷����͸� �����ϰ�, ���Ÿ� ���� ���� ���� ���� �ִ� ���۷����͸� �����ϴ� ���.

        // ������ ��ΰ� �ִ�
        if (path != null && currentPathIndex < path.Count)
        {

            // ������ ���ϰ� ���� �ʴٸ� �̵��Ѵ�
            if (!blockingOperator)
            {
                MoveAlongPath();
                CheckAndAddOperator();
            }
            else
            {
                AttackBlockingOperator();
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

    public void SetPath(List<Vector3> newPath, List<float> newWaitTimes)
    {
        path = newPath;
        waitTimes = newWaitTimes;
        currentPathIndex = 0;
    }
    
    private void RequestPath(Vector3 startPoint, Vector3 endPoint)
    {
        List<Vector3> pathList = PathFindingManager.Instance.FindPath(startPoint, endPoint);
        if (pathList != null && pathList.Count > 0)
        {
            SetPath(pathList, new List<float>(new float[pathList.Count]));
        }
        else
        {
            Destroy(gameObject);
        }
    }



    private void CheckAndAddOperator()
    {
        // ���� ���� Ÿ�Ͽ� ���۷����Ͱ� �ִ��� �˻�
        Vector2Int nowGridPosition = MapManager.Instance.GetGridPosition(transform.position);
        Tile currentTile = MapManager.Instance.GetTile(nowGridPosition.x, nowGridPosition.y);
        Operator tileOperator = currentTile?.OccupyingOperator;

        // �ڽ��� �����ϴ� ���۷����Ͱ� ���� and ���� Ÿ�Ͽ� ���۷����Ͱ� ���� and �� ���۷����Ͱ� ���� ������ ����
        if (tileOperator != null && tileOperator.CanBlockEnemy() && blockingOperator == null)
        {
            blockingOperator = currentTile.OccupyingOperator;
            blockingOperator.TryBlockEnemy(this); // ���۷����Ϳ����� ���� ���� Enemy�� �߰�
        }

    }


    private void MoveAlongPath()
    {
        // �̵��ϸ� ����� Ÿ�� �ε����� ������
        Vector3 targetPosition = path[currentPathIndex];
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, MovementSpeed * Time.deltaTime);
        transform.position = newPosition;
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            // ��� ��
            if (currentPathIndex < waitTimes.Count && waitTimes[currentPathIndex] > 0)
            {
                StartCoroutine(WaitAtNode(waitTimes[currentPathIndex]));
            }
            // �̵� �� *�̰� ��Ȯ���� ���� ����
            else
            {
                currentPathIndex++;
            }

            if (currentPathIndex >= path.Count) // ���� ���� ����
            {
                ReachDestination();
            }
        }
        
    }

    // ��� ���� �� ����
    private IEnumerator WaitAtNode(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        currentPathIndex++;
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
            attackingOperators.Add(op);
        }
    }

    public void RemoveAttackingOperator(Operator op)
    {
        attackingOperators.Remove(op);
    }



}
