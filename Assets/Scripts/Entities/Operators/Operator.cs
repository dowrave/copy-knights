using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using Skills.Base;
using static ICombatEntity;

public class Operator : DeployableUnitEntity, ICombatEntity, ISkill, IRotatable, ICrowdControlTarget
{
    public new OperatorData BaseData { get; protected set; } 
    public new OperatorStats currentStats; // �ϴ� public���� ����

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

    public float AttackCooldown { get; protected set; }
    public float AttackDuration { get; protected set; }

    // ��ġ�� ȸ��
    private Vector2Int operatorGridPos;
    private List<Vector2Int> baseOffsets; // �⺻ ������
    private List<Vector2Int> rotatedOffsets; // ȸ�� �ݿ� ������
    public List<Vector2Int> CurrentAttacakbleGridPos { get; set; } // ȸ�� �ݿ� ���� ����(gridPosition), public set�� ��ų ������

    // ���� ���� ���� �ִ� ���� 
    protected List<Enemy> enemiesInRange = new List<Enemy>();

    public Vector3 FacingDirection { get; protected set; } = Vector3.left;

    // ���� ����
    protected List<Enemy> blockedEnemies = new List<Enemy>();
    public IReadOnlyList<Enemy> BlockedEnemies => blockedEnemies;

    public int DeploymentOrder { get; protected set; } // ��ġ ����
    //protected bool isDeployed = false; // ��ġ �Ϸ� �� true

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

    // ���Ÿ� ���� ������Ʈ Ǯ �ɼ�
    protected string projectileTag;

    // ����Ʈ Ǯ �±�
    private string meleeAttackEffectTag;
    private string hitEffectTag;

    // ��ų ����
    public BaseSkill CurrentSkill { get; private set; }
    public bool IsSkillOn { get; private set; }

    // �̺�Ʈ��
    public event System.Action<float, float> OnSPChanged;
    public event System.Action OnStatsChanged;
    public event System.Action<Operator> OnOperatorDied;

    public virtual void Initialize(OwnedOperator ownedOp)
    {
        // �⺻ ������ �ʱ�ȭ
        BaseData = ownedOp.BaseData;
        CurrentSP = BaseData.initialSP;

        // ���� ���� �ݿ�
        currentStats = ownedOp.CurrentStats;

        // ȸ�� �ݿ�
        baseOffsets = new List<Vector2Int>(ownedOp.CurrentAttackableGridPos); // ���� ���� ����

        // ��ų ����
        CurrentSkill = ownedOp.StageSelectedSkill;
        CurrentSP = ownedOp.CurrentStats.StartSP;
        MaxSP = CurrentSkill?.SPCost ?? 0f;

        SetDeployState(false);
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

    protected void Update()
    {
        if (IsDeployed && StageManager.Instance.currentState == GameState.Battle)
        {
            UpdateAttackDuration();
            UpdateAttackCooldown();
            RecoverSP();
            UpdateCrowdControls(); // CC ȿ�� ����

            if (activeCC.Any(cc => cc is StunEffect)) return;
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

    }


    // �������̽��� ���, PerformAttack���� ����
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
        AttackSource attackSource = new AttackSource(transform.position, false, BaseData.HitEffectPrefab);

        PlayMeleeAttackEffect(target, attackSource);

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
        foreach (Vector2Int eachPos in CurrentAttacakbleGridPos)
        {
            Tile targetTile = MapManager.Instance.CurrentMap.GetTile(eachPos.x, eachPos.y);
            if (targetTile != null)
            {
                List<Enemy> enemiesOnTile = targetTile.GetEnemiesOnTile();
                enemiesInRange.AddRange(enemiesOnTile);
            }
        }

        enemiesInRange = enemiesInRange.Distinct().ToList();
    }

    public void SetGridPosition()
    {
        operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);
    }

