using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Operator : Unit
{
    [SerializeField] // �ʵ� ����ȭ, Inspector���� �� �ʵ� �����
    public OperatorData data;

    [SerializeField, HideInInspector]
    public Vector3 facingDirection = Vector3.left;


    public int currentBlockedEnemies; // ���� ���� ��
    public int deploymentOrder { get; private set; } // ��ġ ����

    [HideInInspector] public bool isBlocking = false; // ���� ���ΰ�

    private Map currentMap;
    private float attackCooldown = 0f; // data.baseStats���� ������ AttackSpeed ���� ���� ������

    // ���Ÿ� ������ ���� ������
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
            // ���� �ӵ��� ���� ������
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
            // Ÿ�� �켱���� ���� ������ �ʿ���
            Enemy target = enemiesInRange[0]; // �ϴ� ����
            Debug.Log("���� ���� ���� ���� ����");

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

        // ���� �� ��ٿ� ����
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

            // ����ü ���� ��ġ
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
            
            GameObject projectileObj = Instantiate(data.projectilePrefab, spawnPosition, Quaternion.identity);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(enemy, stats.AttackPower);
            }
        }
    }


    // ���� Ÿ�Ͽ� �ִ� ������ ��ȯ��
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
                // Ÿ�� ���� ������ ���� ������ Tile.cs�� ������
                List<Enemy> enemiesOnTile = targetTile.GetEnemiesOnTile();
                enemiesInRange.AddRange(enemiesOnTile);
            }
        }
        return enemiesInRange.Distinct().ToList(); // �ߺ� �����ؼ� ��ȯ
    }


    // Operator ȸ��
    public Vector2Int RotateOffset(Vector2Int offset, Vector3 direction)
    {
        if (direction == Vector3.left) return offset;
        if (direction == Vector3.right) return new Vector2Int(-offset.x, -offset.y);
        if (direction == Vector3.forward) return new Vector2Int(-offset.y, offset.x); // 2����(y���)���� ���� ����
        if (direction == Vector3.back) return new Vector2Int(offset.y, -offset.x); // 2���� ���� �Ʒ���
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
        // �� order��� ���� ������ ������Ʈ�� �� ���� �ʿ���
        // ���߿� StageManager�� �����ϵ��� �ؼ� �ű�ٰ� ��������
        deploymentOrder = order;
    }

    // --- ���� ���� �޼����
    
    // �� ���۷����Ͱ� ���� ������ �� �ִ� �����ΰ�?
    public bool CanBlockEnemy()
    {
        return isBlocking && currentBlockedEnemies < data.maxBlockableEnemies;
    }

    // ���� �����ϴٸ� �� ������ + 1
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
