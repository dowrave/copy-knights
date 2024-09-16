using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;


public class Operator : DeployableUnitEntity, ICombatEntity, ISkill, IRotatable
{
    [SerializeField]
    private OperatorData operatorData;
    public new OperatorData Data { get => operatorData; private set => operatorData = value; }

    public new OperatorStats currentStats;

    // ICombatEntity �ʵ�
    public AttackType AttackType => operatorData.attackType;
    public AttackRangeType AttackRangeType => operatorData.attackRangeType;

    public float AttackPower { get => currentStats.AttackPower; private set => currentStats.AttackPower = value; }
    public float AttackSpeed { get => currentStats.AttackSpeed; private set => currentStats.AttackSpeed = value; }
    public float AttackCooldown { get; private set; }


    // IRotatble �ʵ�
    private Vector3 facingDirection = Vector3.left;
    public Vector3 FacingDirection
    {
        get => facingDirection;
        private set
        {
            facingDirection = value.normalized;
            transform.forward = facingDirection;
            UpdateDirectionIndicator(facingDirection);
        }
    }

    // ���� ����
    private List<Enemy> blockedEnemies; // ���� ���� ����
    public IReadOnlyList<Enemy> BlockedEnemies => blockedEnemies.AsReadOnly();
    public int MaxBlockableEnemies { get => currentStats.MaxBlockableEnemies; private set => currentStats.MaxBlockableEnemies = value; }

    public int deploymentOrder { get; private set; } // ��ġ ����
    private bool isDeployed = false; // ��ġ �Ϸ� �� true
    private UnitEntity currentTarget;


    public float CurrentSP 
    {
        get { return currentStats.CurrentSP; }
        set
        {
            currentStats.CurrentSP = Mathf.Clamp(value, 0f, operatorData.maxSP);
            OnSPChanged?.Invoke(CurrentSP, operatorData.maxSP);
        }
    }

    [SerializeField] private GameObject deployableBarUIPrefab;
    private DeployableBarUI deployableBarUI;

    // ���� ���� ���� �ִ� ���� 
    List<Enemy> enemiesInRange = new List<Enemy>();

    private SpriteRenderer directionIndicator;

    // SP ���� �̺�Ʈ
    public event System.Action<float, float> OnSPChanged;


    // �ʵ� �� --------------------------------------------------------

    public void Initialize(OperatorData operatorData)
    {
        InitializeOperatorData(operatorData);
        InitializeUnitProperties();
        InitializeDeployableProperties();
        InitializeOperatorProperties();
    }

    private void InitializeOperatorData(OperatorData operatorData)
    {
        
        currentStats = operatorData.stats;
        if (Data == null)
        {
            Debug.LogError("Data�� null��!!!");
        }
    }


    // ���۷����� ���� ������ �ʱ�ȭ
    private void InitializeOperatorProperties()
    {
        CreateDirectionIndicator(); 
    }

    public override void InitializeFromPrefab()
    {
        if (modelObject == null)
        {
            modelObject = transform.Find("Model").gameObject;
        }
        if (modelObject != null)
        {
            modelRenderer = modelObject.GetComponent<Renderer>();
        }
        // DeployableUnitData �ʱ�ȭ (���� SerializeField�� �����Ǿ� �ִٸ� �̹� �Ҵ�Ǿ� ����)
        if (operatorData != null)
        {
            currentStats = operatorData.stats;
        }
    }

