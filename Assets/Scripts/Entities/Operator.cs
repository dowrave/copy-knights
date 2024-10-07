using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using Skills.Base;

public class Operator : DeployableUnitEntity, ICombatEntity, ISkill, IRotatable
{
    [SerializeField]
    private OperatorData operatorData;
    public new OperatorData Data { get => operatorData; private set => operatorData = value; }

    public new OperatorStats currentStats;

    // ICombatEntity �ʵ�
    public AttackType AttackType => operatorData.attackType;
    public AttackRangeType AttackRangeType => operatorData.attackRangeType;

    public float AttackPower
    {
        get => currentStats.AttackPower;
        set
        {
            if (currentStats.AttackPower != value)
            {
                currentStats.AttackPower = value;
                OnStatsChanged?.Invoke();
            }
        }
    }

    public float AttackSpeed
    {
        get => currentStats.AttackSpeed;
        set
        {
            if (currentStats.AttackSpeed != value)
            {
                currentStats.AttackSpeed = value;
                OnStatsChanged?.Invoke();
            }
        }
    }

    public float Defense
    {
        get => currentStats.Defense;
        set
        {
            if (currentStats.Defense != value)
            {
                currentStats.Defense = value;
                OnStatsChanged?.Invoke();
            }
        }
    }

    public float MagicResistance
    {
        get => currentStats.MagicResistance;
        set
        {
            if (currentStats.MagicResistance != value)
            {
                currentStats.MagicResistance = value;
                OnStatsChanged?.Invoke();
            }
        }
    }

    public int MaxBlockableEnemies
    {
        get => currentStats.MaxBlockableEnemies;
        set
        {
            if (currentStats.MaxBlockableEnemies != value)
            {
                currentStats.MaxBlockableEnemies = value;
                OnStatsChanged?.Invoke();
            }
        }
    }

    public float AttackCooldown { get; private set; }
    public float AttackDuration { get; private set; }
   
    public Vector2Int[] CurrentAttackbleTiles { get; set; }

    // ���� ���� ���� �ִ� ���� 
    List<Enemy> enemiesInRange = new List<Enemy>();

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
    private List<Enemy> blockedEnemies = new List<Enemy>(); // ���� ���� ����. Awake ������ �ʱ�ȭ��.
    public IReadOnlyList<Enemy> BlockedEnemies => blockedEnemies.AsReadOnly();

    public int DeploymentOrder { get; private set; } // ��ġ ����
    private bool isDeployed = false; // ��ġ �Ϸ� �� true
    public UnitEntity CurrentTarget { get; private set; }


    public float CurrentSP 
    {
        get { return currentStats.CurrentSP; }
        private set { currentStats.CurrentSP = Mathf.Clamp(value, 0f, MaxSP); }
    }

    public float MaxSP { get; private set; }

    [SerializeField] private GameObject operatorUIPrefab;
    private GameObject operatorUIInstance;
    private OperatorUI operatorUIScript;

    //[SerializeField] private GameObject deployableBarUIPrefab;
    //private DeployableBarUI deployableBarUI; // ü��, SP
    private SpriteRenderer directionIndicator; // ���� ǥ�� UI

    // ���Ÿ� ���� ������Ʈ Ǯ �ɼ�
    protected int initialPoolSize = 5;
    protected string projectileTag; 


    // ��ų ����
    private List<Skill> skills;
    private Skill activeSkill;
    public Skill ActiveSkill => activeSkill;

    // �̺�Ʈ��
    public event System.Action<float, float> OnSPChanged;
    public event System.Action OnStatsChanged; 

    // �ʵ� �� --------------------------------------------------------

    protected override void Awake()
    {
        base.Awake();
    }

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

        CreateDirectionIndicator(); // ���� ǥ�ñ� ����

        // ����ϴ� ��ų ����
        skills = Data.skills;
        SetActiveSkill(operatorData.defaultSkillIndex);

        // MaxSP�� ����
        if (activeSkill != null)
        {
            MaxSP = activeSkill.SPCost;
        }

        // ���Ÿ� ����ü ������Ʈ Ǯ �ʱ�ȭ
        if (AttackRangeType == AttackRangeType.Ranged)
        {
            InitializeProjectilePool();
        }
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

    private void CreateOperatorUI()
    {
        if (operatorUIPrefab != null)
        {
            operatorUIInstance = Instantiate(operatorUIPrefab, transform);
            operatorUIScript = operatorUIInstance.GetComponent<OperatorUI>();
            operatorUIScript.Initialize(this);
        }
    }

