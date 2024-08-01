using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class Enemy : Unit
{
    public float MovementSpeed { get; private set; } // 이동 속도
    private int currentPathIndex = 0; // 경로 중 현재 인덱스
    private List<Vector3> path;
    private List<float> waitTimes; 

    public float attackRange; // 공격 범위
    private Operator blockingOperator; // 자신을 저지 중인 오퍼레이터
    private bool isBlocked = false;
    private bool isWaiting = false;

    private float maxHealth;
    public float MaxHealth => maxHealth;

    private EnemyCanvas enemyCanvas;

    public void Initialize(UnitStats initialStats, float movementSpeed, Vector3 startPoint, Vector3 endPoint)
    {
        //enemyCanvas = GetComponentInChildren<EnemyCanvas>(); // Enemy의 자식 오브젝트로 EnemyCanvas가 있어야 함
        base.Initialize(initialStats);
        MovementSpeed = movementSpeed;
        transform.position = startPoint; // 시작 위치 설정
        RequestPath(startPoint, endPoint);

        maxHealth = stats.Health; // 초기 체력 설정, 이후에 현재 체력과 비교해서 체력바 표시 여부 결정
        //InitializeCanvas();

        UIManager.Instance.CreateEnemyUI(this);

        // stats을 사용하는 로직은 이후에 추가
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

    // PathManager로 받은 경로를 따라 다음 노드로 이동한다
    private void MoveAlongPath()
    {
        Vector3 targetPosition = path[currentPathIndex];
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, MovementSpeed * Time.deltaTime);

        Operator blockingOp = CheckForBlockingOperator(newPosition);
        if (blockingOp != null && blockingOp.CanBlockEnemy()) // 저지 중인 오퍼레이터가 있고, 그 오퍼레이터가 적을 저지할 수 있는 상태인가?
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
                // 대기 중
                if (currentPathIndex < waitTimes.Count && waitTimes[currentPathIndex] > 0)
                {
                    StartCoroutine(WaitAtNode(waitTimes[currentPathIndex]));
                }

                // 이동 중 *이거 불확실할 수도 있음
                else
                {
                    currentPathIndex++;
                }

                if (currentPathIndex >= path.Count) // 목적 지점 도착
                {
                    ReachDestination();
                }
            }
        }
    }

    // 대기 중일 때 실행
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

    
    // 적(나)을 저지 중인 오퍼레이터를 반환하거나, 없다면 null
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
        // 목적지 도달 시 로직 - 스테이지 라이프만 1 깎으면 됨
        Destroy(gameObject);
    }


    // 오퍼레이터가 적의 공격 범위 내에 있는가?
    public bool OperatorInEnemyAttackRange(Vector3 targetPosition)
    {
        // 적의 경우 공격 범위 내에 있는지만 판별한다
        return Vector3.Distance(transform.position, targetPosition) <= attackRange;
    }

    private void FindAndAttackTarget()
    {
        Unit target = null;

        // 1. 자신을 저지 중인 상대를 먼저 공격한다
        if (blockingOperator != null) 
        {
            target = blockingOperator;
        }
        // 2. 저지 중이 아닐 경우, 공격 범위 내의 가장 마지막에 배치된 대상을 공격한다
        else
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, attackRange); // 가상의 구를 그려 범위 내에 있는 적들 판정
            target = colliders
                .Select(c => c.GetComponent<Operator>()) // 오퍼레이터들만 추려내서
                .Where(o => o != null && CanAttack(o.transform.position)) // 공격 가능한
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
        // 적 특유의 공격 효과를 구현할 수 있다
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
        // 자신을 저지 중인 적이 있다면 저지 수를 -1 해줌
        if (blockingOperator)
        {
            blockingOperator.UnblockEnemy();
        }

        // 계획) 스테이지 매니저를 만들고 나서 사망한 적 카운트를 +1 해줌

        // 오브젝트 파괴
        UIManager.Instance.RemoveEnemyUI(this);
        base.Die();
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);

        // 오버라이드 내용 : 대미지를 받으면 체력 게이지 바가 보이게 한다
        UIManager.Instance.UpdateEnemyUI(this);
        //if (healthBar != null)
        //{
        //    enemyCanvas.SetHealthBarVisible(stats.Health < maxHealth);
        //    enemyCanvas.UpdateHealthBar(stats.Health, maxHealth);
        //}
    }

}
