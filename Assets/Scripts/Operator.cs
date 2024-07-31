using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Operator : Unit
{
    [SerializeField] // �ʵ� ����ȭ, Inspector���� �� �ʵ� �����
    public OperatorData data;

    [SerializeField, HideInInspector]
    public Vector3 facingDirection = Vector3.left;

    public int currentBlockedEnemies; // ���� ���� ��
    public int deploymentOrder { get; private set; } // ��ġ ����
    private bool isDeployed = false;
    private Map currentMap;
    private Enemy currentTarget;
    private float attackCooldown = 0f; // data.baseStats���� ������ AttackSpeed ���� ���� ������
    [HideInInspector] public bool isBlocking = false; // ���� ���ΰ�

    // SP ����
    private float currentSP;

    // ĵ����
    private OperatorCanvas operatorCanvas;

    // �̸����� ����
    private bool isPreviewMode = false;
    public bool IsPreviewMode
    {
        get { return isPreviewMode; }
        set
        {
            isPreviewMode = value;
            UpdateVisuals();
        }
    }
    private MeshRenderer meshRenderer;
    private Material originalMaterial;
    private Material previewMaterial;

    private void Awake()
    {
        PrepareTransparentMaterial();
    }

    // ��ġ�� ��� ���� �ʱ�ȭ ����
    private void Start()
    {
        currentSP = data.initialSP; // SP �ʱ�ȭ
        attackRangeType = data.attackRangeType;
        InitializeStats();
    }

    public void Deploy(Vector3 position, Vector3 direction)
    {
        if (!isDeployed)
        {
            isDeployed = true;
            transform.position = position;
            facingDirection = direction;
            currentMap = FindObjectOfType<Map>();
            InitializeCanvas();
        }
    }

    public void InitializeStats()
    {
        base.Initialize(data.baseStats);
        attackRangeType = data.attackRangeType;
    }


    private void Update()
    {
        attackCooldown -= Time.deltaTime;

        if (attackCooldown <= 0 && isDeployed)
        {
            FindAndAttackTarget();
        }

        RecoverSP();
    }
    
    private void InitializeCanvas()
    {
        operatorCanvas = GetComponentInChildren<OperatorCanvas>();
        if (operatorCanvas != null)
        { 
            operatorCanvas.UpdateHealthBar(stats.Health, data.baseStats.Health);
            operatorCanvas.UpdateSPBar(currentSP, data.maxSP);
        }
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

    // SP ���� �߰�
    private void RecoverSP()
    {
        currentSP = Mathf.Min(currentSP + data.SpRecoveryRate * Time.deltaTime, data.maxSP);
        operatorCanvas.UpdateSPBar(currentSP, data.maxSP);
    }

    public bool TryUseSkill(float spCost)
    {
        if (currentSP >= spCost)
        {
            currentSP -= spCost;
            operatorCanvas.UpdateSPBar(currentSP, data.maxSP);
            return true;
        }
        return false; 
    }

    protected override void Die()
    {
        // ��� �� �۵��ؾ� �ϴ� ������ ���� ��?

        // ������Ʈ �ı�
        base.Die();
    }

    private void PrepareTransparentMaterial()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
            previewMaterial = new Material(originalMaterial);
            previewMaterial.SetFloat("_Mode", 3); // TransParent ���� ����
            Color previewColor = previewMaterial.color;
            previewColor.a = 0.5f;
            previewMaterial.color = previewColor;
        }
    }

    private void UpdateVisuals()
    {
        if (isPreviewMode)
        {
            // ������ ����� ���� �ð� ����
            meshRenderer.material = previewMaterial;
        }
        else
        {
            // ���� ��ġ ����� ���� �ð� ����
            meshRenderer.material = originalMaterial;

        }
    }

}
