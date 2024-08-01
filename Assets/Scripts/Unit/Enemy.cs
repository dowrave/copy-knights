using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class Enemy : Unit
{
    public float MovementSpeed { get; private set; } // �̵� �ӵ�
    private int currentPathIndex = 0; // ��� �� ���� �ε���
    private List<Vector3> path;
    private List<float> waitTimes; 

    public float attackRange; // ���� ����
    private Operator blockingOperator; // �ڽ��� ���� ���� ���۷�����
    private bool isBlocked = false;
    private bool isWaiting = false;

    private float maxHealth;
    public float MaxHealth => maxHealth;

    private EnemyCanvas enemyCanvas;

    public void Initialize(UnitStats initialStats, float movementSpeed, Vector3 startPoint, Vector3 endPoint)
    {
        //enemyCanvas = GetComponentInChildren<EnemyCanvas>(); // Enemy�� �ڽ� ������Ʈ�� EnemyCanvas�� �־�� ��
        base.Initialize(initialStats);
        MovementSpeed = movementSpeed;
        transform.position = startPoint; // ���� ��ġ ����
        RequestPath(startPoint, endPoint);

        maxHealth = stats.Health; // �ʱ� ü�� ����, ���Ŀ� ���� ü�°� ���ؼ� ü�¹� ǥ�� ���� ����
        //InitializeCanvas();

        UIManager.Instance.CreateEnemyUI(this);

        // stats�� ����ϴ� ������ ���Ŀ� �߰�
    }

    private void InitializeCanvas()
    {
        enemyCanvas = GetComponentInChildren<EnemyCanvas>();

        if (enemyCanvas != null)
        {
            enemyCanvas.UpdateHealthBar(stats.Health, maxHealth);
            enemyCanvas.SetHealthBarVisible(false);
        }
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

    private void Update()
    {
        if (path != null && currentPathIndex < path.Count)
        {
            
            if (!isBlocked && !isWaiting)
            {
                MoveAlongPath();
            }
            else if (isBlocked)
            {
                //AttemptToUnblock();
            }

            FindAndAttackTarget();
        }
    }

    // PathManager�� ���� ��θ� ���� ���� ���� �̵��Ѵ�
    private void MoveAlongPath()
    {
        Vector3 targetPosition = path[currentPathIndex];
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, MovementSpeed * Time.deltaTime);

        Operator blockingOp = CheckForBlockingOperator(newPosition);
        if (blockingOp != null && blockingOp.CanBlockEnemy()) // ���� ���� ���۷����Ͱ� �ְ�, �� ���۷����Ͱ� ���� ������ �� �ִ� �����ΰ�?
        {
            isBlocked = true;
            blockingOperator = blockingOp;
            blockingOperator.BlockEnemy();
        }
        else
        {
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
    }

    // ��� ���� �� ����
    private IEnumerator WaitAtNode(float waitTime)
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;
        currentPathIndex++;
    }

    private void AttackBlockingOperator()
    {
        if (blockingOperator != null)
        {
            Attack(blockingOperator);
        }
    }

    
    // ��(��)�� ���� ���� ���۷����͸� ��ȯ�ϰų�, ���ٸ� null
    private Operator CheckForBlockingOperator(Vector3 newPosition)
    {
        Collider[] colliders = Physics.OverlapSphere(newPosition, 0.1f);
        foreach (Collider collider in colliders)
        {
            Operator op = collider.GetComponent<Operator>();
            if (op != null && op.isBlocking)
            {
                return op;
            }
        }
        return null;
    }
    private void ReachDestination()
    {
        // ������ ���� �� ���� - �������� �������� 1 ������ ��
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
        // �ڽ��� ���� ���� ���� �ִٸ� ���� ���� -1 ����
        if (blockingOperator)
        {
            blockingOperator.UnblockEnemy();
        }

        // ��ȹ) �������� �Ŵ����� ����� ���� ����� �� ī��Ʈ�� +1 ����

        // ������Ʈ �ı�
        UIManager.Instance.RemoveEnemyUI(this);
        base.Die();
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);

        // �������̵� ���� : ������� ������ ü�� ������ �ٰ� ���̰� �Ѵ�
        UIManager.Instance.UpdateEnemyUI(this);
        //if (healthBar != null)
        //{
        //    enemyCanvas.SetHealthBarVisible(stats.Health < maxHealth);
        //    enemyCanvas.UpdateHealthBar(stats.Health, maxHealth);
        //}
    }

}
