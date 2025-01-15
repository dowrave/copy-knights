using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using Skills.Base;
using static ICombatEntity;

public class Operator : DeployableUnitEntity, ICombatEntity, ISkill, IRotatable, ICrowdControlTarget
{
    public new OperatorData BaseData { get; protected set; } 
    [HideInInspector] public new OperatorStats currentStats; // �ϴ� public���� ����

    // ICombatEntity �ʵ�
    public AttackType AttackType => BaseData.attackType;
    public AttackRangeType AttackRangeType => BaseData.attackRangeType;

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

    // ICrowdControlTarget �ʵ�
    public float MovementSpeed => 0f;
    public Vector3 Position => transform.position; 
    public void SetMovementSpeed(float newMovementSpeed) { }

    public float AttackCooldown { get; protected set; }
    public float AttackDuration { get; protected set; }
   
    public List<Vector2Int> CurrentAttackbleTiles { get; set; }

    // ���� ���� ���� �ִ� ���� 
    protected List<Enemy> enemiesInRange = new List<Enemy>();

    // IRotatble �ʵ�
    protected Vector3 facingDirection = Vector3.left;
    public Vector3 FacingDirection
    {
        get => facingDirection;
        protected set
        {
            facingDirection = value.normalized;
            transform.forward = facingDirection;
        }
    }

    // ���� ����
    protected List<Enemy> blockedEnemies = new List<Enemy>();
    protected int nowBlockingCount = 0;

    public int DeploymentOrder { get; protected set; } // ��ġ ����
    protected bool isDeployed = false; // ��ġ �Ϸ� �� true
    public UnitEntity CurrentTarget { get; protected set; }

    protected float currentSP;
    public float CurrentSP 
    {
        get { return currentSP; }
        set 
        { 
            currentSP = Mathf.Clamp(value, 0f, MaxSP);
            OnSPChanged?.Invoke(CurrentSP, MaxSP);
        }
    }

    public float MaxSP { get; protected set; }

    [SerializeField] protected GameObject operatorUIPrefab;
    protected GameObject operatorUIInstance;
    protected OperatorUI operatorUIScript;
    protected SpriteRenderer directionIndicator; // ���� ǥ�� UI

    // ���Ÿ� ���� ������Ʈ Ǯ �ɼ�
    protected string projectileTag;

    // ����Ʈ Ǯ �±�
    string meleeAttackEffectTag;
    string hitEffectTag;

    // ��ų ����
    public BaseSkill CurrentSkill { get; private set; }
    public bool IsSkillOn { get; private set; }

    // �̺�Ʈ��
    public event System.Action<float, float> OnSPChanged;
    public event System.Action OnStatsChanged; 