    public void SetDirection(Vector3 direction)
    {
        FacingDirection = direction;
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

    public void Update()
    {
        if (IsDeployed)
        {
            if (AttackCooldown > 0)
            {
                UpdateAttackCooldown();
            }
            RecoverSP();

            ValidateCurrentTarget();

            // ���� ����� ���ٸ�
            if (currentTarget == null)
            {
                FindTarget(); // currentTarget ������Ʈ �õ�
            }

            if (CanAttack())
            {
                Attack(currentTarget, AttackType, AttackPower);
            }

        }
    }

    // ������ ���� ���� ���۷����͸� �˸�
    private void SetAndNotifyTarget(UnitEntity newTarget)
    {
        if (currentTarget != null)
        {
            newTarget.RemoveAttackingEntity(this);
        }

        if (currentTarget != null)
        {
            currentTarget = newTarget;
            newTarget.AddAttackingEntity(this);
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

    public void Attack(UnitEntity target, AttackType attackType, float attackPower)
    {
        if (AttackCooldown > 0 || !(target is Enemy enemy)) return;
        PerformAttack(target, attackType, attackPower);
    }

    private void PerformAttack(UnitEntity target, AttackType attackType, float attackPower)
    {
        switch (operatorData.attackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target, attackType, attackPower);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target, attackType, attackPower);
                break;
        }
    }

    private void PerformMeleeAttack(UnitEntity target, AttackType attackType, float attackPower)
    {
        target.TakeDamage(attackType, attackPower);
    }

    private void PerformRangedAttack(UnitEntity target, AttackType attackType, float attackPower)
    {
        if (operatorData.projectilePrefab != null)
        {
            // ����ü ���� ��ġ
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
            
            GameObject projectileObj = Instantiate(operatorData.projectilePrefab, spawnPosition, Quaternion.identity);
            Projectile projectile = projectileObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(target, attackType, attackPower);
            }
        }
    }


    // ���� Ÿ�Ͽ� �ִ� ������ ��ȯ��
    private void GetEnemiesInAttackRange()
    {
        enemiesInRange.Clear();
        Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);

        foreach (Vector2Int offset in operatorData.attackableTiles)
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
        return operatorData.attackableTiles;
    }

    public bool CanAttack(Vector3 targetPosition)
    {
        Vector2Int relativePosition = WorldToRelativeGridPosition(targetPosition);
        return System.Array.Exists(operatorData.attackableTiles, tile => tile == relativePosition);
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
        return blockedEnemies.Count < currentStats.MaxBlockableEnemies;
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

        float oldSP = CurrentSP;
        if (operatorData.autoRecoverSP)
        {
            CurrentSP = Mathf.Min(CurrentSP + currentStats.SPRecoveryRate * Time.deltaTime, operatorData.maxSP);    

        }

        if (CurrentSP != oldSP)
        {
            deployableBarUI.UpdateSPBar(CurrentSP, operatorData.maxSP);
            //operatorUI.UpdateOperatorUI(this);
        }
    }

    public bool TryUseSkill(float spCost)
    {
        if (CurrentSP >= spCost)
        {
            CurrentSP -= spCost;

            return true;
        }
        return false; 
    }

    public override void TakeDamage(AttackType attackType, float damage)
    {
        base.TakeDamage(attackType, damage);
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

    /// <summary>
    /// ���� ǥ�� UI ����
    /// </summary>
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

        foreach (Vector2Int offset in operatorData.attackableTiles)
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
                currentTarget.RemoveAttackingEntity(this);
                currentTarget = null;
            }
        }
    }

    protected bool IsTargetInRange(UnitEntity unit)
    {
        if (unit is Enemy) // Ÿ�� ��Ī�� `is`�� ���
        {
            Vector2Int enemyGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(unit.transform.position);
            Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position); 

            foreach (Vector2Int offset in operatorData.attackableTiles)
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

        //maxHealth = operatorData.currentStats.health;
        SetDirection(facingDirection);
        CreateOperatorBarUI();

        ShowDirectionIndicator(true);
    }

    public override void Retreat()
    {
        base.Retreat();
    }

    // ICombatEntity �޼��� - �������̽� ����� ��� public���� �����ؾ� ��
    public bool CanAttack()
    {
        return currentTarget != null && AttackCooldown <= 0;
    }

    public void SetAttackCooldown()
    {
        AttackCooldown = 1 / AttackCooldown;
    }

    public void UpdateAttackCooldown()
    {
        if (AttackCooldown > 0f)
        {
            AttackCooldown -= Time.deltaTime;
        }
    }

    // ISkill �޼���
    public bool CanUseSkill()
    {
        return CurrentSP == operatorData.maxSP;
    }

    protected override void InitializeUnitProperties()
    {
        MaxHealth = currentStats.Health;
        CurrentHealth = MaxHealth;
        UpdateCurrentTile();
        Prefab = Data.prefab; // �̰� Ÿ�� ������ �������̵���
    }
}
