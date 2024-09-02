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

    private Tile currentTile;
    private PathNode targetNode;
    private Vector3 targetPosition;
    private bool isWaiting = false;


    public void Initialize(EnemyData enemyData, PathData pathData)
    {
        InitializeStats(enemyData);

        //RequestPath(startPoint, endPoint);
        //transform.position = startPoint; // 시작 위치 설정
        this.pathData = pathData;

        // 이거는 수정해야할 수도 있음 - Spawner의 위치가 시작점임을 생각하면.
        if (pathData.nodes.Count > 0)
        {
            transform.position = MapManager.Instance.GetWorldPosition(pathData.nodes[0].gridPosition); 
        }

        CreateEnemyUI();

        UpdateTargetNode();
        // stats을 사용하는 로직은 이후에 추가(?)
    }
    private void Update()
    {
        // 사실 공격할 적이 있으면 공격하고, 그게 아니라면 이동하는 로직이 맞음
        // 근거리 적은 저지를 당할 때에만 오퍼레이터를 공격하고, 원거리 적은 공격 범위 내에 있는 오퍼레이터를 공격하는 방식.

        // 진행할 경로가 있다
        if (pathData != null && currentNodeIndex < pathData.nodes.Count)
        {
            if (blockingOperator != null) // 저지 중인 오퍼레이터가 있다면
            {
                AttackBlockingOperator(); // 해당 오퍼레이터를 공격
            }
            else // 저지 중인 오퍼레이터가 없다면
            {
                CheckAndAddBlockingOperator(); // 오퍼레이터가 저지하는지 체크 
                if (!isWaiting) // 대기 중이 아니라면
                {
                    MoveAlongPath(); // 이동
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

        maxHealth = stats.Health; // 초기 체력 설정, 이후에 현재 체력과 비교해서 체력바 표시 여부 결정
    }

    private void CheckAndAddBlockingOperator()
    {
        Tile currentTile = FindCurrentTile();

        if (currentTile != null)
        {
            Operator tileOperator = currentTile.OccupyingOperator;

            // 자신을 저지하는 오퍼레이터가 없음 and 현재 타일에 오퍼레이터가 있음 and 그 오퍼레이터가 저지 가능한 상태
            if (tileOperator != null && tileOperator.CanBlockEnemy() && blockingOperator == null)
            {
                blockingOperator = currentTile.OccupyingOperator;
                blockingOperator.TryBlockEnemy(this); // 오퍼레이터에서도 저지 중인 Enemy를 추가
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

    // PathData 사용 버전
    private void MoveAlongPath()
    {
        if (CheckIfReachedDestination())
        {
            ReachDestination();
            return;
        }

        //PathNode targetNode = pathData.nodes[currentNodeIndex];
        //Vector3 targetPosition = MapManager.Instance.GetWorldPosition(targetNode.gridPosition);

        // 새 위치 계산(실제 이동 X)
        Vector3 newPosition = Vector3.MoveTowards(transform.position, targetPosition, MovementSpeed * Time.deltaTime);

        // 실제 이동
        transform.position = newPosition;

        // 타일 갱신
        Tile newTile = MapManager.Instance.GetTileAtPosition(newPosition);
        if (newTile != currentTile)
        {
            ExitCurrentTile();
            EnterNewTile(newTile);
        }

        // 노드 도달 확인
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

    // 대기 중일 때 실행
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
            Debug.Log($"Enemy를 공격하는 Operator 추가 : {op.name}");
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

    // 마지막 타일의 월드 좌표 기준 0.05 이내일 때
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
