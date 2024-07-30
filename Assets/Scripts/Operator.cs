using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Operator : Unit
{
    [SerializeField] // 필드 직렬화, Inspector에서 이 필드 숨기기
    public OperatorData data;

    [SerializeField, HideInInspector]
    public Vector3 facingDirection = Vector3.left;


    public int currentBlockedEnemies; // 현재 저지 수
    public int deploymentOrder { get; private set; } // 배치 순서

    [HideInInspector] public bool isBlocking = false; // 저지 중인가

    private Map currentMap;
    private float attackCooldown = 0f; // data.baseStats에서 들어오는 AttackSpeed 값에 의해 결정됨

    // 원거리 공격을 위한 변수들
    private Enemy currentTarget;


    private void Start()
    {
        base.Initialize(data.baseStats);
        attackRangeType = data.attackRangeType;
        currentMap = FindObjectOfType<Map>();
    }

    private void Update()
    {
        if (attackCooldown > 0)
        {
            // 공격 속도에 의한 딜레이
            attackCooldown -= Time.deltaTime; 
        }
        else
        {
            FindAndAttackTarget();
        }
        Debug.Log(attackCooldown);
    }

    private void FindAndAttackTarget()
    {
        List<Enemy> enemiesInRange = GetEnemiesInAttackRange();
        if (enemiesInRange.Count > 0 )
        {
            // 타겟 우선순위 선정 로직이 필요함
            Enemy target = enemiesInRange[0]; // 일단 떔빵
            Debug.Log("공격 범위 내에 적이 들어옴");

            Attack(target);
        }
    }


    public override void Attack(Unit target)
    {
        if (!canAttack || !(target is Enemy enemy)) return;

        switch (attackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(enemy);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(enemy);
                break;
        }

        // 공격 후 쿨다운 설정
        attackCooldown = 1f / stats.AttackSpeed;
    }

    private void PerformMeleeAttack(Enemy enemy)
    {
        enemy.TakeDamage(stats.AttackPower);
    }

    private void PerformRangedAttack(Enemy enemy)
    {
        if (data.projectilePrefab != null)
        {

            // 투사체 생성 위치
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
            
            GameObject projectileObj = Instantiate(data.projectilePrefab, spawnPosition, Quaternion.identity);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(enemy, stats.AttackPower);
            }
        }
    }


    // 공격 타일에 있는 적들을 반환함
    private List<Enemy> GetEnemiesInAttackRange()
    {
        List<Enemy> enemiesInRange = new List<Enemy>();
        Vector2Int operatorGridPos = currentMap.WorldToGridPosition(transform.position);

        foreach (Vector2Int offset in data.attackableTiles)
        {
            Vector2Int rotatedOffset = RotateOffset(offset, facingDirection);
            Vector2Int targetGridPos = operatorGridPos + rotatedOffset;

            Tile targetTile = currentMap.GetTile(targetGridPos.x, targetGridPos.y);
            if (targetTile != null)
            {
                // 타일 위의 적들을 보는 로직은 Tile.cs에 구현됨
                List<Enemy> enemiesOnTile = targetTile.GetEnemiesOnTile();
                enemiesInRange.AddRange(enemiesOnTile);
            }
        }
        return enemiesInRange.Distinct().ToList(); // 중복 제거해서 반환
    }


    // Operator 회전
    public Vector2Int RotateOffset(Vector2Int offset, Vector3 direction)
    {
        if (direction == Vector3.left) return offset;
        if (direction == Vector3.right) return new Vector2Int(-offset.x, -offset.y);
        if (direction == Vector3.forward) return new Vector2Int(-offset.y, offset.x); // 2차원(y평면)으로 보면 위쪽
        if (direction == Vector3.back) return new Vector2Int(offset.y, -offset.x); // 2차원 기준 아래쪽
        return offset;
    }
    public Vector2Int[] GetAttackableTiles()
    {
        return data.attackableTiles;
    }
    
    public void SetFacingDirection(Vector3 direction)
    {
        facingDirection = direction;
    }

    public override bool CanAttack(Vector3 targetPosition)
    {
        Vector2Int relativePosition = WorldToRelativeGridPosition(targetPosition);
        return System.Array.Exists(data.attackableTiles, tile => tile == relativePosition);
    }

    private Vector2Int WorldToRelativeGridPosition(Vector3 worldPosition)
    {
        if (currentMap != null)
        {
            Vector2Int absoluteGridPos = currentMap.WorldToGridPosition(worldPosition);
            Vector2Int operatorGridPos = currentMap.WorldToGridPosition(transform.position);
            return absoluteGridPos - operatorGridPos;
        }
        return Vector2Int.zero;
    }
    

    public void SetDeploymentOrder(int order)
    {
        // 이 order라는 값을 관리할 오브젝트가 또 따로 필요함
        // 나중에 StageManager를 구현하든지 해서 거기다가 때려넣자
        deploymentOrder = order;
    }

    // --- 저지 관련 메서드들
    
    // 이 오퍼레이터가 적을 저지할 수 있는 상태인가?
    public bool CanBlockEnemy()
    {
        return isBlocking && currentBlockedEnemies < data.maxBlockableEnemies;
    }

    // 저지 가능하다면 현 저지수 + 1
    public void BlockEnemy()
    {
        if (CanBlockEnemy())
        {
            currentBlockedEnemies++;
        }
    }

    public void UnblockEnemy()
    {
        if (currentBlockedEnemies > 0)
        {
            currentBlockedEnemies--;
        }
    }

}
