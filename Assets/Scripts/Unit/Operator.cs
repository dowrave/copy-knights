using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Operator : DeployableUnitEntity, ICombatEntity, ISkill, IRotatable
{
    [SerializeField] // �ʵ� ����ȭ, Inspector���� �� �ʵ� �����
    public OperatorData data;

    // ICombatEntity �ʵ��
    public AttackType AttackType => data.attackType;
    public AttackRangeType AttackRangeType => data.attackRangeType;
    public float AttackPower => data.attackPower;
    public float AttackSpeed => data.attackSpeed; 
    public float AttackCooldown { get; private set; }


    // IRotatble
    [SerializeField, HideInInspector]
    public Vector3 facingDirection = Vector3.left;
    public Vector3 Direction
    {
        get => facingDirection;
        private set
        {
            facingDirection = value.normalized;
            transform.forward = facingDirection;
            UpdateDirectionIndicator(facingDirection);
        }
    }



    public GameObject OriginalPrefab { get; private set; }

    // ���� ����
    private List<Enemy> blockedEnemies; // ���� ���� ����
    public IReadOnlyList<Enemy> BlockedEnemies => blockedEnemies.AsReadOnly();

    public int deploymentOrder { get; private set; } // ��ġ ����
    private bool isDeployed = false; // ��ġ �Ϸ� �� true
    private Enemy currentTarget;
    private float attackCooldown = 0f; // data.baseStats���� ������ AttackSpeed ���� ���� ������

    // SP ����
    public float currentHealth => data.baseStats.health;
    // �ִ� ü��
    private float maxHealth;
    public float MaxHealth => maxHealth;
    private float currentSP;
    public float CurrentSP => currentSP;

    [SerializeField] private GameObject deployableBarUIPrefab;
    private DeployableBarUI deployableBarUI;

    // ���� ���� ���� �ִ� ���� 
    List<Enemy> enemiesInRange = new List<Enemy>();

    private SpriteRenderer directionIndicator;

    public event System.Action<float, float> OnHealthChanged; // ü�� ��ȭ �̺�Ʈ

    // �ʵ� �� --------------------------------------------------------

    private void Awake()
    {
        PrepareTransparentMaterial();
    }

    // ��ġ�� ��� ���� �ʱ�ȭ ����
    private void Start()
    {
        currentSP = data.initialSP; // SP �ʱ�ȭ
        
        
        CreateDirectionIndicator();
        InitializeStats();
    }

    public void Initialize(OperatorData operatorData)
    {
        data = operatorData;
    }

    //public void Initialize(GameObject prefab)
    //{
    //    OriginalPrefab = prefab;
    //    // ���� �ʱ�ȭ �ڵ�...
    //}

    public void SetDirection(Vector3 direction)
    {
        Direction = direction;
    }

    private void CreateOperatorBarUI()
    {
        if (deployableBarUIPrefab != null)
        {
            GameObject uiObject = Instantiate(deployableBarUIPrefab, transform);
            deployableBarUI = uiObject.GetComponentInChildren<DeployableBarUI>();
            deployableBarUI.Initialize(this);
        }
    }

    public void InitializeStats()
    {
        base.Initialize(data.stats);
        attackRangeType = data.stats.attackRangeType;
        blockedEnemies = new List<Enemy>(data.maxBlockableEnemies);
    }


    protected override void Update()
    {
        if (IsDeployed)
        {
            base.Update();

            ValidateCurrentTarget();

            // ���� ����� ���ٸ�
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

    public override void Attack(UnitEntity target)
    {
        if (!IsAttackCooldownComplete || !(target is Enemy enemy)) return;

        base.Attack(target); // ���� ������ ��, PerformAttack�� �����Ű�� ��ٿ ���۵�

    }

    protected override void PerformAttack(UnitEntity target)
    {
        switch (attackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target);
                break;
        }
    }

    private void PerformMeleeAttack(Unit target)
    {
        target.TakeDamage(stats.attackPower);
    }

    private void PerformRangedAttack(Unit target)
    {
        if (data.projectilePrefab != null)
        {
            // ����ü ���� ��ġ
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
            
            GameObject projectileObj = Instantiate(data.projectilePrefab, spawnPosition, Quaternion.identity);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(target, stats.attackPower);
            }
        }
    }


    // ���� Ÿ�Ͽ� �ִ� ������ ��ȯ��
    private void GetEnemiesInAttackRange()
    {
        enemiesInRange.Clear();
        Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);

        foreach (Vector2Int offset in data.attackableTiles)
        {
            Vector2Int rotatedOffset = RotateOffset(offset, facingDirection);
            Vector2Int targetGridPos = operatorGridPos + rotatedOffset;

            Tile targetTile = MapManager.Instance.CurrentMap.GetTile(targetGridPos.x, targetGridPos.y);
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

    public bool CanAttack(Vector3 targetPosition)
    {
        Vector2Int relativePosition = WorldToRelativeGridPosition(targetPosition);
        return System.Array.Exists(data.attackableTiles, tile => tile == relativePosition);
    }

    private Vector2Int WorldToRelativeGridPosition(Vector3 worldPosition)
    {
        if (MapManager.Instance.CurrentMap != null)
        {
            Vector2Int absoluteGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(worldPosition);
            Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);
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
        if (IsDeployed == false) { return;  }

        float oldSP = currentSP;
        if (data.autoRecoverSP)
        {
            currentSP = Mathf.Min(currentSP + data.SpRecoveryRate * Time.deltaTime, data.maxSP);    

        }

        if (currentSP != oldSP)
        {
            deployableBarUI.UpdateSPBar(currentSP, data.maxSP);
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

    public override void TakeDamage(AttackType attackType, float damage)
    {
        base.TakeDamage(attackType, damage);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    protected override void Die()
    {
        // ��� �� �۵��ؾ� �ϴ� ������ ���� ��?
        UnblockAllEnemies();

        // ������Ʈ �ı�
        Destroy(deployableBarUI.gameObject); // �ϴ� ü��/SP ��
        Destroy(directionIndicator.gameObject); // ���� ǥ�ñ�
        base.Die();

        // �ϴ� UI Ȱ��ȭ
        DeployableManager.Instance.OnDeployableRemoved(this);
    }

    public void UseSkill()
    {
        // ��ų ��� ����
        Debug.Log("��ų ��ư Ŭ����");
    }


    public override void OnClick()
    {
        base.OnClick();
        HighlightAttackRange();
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
        if (MapManager.Instance.CurrentMap == null) return;

        Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);
        List<Tile> tilesToHighlight = new List<Tile>();

        foreach (Vector2Int offset in data.attackableTiles)
        {
            Vector2Int rotatedIOffset = RotateOffset(offset, facingDirection);
            Vector2Int targetGridPos = operatorGridPos + rotatedIOffset;
            Tile targetTile = MapManager.Instance.CurrentMap.GetTile(targetGridPos.x, targetGridPos.y);
            if (targetTile != null)
            {
                tilesToHighlight.Add(targetTile);
            }
        }

        DeployableManager.Instance.HighlightTiles(tilesToHighlight, DeployableManager.Instance.attackRangeTileColor);
    }

    /// <summary>
    /// ���� Ÿ���� ��ȿ�� �˻� : currentTarget�� ���� ���� ���� ���ٸ� ������
    /// </summary>
    private void ValidateCurrentTarget()
    {
        if(currentTarget != null)
        {
            if (!IsTargetInRange(currentTarget))
            {
                currentTarget.RemoveAttackingOperator(this);
                currentTarget = null;
            }
        }
    }

    protected override bool IsTargetInRange(Unit unit)
    {
        if (unit is Enemy) // Ÿ�� ��Ī�� `is`�� ���
        {
            Vector2Int enemyGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(unit.transform.position);
            Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position); 

            foreach (Vector2Int offset in data.attackableTiles)
            {
                Vector2Int rotatedOffset = RotateOffset(offset, facingDirection);
                Vector2Int targetGridPos = operatorGridPos + rotatedOffset;

                if (targetGridPos == enemyGridPos)
                {
                    return true;
                }
            }

            // ���� ������ ����
            return false; 

        }

        // Enemy�� �ƴϴ�
        return false;
    }


    public override void Deploy(Vector3 position)
    {
        base.Deploy(position);

        maxHealth = data.stats.health;
        SetDirection(facingDirection);
        CreateOperatorBarUI();

        ShowDirectionIndicator(true);
    }

    public override void Retreat()
    {
        base.Retreat();
    }
}
