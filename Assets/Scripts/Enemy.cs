using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Enemy : Unit
{
    public float MovementSpeed { get; private set; } // �̵� �ӵ�
    private int currentWaypointIndex = 0; // ��� �� ���� �ε���
    private Vector3[] path; // ��δ� PathFindingManager���� �޾ƿ�
    public float attackRange; // ���� ����
    private Operator blockingOperator; // �ڽ��� ���� ���� ���۷�����
    private bool isBlocked = false;

    public void Initialize(UnitStats initialStats, float movementSpeed, Vector3 startPoint, Vector3 endPoint)
    {
        base.Initialize(initialStats);
        MovementSpeed = movementSpeed;
        RequestPath(startPoint, endPoint);
        // stats�� ����ϴ� ������ ���Ŀ� �߰�
    }

    private void RequestPath(Vector3 startPoint, Vector3 endPoint)
    {
        List<Vector3> pathList = PathFindingManager.Instance.FindPath(startPoint, endPoint);
        if (pathList != null && pathList.Count > 0)
        {
            path = pathList.ToArray();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (path != null && currentWaypointIndex < path.Length)
        {
            
            if (!isBlocked)
            {
                Move();
            }

            FindAndAttackTarget();
        }
    }

    // PathManager�� ���� ��θ� ���� �̵��Ѵ�
    private void Move()
    {
        Vector3 targetPosition = path[currentWaypointIndex];
        Vector3 newPosition = transform.position = Vector3.MoveTowards(transform.position, targetPosition, MovementSpeed * Time.deltaTime);

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
                currentWaypointIndex++;

                if (currentWaypointIndex >= path.Length) // ���� ���� ����
                {
                    ReachDestination();
                }
            }
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
        base.Die();
    }

}