    public void SetDeploymentOrder()
    {
        DeploymentOrder = DeployableManager.Instance.CurrentDeploymentOrder;
        DeployableManager.Instance.UpdateDeploymentOrder();
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

            // ���� �ߵ��� ��ų�� ��ų ���� ���� ���¸� ���
            if (!CurrentSkill.autoActivate)
            {
                bool isSkillReady = CurrentSP >= MaxSP;
                operatorUIScript.SetSkillIconVisibility(isSkillReady);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // ���۷����Ϳ��� �浹
        Enemy collidedEnemy = other.GetComponent<Enemy>();

        if (collidedEnemy != null && 
            CanBlockEnemy(collidedEnemy.BlockCount) && // �� ���۷����Ͱ� �� ���� ������ �� ���� �� 
            collidedEnemy.BlockingOperator == null) // �ش� ���� ���� ���� �Ʊ� ���۷����Ͱ� ���� �� 
        {
            BlockEnemy(collidedEnemy); // ���� ����
            collidedEnemy.SetBlockingOperator(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Enemy collidedEnemy = other.GetComponent<Enemy>();

        if (collidedEnemy != null && collidedEnemy.BlockingOperator == this)
        {
            UnblockEnemy(collidedEnemy);
            collidedEnemy.SetBlockingOperator(null);
        }
    }

    // --- ���� ���� �޼����
    // ���� ������ Enemy�� �����ϴ� ���� ���� �����ؾ� �ؼ� �̷��� ������ (���� ��ü 3������ ���� ���� �Ŵϱ�)
    public bool CanBlockEnemy(int enemyBlockCount)
    {
        return IsDeployed &&
            blockedEnemies.Count + enemyBlockCount <= currentStats.MaxBlockableEnemies;
    }

    public void BlockEnemy(Enemy enemy)
    {
        blockedEnemies.Add(enemy);
    }

    public void UnblockEnemy(Enemy enemy)
    {
        blockedEnemies.Remove(enemy);
    }

    public void UnblockAllEnemies()
    {
        foreach (Enemy enemy in blockedEnemies)
        {
            enemy.UnblockFrom(this);
        }
        blockedEnemies.Clear();
    }

    public override void TakeDamage(UnitEntity attacker, AttackSource attackSource, float damage)
    {
        base.TakeDamage(attacker, attackSource, damage);
        StatisticsManager.Instance.UpdateDamageTaken(BaseData, damage);
    }

    protected override void Die()
    {
        // ��ġ�Ǿ�� Die�� ����
        if (!IsDeployed) return;

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

        // ���۷����� ��� �̺�Ʈ �߻�
        OnOperatorDied?.Invoke(this);

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
    public void OnTargetLost(UnitEntity unit)
    {
        // ���� ��󿡼� ����
        if (CurrentTarget == unit)
        {
            // ���� �� �� ����Ʈ���� ����
            if (unit is Enemy enemy)
            {
                enemiesInRange.Remove(enemy); // ���ϸ� ����Ʈ�� �ı��� ������Ʈ�� ���Ƽ� 0�� �ε����� ĳġ���� ����
                Debug.Log($"{enemy} ���, {BaseData.entityName}�� enemiesInRange�� ����  : {enemiesInRange.Count}");
            }

            CurrentTarget = null;
        }
    }

    public void HighlightAttackRange()
    {
        List<Tile> tilesToHighlight = new List<Tile>();

        if (!IsDeployed)
        {
            Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);
            UpdateAttackableTiles();
        }


        foreach (Vector2Int eachPos in CurrentAttacakbleGridPos)
        {
            Tile targetTile = MapManager.Instance.CurrentMap.GetTile(eachPos.x, eachPos.y);
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
        if (CurrentTarget == null) return;
        if (blockedEnemies.Contains(CurrentTarget)) return;

        // �������� ��� ���
        if (!IsCurrentTargetInRange())
        {
            CurrentTarget.RemoveAttackingEntity(this);
            CurrentTarget = null;
        }
    }
    
    // Target�� ���� ���� ���� Ÿ�Ͽ� �ִ��� üũ
    protected bool IsCurrentTargetInRange()
    {
        foreach (Vector2Int eachPos in CurrentAttacakbleGridPos)
        {
            Tile eachTile = MapManager.Instance.GetTile(eachPos.x, eachPos.y);
            if (eachTile.EnemiesOnTile.Contains(CurrentTarget))
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

        IsPreviewMode = false;
        SetDeploymentOrder();
        operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);
        SetDirection(FacingDirection);
        UpdateAttackableTiles();
        CreateOperatorUI();
        CurrentSP = currentStats.StartSP;

        // ����Ʈ ������Ʈ Ǯ ����
        CreateObjectPool();
    }

    public override void Retreat()
    {
        // ���� �� �� ���� ��ġ �ڽ�Ʈ�� ���� ȸ��
        int recoverCost = (int)Mathf.Round(currentStats.DeploymentCost / 2f);
        StageManager.Instance.RecoverDeploymentCost(recoverCost);

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
        if (CurrentTarget != null)
        {
            CurrentTarget.RemoveAttackingEntity(this);
            CurrentTarget = null;
        }
    }

    // ICombatEntity �޼����
    public void NotifyTarget()
    {
        if (CurrentTarget != null)
        {
            CurrentTarget.AddAttackingEntity(this);
        }
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
            AttackDuration <= 0;
    }

    public void UseSkill()
    {
        if (CanUseSkill() && CurrentSkill != null)
        {
            CurrentSkill.Activate(this);
            UpdateOperatorUI();
        }
    }

    // ��ġ ��ġ�� ȸ���� ����� ���� ������ gridPos�� ������
    protected void UpdateAttackableTiles()
    {
        // �ʱ�ȭ�� baseOffset�� ���� ����� ����.
        rotatedOffsets = new List<Vector2Int>(baseOffsets
            .Select(tile => DirectionSystem.RotateGridOffset(tile, FacingDirection))
            .ToList());
        CurrentAttacakbleGridPos = new List<Vector2Int>();

        foreach (Vector2Int offset in rotatedOffsets)
        {
            Vector2Int inRangeGridPosition = operatorGridPos + offset;
            CurrentAttacakbleGridPos.Add(inRangeGridPosition);
        }
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

    private void PlayMeleeAttackEffect(UnitEntity target, AttackSource attackSource)
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
            CombatVFXController combatVFXController = effectObj.GetComponent<CombatVFXController>();
            combatVFXController.Initialize(attackSource, target, meleeAttackEffectTag);
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
        CurrentSkill.CleanupSkill();
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



    protected override float CalculateActualDamage(AttackType attacktype, float incomingDamage)
    {
        float actualDamage = 0; // �Ҵ���� �ʼ�

        switch (attacktype)
        {
            case AttackType.Physical:
                actualDamage = incomingDamage - currentStats.Defense;
                break;
            case AttackType.Magical:
                actualDamage = incomingDamage * (1 - currentStats.MagicResistance / 100);
                break;
            case AttackType.True:
                actualDamage = incomingDamage;
                break;
        }
        return Mathf.Max(actualDamage, 0.05f * incomingDamage); // ���� ������� 5%�� ���Բ� ����
    }

    protected void OnDestroy()
    {
        RemoveObjectPool();
    }
    public void SetMovementSpeed(float newMovementSpeed) { }

}
