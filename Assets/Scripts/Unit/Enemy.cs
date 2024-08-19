using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class Enemy : Unit
{
    private int currentPathIndex = 0; // 경로 중 현재 인덱스
    private List<Vector3> path;
    private List<float> waitTimes; 

    public float attackRange; // 공격 범위

    private Operator blockingOperator; // 자신을 저지 중인 오퍼레이터
    private List<Operator> attackingOperators = new List<Operator>(); // 자신을 공격 중인 오퍼레이터

    // 현재 체력 접근자
    public float CurrentHealth => stats.Health;

    // 최대 체력 접근자
    private float maxHealth;
    public float MaxHealth => maxHealth;

    private EnemyUI enemyUI;

    public EnemyData data;

    public float MovementSpeed { get; private set; } // 이동 속도
    public int BlockCount { get; private set; }

    public float DamageMultiplier { get; private set; }
    public float DefenseMultiplier { get; private set; }



    public void Initialize(EnemyData enemyData, Vector3 startPoint, Vector3 endPoint)
    {
        InitializeStats(enemyData);

        RequestPath(startPoint, endPoint);
        transform.position = startPoint; // 시작 위치 설정
        CreateEnemyUI();

        // stats을 사용하는 로직은 이후에 추가(?)
    }
    private void Update()
    {
        // 사실 공격할 적이 있으면 공격하고, 그게 아니라면 이동하는 로직이 맞음
        // 근거리 적은 저지를 당할 때에만 오퍼레이터를 공격하고, 원거리 적은 공격 범위 내에 있는 오퍼레이터를 공격하는 방식.

        // 진행할 경로가 있다
        if (path != null && currentPathIndex < path.Count)
        {

            // 저지를 당하고 있지 않다면 이동한다
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

        maxHealth = stats.Health; // 초기 체력 설정, 이후에 현재 체력과 비교해서 체력바 표시 여부 결정
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
        // 현재 밟은 타일에 오퍼레이터가 있는지 검사
        Vector2Int nowGridPosition = MapManager.Instance.GetGridPosition(transform.position);
        Tile currentTile = MapManager.Instance.GetTile(nowGridPosition.x, nowGridPosition.y);
        Operator tileOperator = currentTile?.OccupyingOperator;

        // 자신을 저지하는 오퍼레이터가 없음 and 현재 타일에 오퍼레이터가 있음 and 그 오퍼레이터가 저지 가능한 상태
        if (tileOperator != null && tileOperator.CanBlockEnemy() && blockingOperator == null)
        {
            blockingOperator = currentTile.OccupyingOperator;
            blockingOperator.TryBlockEnemy(this); // 오퍼레이터에서도 저지 중인 Enemy를 추가
        }

    }


    private void MoveAlongPath()
    {
        // 이동하며 경로의 타일 인덱스를 갱신함
        Vector3 targetPosition = path[currentPathIndex];
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, MovementSpeed * Time.deltaTime);
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

    // 대기 중일 때 실행
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
        float damage = Stats.AttackPower * DamageMultiplier;
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
        Debug.LogWarning("적 사망");

        // 저지 중인 오퍼레이터에게 저지를 해제시킴
        if (blockingOperator != null)
        {
            blockingOperator.UnblockEnemy(this);
        }

        foreach (Operator op in attackingOperators.ToList())
        {
            op.OnTargetLost(this);
        }

        StageManager.Instance.OnEnemyDefeated(); // 사망한 적 수 +1

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