    // �ʵ� �� --------------------------------------------------------------------------------------------
    public virtual void Initialize(OwnedOperator ownedOp)
    {
        // �⺻ ������ �ʱ�ȭ
        BaseData = ownedOp.BaseData;
        CurrentSP = BaseData.initialSP;

        // ���� ���� �ݿ�
        currentStats = ownedOp.currentStats;
        CurrentAttackbleTiles = new List<Vector2Int>(ownedOp.currentAttackableTiles);

        // ��ų ����
        CurrentSkill = ownedOp.selectedSkill;
        CurrentSP = ownedOp.currentStats.StartSP;
        MaxSP = CurrentSkill?.SPCost ?? 0f;

        IsPreviewMode = true;

        if (modelObject == null)
        {
            InitializeVisual();
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

        if (BaseData != null)
        {
            currentStats = BaseData.stats;
        }
    }

    public void SetDirection(Vector3 direction)
    {
        FacingDirection = direction;
    }

    protected void CreateOperatorUI()
    {
        if (operatorUIPrefab != null)
        {
            operatorUIInstance = Instantiate(operatorUIPrefab, transform);
            operatorUIScript = operatorUIInstance.GetComponent<OperatorUI>();
            operatorUIScript.Initialize(this);
        }
    }

    protected override void Update()
    {
        if (IsDeployed)
        {
            UpdateAttackTimings();
            RecoverSP();

            if (AttackDuration > 0) return;

            SetCurrentTarget(); // CurrentTarget ����
            ValidateCurrentTarget(); 

            if (CanAttack())
            {
                // ���� ������ �ٲ�� �ϴ� ��� ��ų�� ������ ����
                if (ShouldModifyAttackAction())
                {
                    CurrentSkill.PerformChangedAttackAction(this);
                }
                // �ƴ϶�� ��Ÿ
                else
                {
                    Attack(CurrentTarget, AttackPower);
                }

                SetAttackDuration();
                SetAttackCooldown();
            }

            if (CurrentSkill.autoActivate && CurrentSP == MaxSP)
            {
                UseSkill();
            }
        }

        base.Update();
    }


    // �������̽��� ���, �� �ȿ����� ����� �˾� ��Ҹ� �߰��ؼ� PerformAttack���� ����
    public virtual void Attack(UnitEntity target, float damage)
    {
        bool showDamagePopup = false;
        PerformAttack(target, damage, showDamagePopup);
    }

    protected void PerformAttack(UnitEntity target, float damage, bool showDamagePopup)
    {
        if (CurrentSkill != null)
        {
            CurrentSkill.OnAttack(this, ref damage, ref showDamagePopup);    
        }

        switch (BaseData.attackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target, damage, showDamagePopup);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target, damage, showDamagePopup);
                break;
        }
    }

    protected virtual void PerformMeleeAttack(UnitEntity target, float damage, bool showDamagePopup)
    {
        AttackSource attackSource = new AttackSource(transform.position, false);

        PlayMeleeAttackEffect(target);

        target.TakeDamage(this, attackSource, damage);
        if (showDamagePopup)
        {
            ObjectPoolManager.Instance.ShowFloatingText(target.transform.position, damage, false);
        }
    }

    protected virtual void PerformRangedAttack(UnitEntity target, float damage, bool showDamagePopup)
    {
        if (BaseData.projectilePrefab != null)
        {
            // ����ü ���� ��ġ
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;

            GameObject projectileObj = ObjectPoolManager.Instance.SpawnFromPool(projectileTag, spawnPosition, Quaternion.identity);
            if (projectileObj != null)
            {
                Projectile projectile = projectileObj.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(this, target, damage, showDamagePopup, projectileTag, BaseData.hitEffectPrefab);
                }
            }
        }
    }


    protected void GetEnemiesInAttackRange()
    {
        enemiesInRange.Clear();
        Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);

        // ���� ����(Ÿ�ϵ�)�� �ִ� ������ �����մϴ�
        foreach (Vector2Int offset in BaseData.attackableTiles)
        {
            Vector2Int rotatedOffset = DirectionSystem.RotateGridOffset(offset, facingDirection);
            Vector2Int targetGridPos = operatorGridPos + rotatedOffset;
            Tile targetTile = MapManager.Instance.CurrentMap.GetTile(targetGridPos.x, targetGridPos.y);
            if (targetTile != null)
            {
                List<Enemy> enemiesOnTile = targetTile.GetEnemiesOnTile();
                enemiesInRange.AddRange(enemiesOnTile);
            }
        }

        enemiesInRange = enemiesInRange.Distinct().ToList();
    }

    public void SetDeploymentOrder()
    {
        DeploymentOrder = DeployableManager.Instance.CurrentDeploymentOrder;
        DeployableManager.Instance.UpdateDeploymentOrder();
    }

    // --- ���� ���� �޼����
    public bool CanBlockEnemy(int enemyBlockCount)
    {
        return nowBlockingCount + enemyBlockCount <= currentStats.MaxBlockableEnemies;
    }

    // ���� �����ϴٸ� �� ������ + 1
    public void TryBlockEnemy(Enemy enemy)
    {
        int enemyBlockCount = enemy.BaseData.blockCount;
        if (CanBlockEnemy(enemyBlockCount))
        {
            blockedEnemies.Add(enemy);
            nowBlockingCount += enemyBlockCount;
        }
    }

    public void UnblockEnemy(Enemy enemy)
    {
        blockedEnemies.Remove(enemy);
        nowBlockingCount -= enemy.BaseData.blockCount;
    }

    public void UnblockAllEnemies()
    {
        foreach (Enemy enemy in blockedEnemies)
        {
            enemy.UnblockFrom(this);
        }
        blockedEnemies.Clear();
        nowBlockingCount = BaseData.stats.MaxBlockableEnemies;

    }

    // SP �ڵ�ȸ�� ���� �߰�
    protected void RecoverSP()
    {
        if (IsDeployed == false || CurrentSkill == null) { return;  }

        float oldSP = CurrentSP;

        if (CurrentSkill.autoRecover)
        {
            CurrentSP = Mathf.Min(CurrentSP + currentStats.SPRecoveryRate * Time.deltaTime, MaxSP);
        }

        if (CurrentSP != oldSP)
        {
            operatorUIScript.UpdateUI();
            OnSPChanged?.Invoke(CurrentSP, MaxSP);

            bool isSkillReady = CurrentSP >= MaxSP;
            operatorUIScript.SetSkillIconVisibility(isSkillReady);
        }
    }

    public override void TakeDamage(UnitEntity attacker, AttackSource attackSource, float damage)
    {
        base.TakeDamage(attacker, attackSource, damage);
        StatisticsManager.Instance.UpdateDamageTaken(this, damage);
    }

    protected override void Die()
    {
        // ��� �� ���� ����
        UnblockAllEnemies();

        // ������Ʈ �ı�
        Destroy(operatorUIInstance.gameObject);
        
        OnSPChanged = null;

        // ����Ʈ Ǯ ����
        if (BaseData.hitEffectPrefab != null)
        {
            ObjectPoolManager.Instance.RemovePool("Effect_" + BaseData.entityName);
        } 

        // �ϴ� UI Ȱ��ȭ
        DeployableManager.Instance.OnDeployableRemoved(this);

        base.Die();
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

    public void HighlightAttackRange()
    {
        Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);
        List<Tile> tilesToHighlight = new List<Tile>();

        foreach (Vector2Int offset in BaseData.attackableTiles)
        {
            Vector2Int rotatedOffset = DirectionSystem.RotateGridOffset(offset, facingDirection);
            Vector2Int targetGridPos = operatorGridPos + rotatedOffset;
            Tile targetTile = MapManager.Instance.CurrentMap.GetTile(targetGridPos.x, targetGridPos.y);
            if (targetTile != null)
            {
                tilesToHighlight.Add(targetTile);
            }
        }

        DeployableManager.Instance.HighlightTiles(tilesToHighlight, DeployableManager.Instance.attackRangeTileColor);
    }

    /// ���� Ÿ���� ��ȿ�� �˻�
    protected virtual void ValidateCurrentTarget()
    {
        if (CurrentTarget == null)
        {
            return;
        }
           
        // �������� ��� ���
        if (!IsCurrentTargetInRange())
        {
            CurrentTarget.RemoveAttackingEntity(this);
            CurrentTarget = null;
        }
    }

    /// CurrentTarget�� �̵����� ��, ���ݹ��� ���� �ִ��� üũ
    protected bool IsCurrentTargetInRange()
    {
        Vector2Int targetGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(CurrentTarget.transform.position);
        Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);

        foreach (Vector2Int offset in BaseData.attackableTiles)
        {
            Vector2Int rotatedOffset = DirectionSystem.RotateGridOffset(offset, facingDirection);
            Vector2Int inRangeGridPos = operatorGridPos + rotatedOffset;

            if (inRangeGridPos == targetGridPos)
            {
                return true;
            }
        }
        return false; 
    }


    public override void Deploy(Vector3 position)
    {
        base.Deploy(position);

        if (BaseData.deployEffectPrefab != null)
        {
            GameObject deployEffect = Instantiate(
                BaseData.deployEffectPrefab,
                transform.position,
                Quaternion.identity
            );

            var vfx = deployEffect.GetComponent<VisualEffect>();
            if (vfx != null)
            {
                vfx.Play();
                Destroy(deployEffect, 1.2f); // 1�� �� �ı�. 
            }
        }

        // ����Ʈ �ı��� ��ٸ��� �ʰ� ������
        IsPreviewMode = false;
        SetDeploymentOrder();
        SetDirection(facingDirection);
        UpdateAttackbleTiles();
        CreateOperatorUI();
        CurrentSP = currentStats.StartSP;

        // ����Ʈ ������Ʈ Ǯ ����
        CreateObjectPool();
    }

    public override void Retreat()
    {
        Die();
    }

    // ISkill �޼���
    public bool CanUseSkill()
    {
        return IsDeployed && CurrentSP >= MaxSP;
    }

    protected override void InitializeHP()
    {
        MaxHealth = currentStats.Health;
        CurrentHealth = MaxHealth;
    }


    // ���� ��� ���� ����
    public virtual void SetCurrentTarget()
    {
        // 1. ���� ���� �� -> ���� ���� ��
        if (blockedEnemies.Count > 0)
        {
            for (int i = 0; i < blockedEnemies.Count; i++)
            {
                if (blockedEnemies[i])
                {
                    CurrentTarget = blockedEnemies[i];
                    break;
                }
            }

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


    // ���� ��� ���� ����
    public void RemoveCurrentTarget()
    {
        if (CurrentTarget == null) return;

        CurrentTarget.RemoveAttackingEntity(this);
        CurrentTarget = null;
    }

    public void UpdateAttackTimings()
    {
        UpdateAttackDuration();
        UpdateAttackCooldown();
    }

    // ICombatEntity �޼����
    public void NotifyTarget()
    {
        CurrentTarget.AddAttackingEntity(this);
    }

    // ���� ��� 
    public void UpdateAttackDuration()
    {
        if (AttackDuration > 0f)
        {
            AttackDuration -= Time.deltaTime;
        }
    }

    // ���� ���� ���� �ð�
    public void UpdateAttackCooldown()
    {
        if (AttackCooldown > 0f)
        {
            AttackCooldown -= Time.deltaTime;
        }
    }

    // ���� ���
    public void SetAttackDuration()
    {
        AttackDuration = 0.3f / AttackSpeed;
    }

    // ���� ���ݱ����� ��� �ð�
    public void SetAttackCooldown(float? intentionalCooldown = null)
    {
        if (intentionalCooldown.HasValue)
        {
            AttackCooldown = intentionalCooldown.Value;
        }
        else
        {
            AttackCooldown = 1 / AttackSpeed;
        }
    }

    public bool CanAttack()
    {
        return IsDeployed &&
            CurrentTarget != null &&
            AttackCooldown <= 0 &&
            AttackDuration <= 0 && 
            IsCurrentTargetInRange(); // ���� ���� ���� ����
    }

    public void UseSkill()
    {
        if (CanUseSkill() && CurrentSkill != null)
        {
            CurrentSkill.Activate(this);
            UpdateOperatorUI();
        }
    }

    protected void UpdateAttackbleTiles()
    {
        CurrentAttackbleTiles = BaseData.attackableTiles
            .Select(tile => DirectionSystem.RotateGridOffset(tile, FacingDirection))
            .ToList();
    }

    // ��ų ��� �� SP Bar ���� ����
    public void StartSkillDurationDisplay(float duration)
    {
        UpdateOperatorUI();
    }

    public void UpdateSkillDurationDisplay(float remainingPercentage)
    {
        CurrentSP = MaxSP * remainingPercentage;
        UpdateOperatorUI();
    }

    public void EndSkillDurationDisplay()
    {
        UpdateOperatorUI();
    }

    protected void UpdateOperatorUI()
    {
        if (operatorUIScript != null)
        {
            operatorUIScript.UpdateUI();
        }
    }


    // ������ ������Ʈ Ǯ���� �ִٸ� ����� ����
    private void CreateObjectPool()
    {
        // ���� ���� ����Ʈ Ǯ ����
        if (BaseData.meleeAttackEffectPrefab != null)
        {
            meleeAttackEffectTag = BaseData.entityName + BaseData.meleeAttackEffectPrefab.name;
            ObjectPoolManager.Instance.CreatePool(
                meleeAttackEffectTag,
                BaseData.meleeAttackEffectPrefab
            );
        }

        // �ǰ� ����Ʈ Ǯ ����
        if (BaseData.hitEffectPrefab != null)
        {
            hitEffectTag = BaseData.entityName + BaseData.hitEffectPrefab.name;
            ObjectPoolManager.Instance.CreatePool(
                hitEffectTag,
                BaseData.hitEffectPrefab
            );
        }

        // ���Ÿ��� ��� ����ü Ǯ ����
        InitializeProjectilePool();

        // ��ų ����Ʈ Ǯ ����
        CurrentSkill.InitializeSkillObjectPool();
    }

    private void PlayMeleeAttackEffect(UnitEntity target)
    {
        // ����Ʈ ó��
        if (BaseData.meleeAttackEffectPrefab != null)
        {
            GameObject effectObj = ObjectPoolManager.Instance.SpawnFromPool(
                   meleeAttackEffectTag,
                   transform.position,
                   Quaternion.identity
           );
            VisualEffect vfx = effectObj.GetComponent<VisualEffect>();

            // ������ �ִٸ� ���� ���
            if (vfx != null && vfx.HasVector3("BaseDirection"))
            {
                Vector3 baseDirection = vfx.GetVector3("BaseDirection");
                Vector3 attackDirection = (target.transform.position - transform.position).normalized;
                Quaternion rotation = Quaternion.FromToRotation(baseDirection, attackDirection);
                effectObj.transform.rotation = rotation;

                vfx.Play();
            }

            StartCoroutine(ReturnEffectToPool(meleeAttackEffectTag, effectObj, 1f));
        }
    }

    private void RemoveObjectPool()
    {
        if (meleeAttackEffectTag != null)
        {
            ObjectPoolManager.Instance.RemovePool(meleeAttackEffectTag);
        }

        if (hitEffectTag != null)
        {
            ObjectPoolManager.Instance.RemovePool(hitEffectTag);
        }

        RemoveProjectilePool();
        CurrentSkill.CleanupSkillObjectPool();
    }

    public void InitializeProjectilePool()
    {
        if (AttackRangeType == AttackRangeType.Ranged)
        {
            projectileTag = $"{BaseData.entityName}_Projectile";
            ObjectPoolManager.Instance.CreatePool(projectileTag, BaseData.projectilePrefab, 5);
        }
    }

    protected void RemoveProjectilePool()
    {
        if (AttackRangeType == AttackRangeType.Ranged && !string.IsNullOrEmpty(projectileTag))
        {
            ObjectPoolManager.Instance.RemovePool(projectileTag);
        }
    }

    private bool ShouldModifyAttackAction()
    {
        return CurrentSkill.modifiesAttackAction && IsSkillOn;
    }

    // ���� �ð��� �ִ� ��ų�� �Ѱų� �� �� ȣ���
    public void SetSkillOnState(bool skillOnState)
    {
        IsSkillOn = skillOnState;
    }


    protected void OnDestroy()
    {
        RemoveObjectPool();
    }
}
