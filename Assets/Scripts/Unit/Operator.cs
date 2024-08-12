using System;
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

    // ���� ����
    private Enemy[] blockedEnemies;
    public IReadOnlyList<Enemy> BlockedEnemies => Array.AsReadOnly(blockedEnemies);
    private int currentBlockedEnemiesCount = 0;
    public int CurrentBlockedEnemiesCount => currentBlockedEnemiesCount;

    public int deploymentOrder { get; private set; } // ��ġ ����
    private bool isDeployed = false;
    private Map currentMap;
    private Enemy currentTarget;
    private float attackCooldown = 0f; // data.baseStats���� ������ AttackSpeed ���� ���� ������
    //[HideInInspector] public bool isBlocking = false; // ���� ���ΰ�

    // SP ����
    private float currentSP;
    public float CurrentSP => currentSP;

    //[SerializeField] private GameObject operatorUIPrefab;
    private OperatorUI operatorUI;

    public float currentHealth => stats.Health;
    // �ִ� ü��
    private float maxHealth;
    public float MaxHealth => maxHealth;

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

    // �ʵ� �� --------------------------------------------------------

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
            maxHealth = data.baseStats.Health;

            CreateOperatorUI();
        }
    }
    private void CreateOperatorUI()
    {
        //if (operatorUIPrefab != null)
        //{
            //GameObject uiObject = Instantiate(operatorUIPrefab, transform);
        operatorUI = GetComponentInChildren<OperatorUI>();
        if (operatorUI != null)
        {
            operatorUI.Initialize(this);
        }
        //}
    }

    public void InitializeStats()
    {
        base.Initialize(data.baseStats);
        attackRangeType = data.attackRangeType;
        blockedEnemies = new Enemy[data.maxBlockableEnemies];
        currentBlockedEnemiesCount = 0;
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

    private void FindAndAttackTarget()
    {
        
        // 1. ���� ���� ������ ���� ���� �� �߿��� ����
        if (blockedEnemies.Length > 0)
        {
            Enemy target = blockedEnemies[0]; // �ϴ� ����
            if (target != null)
            {
                Attack(target);
                return; 
            }
        }

        List<Enemy> enemiesInRange = GetEnemiesInAttackRange();
        // 2. ���� ���� �ƴ� ������ ���� ���� ���� �� �߿��� ������
        if (enemiesInRange.Count > 0 )
        {
            // Ÿ�� �켱���� ���� ������ �ʿ���
            Enemy target = enemiesInRange[0]; // �ϴ� ����
            if (target != null)
            {
                Attack(target);
                return;
            }
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
        return currentBlockedEnemiesCount < data.maxBlockableEnemies;
    }

    // ���� �����ϴٸ� �� ������ + 1
    public bool TryBlockEnemy(Enemy enemy)
    {
        if (CanBlockEnemy())
        {
            for (int i = 0; i < blockedEnemies.Length; i++)
            {
                if (blockedEnemies[i] == null)
                {
                    blockedEnemies[i] = enemy;
                    currentBlockedEnemiesCount++;

                    return true;
                }
            }
        }
        return false;
    }

    public void UnblockEnemy(Enemy enemy)
    {
        for (int i = 0; i < blockedEnemies.Length; i++)
        {
            if (blockedEnemies[i] == enemy)
            {
                // �ش� ���� �����ϰ� �������� ������ ���ϴ�.
                for (int j = i; j < blockedEnemies.Length - 1; j++)
                {
                    blockedEnemies[j] = blockedEnemies[j + 1];
                }
                blockedEnemies[blockedEnemies.Length - 1] = null;
                currentBlockedEnemiesCount--;
                break;
            }
        }
    }

    // ������ ��� �� ����
    public void UnblockAllEnemies()
    {
        for (int i = 0; i < blockedEnemies.Length; i++)
        {
            if (blockedEnemies[i] != null)
            {
                blockedEnemies[i] = null;
            }
        }
        currentBlockedEnemiesCount = 0;
    }

    // SP ���� �߰�
    private void RecoverSP()
    {
        if (isDeployed == false) { return;  }

        float oldSP = currentSP;
        if (data.autoRecoverSP)
        {
            currentSP = Mathf.Min(currentSP + data.SpRecoveryRate * Time.deltaTime, data.maxSP);    

        }

        if (currentSP != oldSP)
        {
            operatorUI.UpdateSPBar(currentSP, data.maxSP);
            //operatorUI.UpdateOperatorUI(this);
        }
    }

    public bool TryUseSkill(float spCost)
    {
        if (currentSP >= spCost)
        {
            currentSP -= spCost;

            return true;
        }
        return false; 
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        //operatorUI.UpdateOperatorUI(this);
        operatorUI.UpdateUI();
    }

    protected override void Die()
    {
        // ��� �� �۵��ؾ� �ϴ� ������ ���� ��?
        UnblockAllEnemies();

        // ������Ʈ �ı�
        Destroy(operatorUI.gameObject);
        base.Die();

        // �ϴ� UI Ȱ��ȭ
        OperatorManager.Instance.OnOperatorRemoved(data);
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