    public void Update()
    {
        if (IsDeployed)
        {
            UpdateAttackTimings();

            if (AttackDuration > 0) return;

            SetCurrentTarget(); // CurrentTarget ����
            ValidateCurrentTarget(); 

            if (CanAttack())
            {
                Attack(CurrentTarget, AttackType, AttackPower);
            }

            RecoverSP();
        }
    }

    protected void OnDestroy()
    {
        if (AttackRangeType == AttackRangeType.Ranged)
        {
            CleanupProjectilePool();
        }
    }

    public void Attack(UnitEntity target, AttackType attackType, float damage)
    {
        PerformAttack(target, attackType, damage);
    }

    private void PerformAttack(UnitEntity target, AttackType attackType, float damage)
    {
        switch (operatorData.attackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target, attackType, damage);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target, attackType, damage);
                break;
        }
    }

    private void PerformMeleeAttack(UnitEntity target, AttackType attackType, float damage)
    {
        SetAttackTimings();
        target.TakeDamage(attackType, damage);
    }

    private void PerformRangedAttack(UnitEntity target, AttackType attackType, float damage)
    {
        SetAttackTimings();
        if (Data.projectilePrefab != null)
        {
            // ����ü ���� ��ġ
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;

            GameObject projectileObj = ObjectPoolManager.Instance.SpawnFromPool(projectileTag, spawnPosition, Quaternion.identity);
            if (projectileObj != null)
            {
                Projectile projectile = projectileObj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(target, attackType, damage, projectileTag);
                }
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

    public void SetDeploymentOrder()
    {
        DeploymentOrder = DeployableManager.Instance.CurrentDeploymentOrder;
        DeployableManager.Instance.UpdateDeploymentOrder();
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
        if (Data.autoRecoverSP)
        {
            CurrentSP = Mathf.Min(CurrentSP + currentStats.SPRecoveryRate * Time.deltaTime, MaxSP);    

        }

        if (CurrentSP != oldSP)
        {
            operatorUIScript.UpdateSPBar(CurrentSP, MaxSP);
            OnSPChanged?.Invoke(CurrentSP, MaxSP);

            bool isSkillReady = CurrentSP >= MaxSP;
            operatorUIScript.SetSkillIconVisibility(isSkillReady);
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
        //Destroy(deployableBarUI.gameObject); // �ϴ� ü��/SP ��
        Destroy(operatorUIInstance.gameObject);
        Destroy(directionIndicator.gameObject); // ���� ǥ�ñ�

        base.Die();

        // �ϴ� UI Ȱ��ȭ
        DeployableManager.Instance.OnDeployableRemoved(this);
    }

    public override void OnClick()
    {
        base.OnClick();
        if (IsDeployed)
        {
            HighlightAttackRange();
        }
    }

    // ���� ����� ���� �׾��� �� �۵���. ���� ������ ������ ����
    public void OnTargetLost(Enemy enemy)
    {
        // ���� ��󿡼� ����
        if (CurrentTarget == enemy)
        {
            CurrentTarget = null;
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
    /// ���� Ÿ���� ��ȿ�� �˻� : CurrentTarget�� ���� ���� ���� ���ٸ� ������
    /// </summary>
    private void ValidateCurrentTarget()
    {
        if (CurrentTarget == null) return;

        // �������� ��� ���
        if (!IsCurrentTargetInRange())
        {
            CurrentTarget.RemoveAttackingEntity(this);
            CurrentTarget = null;
        }
        
    }

    /// <summary>
    /// CurrentTarget�� ���ݹ��� ���� �ִ��� üũ
    /// </summary>
    protected bool IsCurrentTargetInRange()
    {
        if (CurrentTarget == null) return false;

        Vector2Int enemyGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(CurrentTarget.transform.position);
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

        return false; 
    }


    public override void Deploy(Vector3 position)
    {
        base.Deploy(position);
        SetDeploymentOrder();
        SetDirection(facingDirection);
        UpdateAttackbleTiles();
        //CreateOperatorBarUI();
        CreateOperatorUI();
        ShowDirectionIndicator(true);
    }

    public override void Retreat()
    {
        base.Retreat();
    }

    // ISkill �޼���
    public bool CanUseSkill()
    {
        return IsDeployed && CurrentSP >= MaxSP;
    }

    /// <summary>
    /// Data, Stat�� ��ƼƼ���� �ٸ��� ������ �ڽ� �޼��忡�� �����ǰ� �׻� �ʿ�
    /// </summary>
    protected override void InitializeUnitProperties()
    {
        UpdateCurrentTile();
        Prefab = Data.prefab; 
    }

    protected override void InitializeHP()
    {
        MaxHealth = currentStats.Health;
        CurrentHealth = MaxHealth;
    }

    /// <summary>
    /// ���� ��� ���� ����
    /// </summary>
    public void SetCurrentTarget()
    {
        // 1. ���� ���� �� -> ���� ���� ��
        if (blockedEnemies.Count > 0)
        {
            CurrentTarget = blockedEnemies[0];
            NotifyTarget();
            return;
        }

        GetEnemiesInAttackRange(); // ���� ���� ���� ���� ����

        // 2. ���� ���� �ƴ� ������ ���� ���� ���� �� �߿��� ������
        if (enemiesInRange.Count > 0)
        {
            CurrentTarget = enemiesInRange.OrderBy(E => E.GetRemainingPathDistance()).FirstOrDefault();
            if (CurrentTarget != null)
            {
                NotifyTarget();
            }
            return;
        }

        // ���� ���� ���� ����, ���� ���� ���� ���� ���ٸ� ���� Ÿ���� ����
        CurrentTarget = null;
    }

    /// <summary>
    /// ���� ��� ���� ����
    /// </summary>
    public void RemoveCurrentTarget()
    {
        if (CurrentTarget == null) return;

        CurrentTarget.RemoveAttackingEntity(this);
        CurrentTarget = null;
    }

    /// <summary>
    /// CurrentTarget���� �ڽ��� �����ϰ� ������ �˸�
    /// </summary>
    public void NotifyTarget()
    {
        CurrentTarget.AddAttackingEntity(this);
    }

    public void InitializeProjectilePool()
    {
        projectileTag = $"{Data.entityName}_Projectile";
        ObjectPoolManager.Instance.CreatePool(projectileTag, Data.projectilePrefab, initialPoolSize);
    }

    private void CleanupProjectilePool()
    {
        if (!string.IsNullOrEmpty(projectileTag))
        {
            ObjectPoolManager.Instance.RemovePool(projectileTag);
        }
    }

    // ICombatEntity �޼����

    /// <summary>
    /// Update�� ����, 
    /// �� �� ��� ���� ������ �۵��Ѵ�.
    /// </summary>
    public void UpdateAttackTimings()
    {
        UpdateAttackDuration();
        UpdateAttackCooldown();
    }

    public void UpdateAttackDuration()
    {
        if (AttackDuration > 0f)
        {
            AttackDuration -= Time.deltaTime;
        }
    }

    public void UpdateAttackCooldown()
    {
        if (AttackCooldown > 0f)
        {
            AttackCooldown -= Time.deltaTime;
        }
    }


    // ���� ��� �ð�, ���� ��Ÿ�� �ð� ����
    public void SetAttackTimings()
    {
        if (AttackDuration <= 0f)
        {
            SetAttackDuration();
        }
        if (AttackCooldown <= 0f)
        {
            SetAttackCooldown();
        }
    }

    public void SetAttackDuration()
    {
        AttackDuration = 0.3f / AttackSpeed;
    }

    public void SetAttackCooldown()
    {
        AttackCooldown = 1 / AttackSpeed;
    }

    public bool CanAttack()
    {
        return IsDeployed &&
            CurrentTarget != null &&
            AttackCooldown <= 0 &&
            AttackDuration <= 0 && 
            IsCurrentTargetInRange(); // ���� ���� ���� ����
    }

    // ��ų ����
    public void SetActiveSkill(int index)
    {
        if (index >= 0 && index < skills.Count)
        {
            activeSkill = skills[index];
        }
    }

    public void UseSkill()
    {
        if (CanUseSkill() && activeSkill != null)
        {
            activeSkill.Activate(this);
            CurrentSP -= activeSkill.SPCost;
            // SP �Ҹ� �� ��ٿ� ���� �߰�
        }
    }

    private void UpdateAttackbleTiles()
    {
        CurrentAttackbleTiles = Data.attackableTiles
            .Select(tile => RotateOffset(tile, FacingDirection))
            .ToArray();
    }
}
