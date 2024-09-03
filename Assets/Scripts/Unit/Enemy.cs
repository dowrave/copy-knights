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

    private float _attackRange; // 공격 범위
    // 프로퍼티를 쓰면 근거리는 공격범위 0, 원거리는 넣은 값만큼 나옴
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
        this.pathData = pathData;
        AttackRange = data.attackRange;

        // 시작점 위치 설정(경로 설정 시 스포너의 위치와 동일하게 설정하면 좋다)
        if (pathData.nodes.Count > 0)
        {
            transform.position = MapManager.Instance.GetWorldPosition(pathData.nodes[0].gridPosition) + Vector3.up * 0.5f;
        }

        CreateEnemyUI();

        UpdateTargetNode();
        // stats을 사용하는 로직은 이후에 추가(?)
    }
    private void Update()
    {
        
        // 진행할 경로가 있다
        if (pathData != null && currentNodeIndex < pathData.nodes.Count)
        {
            // 1. 저지 중인 오퍼레이터가 있을 때
            if (blockingOperator != null) 
            {
                // 공격 가능한 상태라면
                if (attackCooldown <= 0)
                {
                    Attack(blockingOperator);
                }
            }

            // 2. 저지 중이 아니라면
            else 
            {
                UpdateOperatorsInRange(); // 공격 범위 내에 있는 오퍼레이터 리스트를 관리

                if (attackCooldown <= 0)
                {
                    AttackLastDeployedOperator(); // 가장 마지막에 배치된 오퍼레이터 공격
                }

                // 이동 관련 로직. 저지 중이 아닐 때에만 동작해야 한다. 
                if (!isWaiting) // 경로 이동 중, 대기가 아니라면
                {
                    MoveAlongPath(); // 이동
                    CheckAndAddBlockingOperator(); // 같은 타일에 있는 오퍼레이터의 저지 가능 여부 체크
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
    ///  공격 범위 내의 오퍼레이터 리스트를 업데이트한다
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

        // 공격 후 쿨다운 설정
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

            // 투사체 생성 위치
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

    // 마지막 타일의 월드 좌표 기준
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

    // 그리드 -> 월드 좌표 변환 후, 월드 좌표의 y좌표를 0.5로 넣음
    private Vector3 GetAdjustedTargetPosition()
    {
        Vector3 adjustedPosition = targetPosition;
        adjustedPosition.y = 0.5f;
        return adjustedPosition;
    }

    // 가장 마지막에 배치된 오퍼레이터를 공격함
    private void AttackLastDeployedOperator()
    {
        if (operatorsInRange.Count > 0 && canAttack)
        {
            Operator target = operatorsInRange.OrderByDescending(o => o.deploymentOrder).First();
            Attack(target);
        }
    }
}
