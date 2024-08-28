using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public class Operator : Unit, IClickable
{

    [SerializeField] // �ʵ� ����ȭ, Inspector���� �� �ʵ� �����
    public OperatorData data;

    [SerializeField, HideInInspector]
    public Vector3 facingDirection = Vector3.left;

    // ���� ����
    private List<Enemy> blockedEnemies; // ���� ���� ���� ����
    public IReadOnlyList<Enemy> BlockedEnemies => blockedEnemies.AsReadOnly();

    public int deploymentOrder { get; private set; } // ��ġ ����
    private bool isDeployed = false; // ��ġ �Ϸ� �� true
    private Map currentMap;
    private Enemy currentTarget;
    private float attackCooldown = 0f; // data.baseStats���� ������ AttackSpeed ���� ���� ������
    //[HideInInspector] public bool isBlocking = false; // ���� ���ΰ�

    // SP ����
    public float currentHealth => stats.Health;
    // �ִ� ü��
    private float maxHealth;
    public float MaxHealth => maxHealth;
    private float currentSP;
    public float CurrentSP => currentSP;

    [SerializeField] private GameObject operatorUIPrefab;
    private OperatorUI operatorUI;

    // ���� ���� ���� �ִ� ���� 
    List<Enemy> enemiesInRange = new List<Enemy>();

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

    private SpriteRenderer directionIndicator;

    // �ʵ� �� --------------------------------------------------------

    private void Awake()
    {
        PrepareTransparentMaterial();
        currentMap = FindObjectOfType<Map>();
    }

    // ��ġ�� ��� ���� �ʱ�ȭ ����
    private void Start()
    {
        currentSP = data.initialSP; // SP �ʱ�ȭ
        attackRangeType = data.attackRangeType;
        CreateDirectionIndicator();
        InitializeStats();
    }

    public void Deploy(Vector3 position, Vector3 direction)
    {
        if (!isDeployed)
        {
            isDeployed = true;
            transform.position = position;
            SetDirection(direction);

            maxHealth = data.baseStats.Health;
            //currentMap = FindObjectOfType<Map>();

            CreateOperatorUI();
        }
    }

    public void SetDirection(Vector3 direction)
    {
        facingDirection = direction.normalized;
        transform.forward = facingDirection;
        UpdateDirectionIndicator(facingDirection);
    }

    private void CreateOperatorUI()
    {
        if (operatorUIPrefab != null)
        {
            GameObject uiObject = Instantiate(operatorUIPrefab, transform);
            operatorUI = uiObject.GetComponentInChildren<OperatorUI>();
            operatorUI.Initialize(this);
        }
    }

    public void InitializeStats()
    {
        base.Initialize(data.baseStats);
        attackRangeType = data.attackRangeType;
        blockedEnemies = new List<Enemy>(data.maxBlockableEnemies);
    }


    private void Update()
    {
        if (isDeployed)
        {
            attackCooldown -= Time.deltaTime;



            // ���� ���
            if (currentTarget == null)
            {
                FindTarget(); // currentTarget ������Ʈ �õ�
            }

            if (attackCooldown <= 0 && currentTarget != null)
            {
                Attack(currentTarget);
            }

            RecoverSP();
        }


    }

    // ������ ���� ���� ���۷����͸� �˸�
    private void SetAndNotifyTarget(Enemy newTarget)
    {
        if (currentTarget != null)
        {
            newTarget.RemoveAttackingOperator(this);
        }

        if (currentTarget != null)
        {
            currentTarget = newTarget;
            newTarget.AddAttackingOperator(this);
        }
    }

    // currentTarget ���� ����
    private void FindTarget()
    {
        // 1. ���� ���� ������ ���� ���� �� �߿��� ����
        if (blockedEnemies.Count > 0)
        {
            currentTarget = blockedEnemies[0]; // ù ��° ������ ���� Ÿ������
            SetAndNotifyTarget(currentTarget);
            return;
        }

        GetEnemiesInAttackRange(); // ���� ���� ���� ���� ����

        // 2. ���� ���� �ƴ� ������ ���� ���� ���� �� �߿��� ������
        // enemiesInRange ������Ʈ
        if (enemiesInRange.Count > 0)
        {
            string enemiesInfo = string.Join(", ", enemiesInRange.Select((enemy, index) =>
                $"Enemy {index}: {enemy.name} (Health: {enemy.CurrentHealth}/{enemy.MaxHealth}, Position: {enemy.transform.position})"));

            currentTarget = enemiesInRange[0];
            SetAndNotifyTarget(currentTarget);
            return;
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
    private void GetEnemiesInAttackRange()
    {
        enemiesInRange.Clear();
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

        enemiesInRange = enemiesInRange.Distinct().ToList(); // �ߺ� �����ؼ� ��ȯ
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
        return blockedEnemies.Count < data.maxBlockableEnemies;
    }

    // ���� �����ϴٸ� �� ������ + 1
    public bool TryBlockEnemy(Enemy enemy)
    {
        if (CanBlockEnemy())
        {
            blockedEnemies.Add(enemy);
            Debug.Log($"���� ����: {enemy}");
            return true;
        }
        return false;
    }

    public void UnblockEnemy(Enemy enemy)
    {
        Debug.LogWarning("�� ���� ����");
        blockedEnemies.Remove(enemy);
    }

    public void UnblockAllEnemies()
    {
        blockedEnemies.Clear();
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
        Destroy(operatorUI.gameObject); // �ϴ� ü��/SP ��
        Destroy(directionIndicator.gameObject); // ���� ǥ�ñ�
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

    public void ShowActionUI()
    {
        OperatorManager.Instance.ShowActionUI(this);
    }

    public void UseSkill()
    {
        // ��ų ��� ����
        Debug.Log("��ų ��ư Ŭ����");
    }

    public void Retreat()
    {
        Debug.Log("�� ��ư Ŭ����");

        // ���� �ʿ�) ��� vs ���� ���̰� �ʿ� - ���� ��ȯ ��ġ �ڽ�Ʈ�� �ִ�
        OperatorManager.Instance.OnOperatorRemoved(data);

        Destroy(gameObject);

    }

    /// <summary>
    /// ���۷����Ͱ� Ŭ���Ǿ��� ���� ���� 
    /// </summary>
    public void OnClick()
    {
        if (isDeployed && !IsPreviewMode && StageManager.Instance.currentState == GameState.Battle)
        {
            OperatorManager.Instance.CancelPlacement(); // ���۷����͸� Ŭ���ߴٸ� ���� ���� ���� ��ġ ������ ��ҵǾ�� ��

            // �̸����� ���¿��� �����ϸ� �ȵ�
            if (IsPreviewMode == false)
            {
                UIManager.Instance.ShowOperatorInfo(data, transform.position);
            }

            HighlightAttackRange();
            ShowActionUI();
        }
    }

    // ���� ����� ���� �׾��� �� �۵���. ���� ������ ������ ����
    public void OnTargetLost(Enemy enemy)
    {

        // ���� ��󿡼� ����
        if (currentTarget == enemy)
        {
            currentTarget = null;
        }

        // ���� �� �� ����Ʈ���� ����
        enemiesInRange.Remove(enemy); // ���ϸ� ����Ʈ�� �ı��� ������Ʈ�� ���Ƽ� 0�� �ε����� ĳġ���� ����
    }

    private void CreateDirectionIndicator()
    {
        GameObject indicator = new GameObject("DirectionIndicator");
        indicator.transform.SetParent(transform);
        indicator.transform.localPosition = new Vector3(0, -0.1f, 0);
        indicator.transform.localRotation = Quaternion.Euler(90, 0, -90);
        indicator.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

        directionIndicator = indicator.AddComponent<SpriteRenderer>();
        directionIndicator.sprite = Resources.Load<Sprite>("direction_sprite");
        directionIndicator.enabled = false;
    }

    public void UpdateDirectionIndicator(Vector3 direction)
    {
        if (directionIndicator != null)
        {
            float angle = Vector3.SignedAngle(Vector3.left, direction, Vector3.up);

            // x�� ȸ�� : �ٴڿ� ������ / z�� �߽����� -angle��ŭ ȸ����Ű�� ������ ����(�׽�Ʈ �Ϸ�)
            directionIndicator.transform.localRotation = Quaternion.Euler(90, 0, -90);
        }
    }

    public void ShowDirectionIndicator(bool show)
    {
        if (directionIndicator != null)
        {
            directionIndicator.enabled = show;
        }
    }

    public void HighlightAttackRange()
    {
        if (currentMap == null) return;

        Vector2Int operatorGridPos = currentMap.WorldToGridPosition(transform.position);
        List<Tile> tilesToHighlight = new List<Tile>();

        foreach (Vector2Int offset in data.attackableTiles)
        {
            Vector2Int rotatedIOffset = RotateOffset(offset, facingDirection);
            Vector2Int targetGridPos = operatorGridPos + rotatedIOffset;
            Tile targetTile = currentMap.GetTile(targetGridPos.x, targetGridPos.y);
            if (targetTile != null)
            {
                tilesToHighlight.Add(targetTile);
            }
        }

        OperatorManager.Instance.HighlightTiles(tilesToHighlight, OperatorManager.Instance.attackRangeTileColor);
    }
}
