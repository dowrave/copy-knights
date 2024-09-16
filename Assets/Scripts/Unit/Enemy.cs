using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemy : UnitEntity, IMovable, ICombatEntity
{
    [SerializeField]
    private EnemyData enemyData;
    public new EnemyData Data => enemyData;

    private EnemyStats currentStats;

    public AttackType AttackType => enemyData.attackType;
    public AttackRangeType AttackRangeType => enemyData.attackRangeType;
    public float AttackPower { get => currentStats.AttackPower; private set => currentStats.AttackPower = value; }
    public float AttackSpeed { get => currentStats.AttackSpeed; private set => currentStats.AttackSpeed = value; }
    public float MovementSpeed { get => currentStats.MovementSpeed; private set => currentStats.MovementSpeed = value; } 
    public int BlockCount { get => enemyData.blockCount; private set => enemyData.blockCount = value; } // Enemy가 차지하는 저지 수

    public float AttackCooldown { get; private set; }

    public float AttackRange
    {
        get
        {
            return enemyData.attackRangeType == AttackRangeType.Melee ? 0f : currentStats.AttackRange;
        }
        private set
        {
            currentStats.AttackRange = value;
        }
    }

    // 경로 관련
    private PathData pathData;
    private int currentNodeIndex = 0;
    private List<Operator> operatorsInRange = new List<Operator>();

    private Operator blockingOperator; // 자신을 저지 중인 오퍼레이터
    private UnitEntity currentTarget;

    private EnemyUI enemyUI;

    private PathNode targetNode;
    private Vector3 targetPosition;
    private bool isWaiting = false;

    public void Initialize(EnemyData enemyData, PathData pathData)
    {
        this.enemyData = enemyData;
        currentStats = enemyData.stats;

        this.pathData = pathData;

        base.InitializeUnitProperties();
        InitializeEnemyProperties();
    }

    private void InitializeEnemyProperties()
    {
        SetupInitialPosition();
        CreateEnemyUI();
        UpdateTargetNode();
    }

    private void SetupInitialPosition()
    {
        if (pathData != null && pathData.nodes.Count > 0)
        {
            transform.position = MapManager.Instance.GetWorldPosition(pathData.nodes[0].gridPosition) + Vector3.up * 0.5f;
        }
        else
        {
            Debug.LogWarning("PathData is not set or empty. Initial position not set.");
        }
    }

    private void Update()
    {
        UpdateAttackCooldown();

        // 진행할 경로가 있다
        if (pathData != null && currentNodeIndex < pathData.nodes.Count)
        {
            // 1. 저지 중인 오퍼레이터가 있을 때
            if (blockingOperator != null) 
            {
                // 공격 가능한 상태라면
                if (AttackCooldown <= 0)
                {
                    PerformMeleeAttack(blockingOperator); // 저지를 당하는 상태라면, 적의 공격 범위에 관계 없이 근거리 공격을 함
                    
                }
            }

            // 2. 저지 중이 아니라면
            else 
            {
                UpdateOperatorsInRange();

                if (HasTargetInRange()) // 공격 범위 내에 적이 있다면
                {
                    Debug.Log("공격 범위 내에 적이 있음");
                    if (AttackCooldown <= 0) // 공격 쿨타임이 아니라면
                    {
                        AttackLastDeployedOperator(); // 가장 마지막에 배치된 오퍼레이터 공격
                    }
                }

                // 이동 관련 로직. 저지 중이 아닐 때에만 동작해야 한다. 
                if (!isWaiting) // 경로 이동 중 기다리는 상황이 아니라면
                {
                    MoveAlongPath(); // 이동
                    CheckAndAddBlockingOperator(); // 같은 타일에 있는 오퍼레이터의 저지 가능 여부 체크
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

    private void CheckAndAddBlockingOperator()
    {
        Tile currentTile = FindCurrentTile();

        if (currentTile != null)
        {
            IDeployable tileDeployable = currentTile.OccupyingDeployable;
            if (tileDeployable is Operator op)
            {
                // 자신을 저지하는 오퍼레이터가 없음 and 현재 타일에 오퍼레이터가 있음 and 그 오퍼레이터가 저지 가능한 상태
                if (op != null && op.CanBlockEnemy() && blockingOperator == null)
                {
                    blockingOperator = op;
                    blockingOperator.TryBlockEnemy(this); // 오퍼레이터에서도 저지 중인 Enemy를 추가
                }

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

    // 
    private void MoveAlongPath()
    {
        if (CheckIfReachedDestination())
        {
            ReachDestination();
            return;
        }

        Move(targetPosition);

        // 타일 갱신
        Tile newTile = MapManager.Instance.GetTileAtPosition(transform.position);
        if (newTile != CurrentTile)
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

    public void Move(Vector3 destination)
    {
        transform.position = Vector3.MoveTowards(transform.position, destination, MovementSpeed * Time.deltaTime);
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
    }

    private void ReachDestination()
    {
        StageManager.Instance.OnEnemyReachDestination();
        Destroy(gameObject);
    }
    private void OnEnable()
    {
        Barricade.OnBarricadeDeployed += OnBarricadeStateChanged;
        Barricade.OnBarricadeRemoved += OnBarricadeStateChanged; 
    }

    private void OnDisable()
    {
        Barricade.OnBarricadeDeployed -= OnBarricadeStateChanged;
        Barricade.OnBarricadeRemoved -= OnBarricadeStateChanged;
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
            Operator op = collider.transform.parent?.GetComponent<Operator>(); // GetComponent : 해당 오브젝트부터 시작, 부모 오브젝트로 올라가며 컴포넌트를 찾는다.

            if (op != null && op.IsDeployed)
            {
                // 콜라이더는 구에 '닿기만 하면' 판정이 되므로, 실제 AttackRange보다 공격 범위가 더 클 수 있음
                // 그래서 콜라이더는 후보군을 추리는 역할을 하고, 실제 거리 계산은 따로 수행한다.

                // 실제 거리 계산
                float actualDistance = Vector3.Distance(transform.position, op.transform.position);
                if (actualDistance <= AttackRange)
                {
                    operatorsInRange.Add(op);
                }
            }
        }
    }

    public void Attack(UnitEntity target, AttackType attackType, float damage)
    {
        PerformAttack(target);    
    }

    private void PerformAttack(UnitEntity target)
    {
        switch (AttackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target);
                break;
        }
    }

    private void PerformMeleeAttack(UnitEntity target)
    {
        float damage = AttackPower; 
        target.TakeDamage(AttackType, damage);
    }

    private void PerformRangedAttack(UnitEntity target)
    {
        if (enemyData.projectilePrefab != null)
        {
            float damage = currentStats.AttackPower;

            // 투사체 생성 위치
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;

            GameObject projectileObj = Instantiate(enemyData.projectilePrefab, spawnPosition, Quaternion.identity);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(target, AttackType, damage);
            }
        }
    }

    protected bool IsTargetInRange(UnitEntity target)
    {
        return Vector3.Distance(target.transform.position, transform.position) <= AttackRange;
    }

    private bool HasTargetInRange()
    {
        return operatorsInRange.Count > 0;
    }

    protected override void Die()
    {
        Debug.LogWarning("적 사망");

        // 저지 중인 오퍼레이터에게 저지를 해제시킴
        if (blockingOperator != null)
        {
            blockingOperator.UnblockEnemy(this);
        }

        // 공격 중인 개체의 현재 타겟 제거
        foreach (Operator op in attackingEntities.ToList())
        {
            op.OnTargetLost(this);
        }

        StageManager.Instance.OnEnemyDefeated(); // 사망한 적 수 +1

        // UI 제거
        if (enemyUI != null)
        {
            Destroy(enemyUI.gameObject);
        }

        base.Die();
    }

    public override void TakeDamage(AttackType attackType, float damage)
    {
        base.TakeDamage(attackType, damage);

        // UI 업데이트
        if (enemyUI != null)
        {
            enemyUI.UpdateUI();
        }
    }

    /// <summary>
    /// 이 개체를 공격하는 적 추가
    /// </summary>
    public override void AddAttackingEntity(ICombatEntity combatEntity)
    {
        if (combatEntity is UnitEntity unitEntity)
        {
            Debug.Log($"Enemy를 공격하는 Operator 추가 : {unitEntity.Data.entityName}");
        }

        base.AddAttackingEntity(combatEntity);
    }

    public override void RemoveAttackingEntity(ICombatEntity combatEntity)
    {
        base.RemoveAttackingEntity(combatEntity);
    }

    private void ExitCurrentTile()
    {
        if (CurrentTile != null)
        {
            CurrentTile.EnemyExited(this);
            CurrentTile = null;
        }
    }

    private void EnterNewTile(Tile newTile)
    {
        CurrentTile = newTile;
        CurrentTile.EnemyEntered(this);
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
        if (operatorsInRange.Count > 0 && AttackCooldown <= 0)
        {
            Operator target = operatorsInRange.OrderByDescending(o => o.deploymentOrder).First();
            Attack(target, AttackType, AttackPower);
        }
    }

    // 바리케이트가 배치되거나 사라질 때마다 호출, 경로를 수정해야 하는 경우 수정하거나 기존 경로 위에 있는 Barricade 파괴
    private void OnBarricadeStateChanged(Barricade barricade)
    {
        // 현재 경로의 유효성 확인
        if (!IsCurrentPathValid())
        {
            RecalculatePath();
        }
    }

    private bool IsCurrentPathValid()
    {
        if (currentNodeIndex < pathData.nodes.Count)
        {
            Vector2Int nextGridPos = pathData.nodes[currentNodeIndex].gridPosition;
            Tile nextTile = MapManager.Instance.GetTile(nextGridPos.x, nextGridPos.y);
            return nextTile != null && nextTile.data.isWalkable;
        }
        return false;
    }

    private void RecalculatePath()
    {
        Vector3 currentPosition = transform.position;
        Vector3 endPosition = MapManager.Instance.GetEndPoint();
        pathData = PathFindingManager.Instance.FindPath(currentPosition, endPosition);
        currentNodeIndex = 0;
    }

    public void UpdateAttackCooldown()
    {
        if (AttackCooldown > 0f)
        {
            AttackCooldown -= Time.deltaTime;
        }
    }

    // ICombatEntity 메서드 - 인터페이스 멤버는 모두 public으로 구현해야 함
    public bool CanAttack()
    {
        return currentTarget != null && AttackCooldown <= 0;
    }

    public void SetAttackCooldown()
    {
        AttackCooldown = 1 / AttackSpeed;
    }
}
