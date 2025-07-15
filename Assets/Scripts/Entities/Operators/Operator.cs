using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using Skills.Base;
using static ICombatEntity;
using Unity.VisualScripting;

public class Operator : DeployableUnitEntity, ICombatEntity, ISkill, IRotatable, ICrowdControlTarget
{
    public OperatorData OperatorData { get; protected set; } = default!;
    [HideInInspector] 
    public OperatorStats currentOperatorStats; // �ϴ� public���� ����

    public string EntityName => OperatorData.entityName;

    // ICombatEntity �ʵ�
    public AttackType AttackType => OperatorData.attackType;
    public AttackRangeType AttackRangeType => OperatorData.attackRangeType;

    public float AttackPower
    {
        get => currentOperatorStats.AttackPower;
        set
        {
            if (currentOperatorStats.AttackPower != value)
            {
                currentOperatorStats.AttackPower = value;
                OnStatsChanged?.Invoke();
            }
        }
    }
    public float AttackSpeed
    {
        get => currentOperatorStats.AttackSpeed;
        set
        {
            if (currentOperatorStats.AttackSpeed != value)
            {
                currentOperatorStats.AttackSpeed = value;
                OnStatsChanged?.Invoke();
            }
        }
    }


    public float Defense
    {
        get => currentOperatorStats.Defense;
        set
        {
            if (currentOperatorStats.Defense != value)
            {
                currentOperatorStats.Defense = value;
                OnStatsChanged?.Invoke();
            }
        }
    }

    public float MagicResistance
    {
        get => currentOperatorStats.MagicResistance;
        set
        {
            if (currentOperatorStats.MagicResistance != value)
            {
                currentOperatorStats.MagicResistance = value;
                OnStatsChanged?.Invoke();
            }
        }
    }

    public int MaxBlockableEnemies
    {
        get => currentOperatorStats.MaxBlockableEnemies;
        set
        {
            if (currentOperatorStats.MaxBlockableEnemies != value)
            {
                currentOperatorStats.MaxBlockableEnemies = value;
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
    private List<Vector2Int> baseOffsets = new List<Vector2Int>(); // �⺻ ������
    private List<Vector2Int> rotatedOffsets = new List<Vector2Int>(); // ȸ�� �ݿ� ������
    public List<Vector2Int> CurrentAttackableGridPos { get; set; } = new List<Vector2Int>(); // ȸ�� �ݿ� ���� ����(gridPosition), public set�� ��ų ������

    // ���� ���� ���� �ִ� ���� 
    protected List<Enemy> enemiesInRange = new List<Enemy>();

    public Vector3 FacingDirection { get; protected set; } = Vector3.left;

    // ���� ����
    protected List<Enemy> blockedEnemies = new List<Enemy>(); // ������ ���� ���� ����
    public IReadOnlyList<Enemy> BlockedEnemies => blockedEnemies;
    protected int currentBlockCount; // ���� ���� ��

    protected List<Enemy> blockableEnemies = new List<Enemy>(); // �ݶ��̴��� ���ļ� ������ ������ ����
    public IReadOnlyList<Enemy> BlockableEnemies => blockableEnemies;


    public int DeploymentOrder { get; protected set; } = 0;// ��ġ ����

    public UnitEntity? CurrentTarget { get; protected set; }

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

    [SerializeField] protected GameObject operatorUIPrefab = default!;
    protected GameObject operatorUIInstance = default!;
    protected OperatorUI? operatorUI;
    public OperatorUI? OperatorUI => operatorUI;

    // ���Ÿ� ���� ������Ʈ Ǯ �ɼ�
    protected string? projectileTag;

    // ����Ʈ Ǯ �±�
    protected string? meleeAttackEffectTag;
    protected string hitEffectTag = string.Empty;
    public string HitEffectTag => hitEffectTag;

    // ��ų ����
    public BaseSkill CurrentSkill { get; private set; } = default!;
    private bool _isSkillOn;
    public bool IsSkillOn
    {
        get => _isSkillOn;
        private set
        {
            if (_isSkillOn != value)
            {
                _isSkillOn = value;
                OnSkillStateChanged?.Invoke();
            }
        }
    }
    private Coroutine _activeSkillCoroutine; // ���ӽð��� �ִ� ��ų�� �ش��ϴ� �ڷ�ƾ

    // ���� ���۷������� ���� ���� - Current�� ������ �������� �ʰ���
    public OperatorGrowthSystem.ElitePhase ElitePhase { get; private set; }
    public int Level { get; private set; }

    // �̺�Ʈ��
    public event System.Action<float, float> OnSPChanged = delegate { };
    public event System.Action OnStatsChanged = delegate { };
    public event System.Action<Operator> OnOperatorDied = delegate { };
    public event System.Action OnSkillStateChanged = delegate { };

    public new virtual void Initialize(DeployableManager.DeployableInfo opInfo)
    {
        DeployableInfo = opInfo;
        if (opInfo.ownedOperator != null)
        {
            OwnedOperator ownedOp = opInfo.ownedOperator;

            // �⺻ ������ �ʱ�ȭ
            OperatorData = ownedOp.OperatorProgressData;
            CurrentSP = OperatorData.initialSP;

            // ���� ���� �ݿ�
            currentOperatorStats = ownedOp.CurrentStats;

            // ȸ�� �ݿ�
            baseOffsets = new List<Vector2Int>(ownedOp.CurrentAttackableGridPos); // ���� ���� ����

            // ��ų ����
            if (opInfo.skillIndex.HasValue)
            {
                CurrentSkill = ownedOp.UnlockedSkills[opInfo.skillIndex.Value];
            }
            else
            {
                throw new System.InvalidOperationException("�ε����� ��� CurrentSkill�� �������� ����");
            }

            MaxSP = CurrentSkill?.SPCost ?? 0f;

            ElitePhase = ownedOp.currentPhase;
            Level = ownedOp.currentLevel;

            SetDeployState(false);
        }
        else
        {
            Debug.LogError("���۷������� ownedOperator ������ ����!");
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
            operatorUIInstance = Instantiate(operatorUIPrefab);
            operatorUI = operatorUIInstance.GetComponent<OperatorUI>();
            if (operatorUI != null)
            {
                operatorUI.Initialize(this);
            }
        }
    }
    
    protected void DestroyOperatorUI()
    {
        // ������Ʈ�� �ı��ϴ� �� �ƴ϶� ������Ʈ�� �ı��ؾ� ��
        Destroy(operatorUI.gameObject);
    }

    protected void Update()
    {
        if (IsDeployed && StageManager.Instance!.currentState == GameState.Battle)
        {
            // ----- ���� ���� ����. �ൿ ����� ���� -----
            UpdateAttackDuration();
            UpdateAttackCooldown();

            HandleSPRecovery(); // SP ȸ��
            UpdateCrowdControls(); // CC ȿ�� ����

            // �ൿ �Ҵ� ���� üũ
            if (activeCC.Any(cc => cc is StunEffect)) return; // ���� ȿ�� ���� �� �Ʒ� ������ ����

            // ----- �ൿ ���� ������ ���� -----
            // ������ �ƿ� ���ϴ� ��Ȳ�� �����Ѵ�. ���� ��� ���� �����ε� ��ų�� �ڵ����� �� ���� ����.
            HandleSkillAutoActivate();

            if (AttackDuration > 0) return; // ���� ��� �߿��� Ÿ�� ����/���� �Ұ���
            // ����) ���� ����� �ƴ����� ��ٿ��� ���� Ÿ���� ��� �����Ѵ�. �׷��� AttackCooldown ������ ���� ����.

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
                    Attack(CurrentTarget!, AttackPower);
                }
            }
        }
    }

    protected virtual void HandleSkillAutoActivate()
    {
        if (CurrentSkill != null && CurrentSkill.autoActivate && CanUseSkill())
            {
                UseSkill();
            }
    }


    // �������̽��� ���, PerformAttack���� ����
    public virtual void Attack(UnitEntity target, float damage)
    {
        bool showDamagePopup = false;
        float polishedDamage = Mathf.Floor(damage);
        PerformAttack(target, polishedDamage, showDamagePopup);

        // ���� ��� ���̶�� �ð� ����
        SetAttackDuration();

        // ���� ��ٿ� ����
        SetAttackCooldown();
    }

    protected virtual void PerformAttack(UnitEntity target, float damage, bool showDamagePopup)
    {
        float spBeforeAttack = CurrentSP;

        if (CurrentSkill != null)
        {
            CurrentSkill.OnBeforeAttack(this, ref damage, ref showDamagePopup);
        }
    
        switch (OperatorData.attackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target, damage, showDamagePopup);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target, damage, showDamagePopup);
                break;
        }

        if (CurrentSkill != null)
        {
            CurrentSkill.OnAfterAttack(this);

            // SP ���� �� ȸ�� ����
            if (!CurrentSkill.autoRecover && // �ڵ�ȸ���� �ƴϸ鼭
                    !IsSkillOn && 
                    spBeforeAttack != MaxSP) // ��ų�� �Ƿ��� ���� ������ ���� SP ȸ�� X
            {
                CurrentSP += 1; // ���Ϳ� Clamp�� �����Ƿ� ���⼭ ���� �ʾƵ� ��.
            }
        }
    }

    protected virtual void PerformMeleeAttack(UnitEntity target, float damage, bool showDamagePopup)
    {
        AttackSource attackSource = new AttackSource(transform.position, false, OperatorData.HitEffectPrefab, hitEffectTag);

        PlayMeleeAttackEffect(target, attackSource);

        target.TakeDamage(attackSource);
        if (showDamagePopup)
        {
            ObjectPoolManager.Instance!.ShowFloatingText(target.transform.position, damage, false);
        }
    }

    protected virtual void PerformRangedAttack(UnitEntity target, float damage, bool showDamagePopup)
    {
        if (OperatorData.projectilePrefab != null)
        {
            // ����ü ���� ��ġ
            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;

            if (projectileTag != null)
            {
                GameObject? projectileObj = ObjectPoolManager.Instance!.SpawnFromPool(projectileTag, spawnPosition, Quaternion.identity);
                if (projectileObj != null)
                {
                    Projectile? projectile = projectileObj.GetComponent<Projectile>();
                    if (projectile != null)
                    {
                        projectile.Initialize(this, target, damage, showDamagePopup, projectileTag, OperatorData.hitEffectPrefab, hitEffectTag);
                    }
                }
            }
        }
    }

    public void SetGridPosition()
    {
        operatorGridPos = MapManager.Instance!.CurrentMap!.WorldToGridPosition(transform.position);
    }

    public void SetDeploymentOrder()
    {
        DeploymentOrder = DeployableManager.Instance!.CurrentDeploymentOrder;
        DeployableManager.Instance!.UpdateDeploymentOrder();
    }

    // SP �ڵ�ȸ�� ���� �߰�
    protected void HandleSPRecovery()
    {
        if (IsDeployed == false || CurrentSkill == null) { return; }

        // �ڵ�ȸ���� ���� ó��
        if (CurrentSkill.autoRecover)
        {
            float oldSP = CurrentSP;

            // �ִ� SP �ʰ� ���� (�̺�Ʈ�� ��ü �߻�)
            CurrentSP = Mathf.Min(CurrentSP + currentOperatorStats.SPRecoveryRate * Time.deltaTime, MaxSP);
        }
        
        // ����ȸ�� ��ų�� ���� �ÿ� ȸ���ǹǷ� ���⼭ ó������ ����
    }

    public override void OnBodyTriggerEnter(Collider other)
    {
        // ���� ������ ��ü �ݶ��̴��� �浹�� ���� �߻�
        BodyColliderController body = other.GetComponent<BodyColliderController>();

        // Enemy�� ���� ���� ���� ����
        if (body != null && body.ParentUnit is Enemy enemy)
        {
            if (!blockableEnemies.Contains(enemy))
            {
                blockableEnemies.Add(enemy);
                TryBlockNextEnemy();
            }
        }
    }

    // ���� ������ ������ ���� �� blockableEnemies���� ���� ���� ã�� �����Ѵ�.
    private void TryBlockNextEnemy()
    {
        // ������ ���ٸ� ����
        if (currentBlockCount >= MaxBlockableEnemies) return;

        // ���� ������ �� ����� ��ȸ, ����Ʈ ���� = ���� ���� ��
        foreach (Enemy candidateEnemy in blockableEnemies)
        {
            // �� ���� ������ �� �ִ��� Ȯ��
            if (CanBlockEnemy(candidateEnemy))
            {
                BlockEnemy(candidateEnemy);
                candidateEnemy.UpdateBlockingOperator(this);
            }
        }
    }


    public override void OnBodyTriggerExit(Collider other)
    {
        // if (!IsDeployed) return;
        // ���� ������ ��ü �ݶ��̴��� �浹�� ���� �߻�
        BodyColliderController body = other.GetComponent<BodyColliderController>();

        // ���� body�� ���� ���� ���� ����
        if (body != null && body.ParentUnit is Enemy enemy)
        {
            blockableEnemies.Remove(enemy);

            // ���� ���� ���̾��ٸ� ���� ����
            if (blockedEnemies.Contains(enemy))
            {
                UnblockEnemy(enemy);
                enemy.UpdateBlockingOperator(null);
            }

            // �ٸ� �� ���� �õ�
            TryBlockNextEnemy();
        }
    }

    // �ش� ���� ������ �� �ִ°�
    private bool CanBlockEnemy(Enemy enemy)
    {
        return enemy != null && 
            IsDeployed &&
            !blockedEnemies.Contains(enemy) && // �̹� ���� ���� ���� �ƴ�
            enemy.BlockingOperator == null &&  // ���� �����ϰ� �ִ� ���۷����Ͱ� ����
            currentBlockCount + enemy.BlockCount <= MaxBlockableEnemies; // �� ���� �������� �� �ִ� ���� ���� �ʰ����� ����
    }

    private void BlockEnemy(Enemy enemy)
    {
        blockedEnemies.Add(enemy);
        currentBlockCount += enemy.BlockCount;
    }

    private void UnblockEnemy(Enemy enemy)
    {
        blockedEnemies.Remove(enemy);
        currentBlockCount -= enemy.BlockCount;
    }

    public void UnblockAllEnemies()
    {
        // ToList�� ���� ���� : �ݺ��� �ȿ��� ����Ʈ�� �����ϸ� ������ �߻��� �� �ִ� - ���纻�� ��ȸ�ϴ� �� �����ϴ�.
        foreach (Enemy enemy in blockedEnemies.ToList())
        {
            enemy.UpdateBlockingOperator(null);
            UnblockEnemy(enemy);
        }
    }

    // �θ� Ŭ�������� ���ǵ� TakeDamage���� ����
    protected override void OnDamageTaken(UnitEntity attacker, float actualDamage)
    {
        StatisticsManager.Instance!.UpdateDamageTaken(OperatorData, actualDamage);
    }

    protected override void Die()
    {
        // ��ġ�Ǿ�� Die�� ����
        if (!IsDeployed) return;

        // ���� ���� Ÿ�� ����
        UnregisterTiles();

        // ��ų ���� ���̾��ٸ� �ڷ�ƾ ��� 
        if (_activeSkillCoroutine != null)
        {
            StopCoroutine(_activeSkillCoroutine);
            _activeSkillCoroutine = null;
        }

        // ��� �� ���� ����
        UnblockAllEnemies();

        // UI �ı�
        DestroyOperatorUI();
        
        // �ʿ����� �𸣰ھ �ϴ� �ּ�ó��
        //OnSPChanged = null;

        // ����Ʈ Ǯ ����
        if (OperatorData.hitEffectPrefab != null)
        {
            ObjectPoolManager.Instance!.RemovePool("Effect_" + OperatorData.entityName);
        }

        // ��ġ�� ��ҿ��� ����
        DeployableInfo.deployedOperator = null;

        // ���۷����� ��� �̺�Ʈ �߻�
        OnOperatorDied?.Invoke(this);

        // �̺�Ʈ ���� ����
        Enemy.OnEnemyDespawned -= HandleEnemyDespawn;

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

    public void HighlightAttackRange()
    {
        List<Tile> tilesToHighlight = new List<Tile>();

        if (!IsDeployed)
        {
            Vector2Int operatorGridPos = MapManager.Instance!.CurrentMap!.WorldToGridPosition(transform.position);
            UpdateAttackableTiles();
        }


        foreach (Vector2Int eachPos in CurrentAttackableGridPos)
        {
            Tile? targetTile = MapManager.Instance!.CurrentMap!.GetTile(eachPos.x, eachPos.y);
            if (targetTile != null)
            {
                tilesToHighlight.Add(targetTile);
            }
        }

        HighlightAttackRanges(tilesToHighlight);
    }

    protected virtual void HighlightAttackRanges(List<Tile> tiles)
    {
        DeployableManager.Instance!.HighlightAttackRanges(tiles, false);
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
        foreach (Vector2Int eachPos in CurrentAttackableGridPos)
        {
            Tile? eachTile = MapManager.Instance!.GetTile(eachPos.x, eachPos.y);
            if (eachTile != null && eachTile.EnemiesOnTile.Contains(CurrentTarget))
            {
                return true;
            }
        }
        return false; 
    }

    public override void Deploy(Vector3 position)
    {
        ClearStates();

        base.Deploy(position);

        IsPreviewMode = false;
        SetDeploymentOrder();
        operatorGridPos = MapManager.Instance!.CurrentMap!.WorldToGridPosition(transform.position);
        SetDirection(FacingDirection);
        RegisterTiles();
        UpdateAttackableTiles();

        CreateDirectionIndicator();
        CreateOperatorUI();

        // deployableInfo�� ��ġ�� ���۷����͸� �̰����� ����
        DeployableInfo.deployedOperator = this;

        // ����Ʈ ������Ʈ Ǯ ����
        CreateObjectPool();

        // �� ��� �̺�Ʈ ����
        Enemy.OnEnemyDespawned += HandleEnemyDespawn;

        // ��ġ VFX
        if (OperatorData.deployEffectPrefab != null)
        {
            GameObject deployEffect = Instantiate(
                OperatorData.deployEffectPrefab,
                transform.position,
                Quaternion.identity
            );

            var vfx = deployEffect.GetComponent<VisualEffect>();
            if (vfx != null)
            {
                vfx.Play();
                Destroy(deployEffect, 1.2f); // 1.2�� �� �ı�, �Ʒ��� ������ ���� �ʴ´�.
            }
        }

    }

    private void ClearStates()
    {
        enemiesInRange.Clear();
        blockedEnemies.Clear();
        blockableEnemies.Clear();
        currentBlockCount = 0;
        CurrentTarget = null; 
    }

    // �׽�Ʈ) �� �ı� �̺�Ʈ�� �޾� ���۷����Ϳ����� ó���� �۾���
    private void HandleEnemyDespawn(Enemy enemy, DespawnReason reason)
    {
        // 1. ���� Ÿ���̶�� Ÿ�� ����
        if (CurrentTarget == enemy)
        {
            CurrentTarget = null;
        }

        // 2. ���� ���� ���� �ش� ����� �ִٸ� ���� ������ ����
        if (enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Remove(enemy);
        }

        // 3. ���� ���� ���, ���� ���� ����� �� ����
        // OnTriggerExit�� ��ģ ���¿����� �ı��� �������� ���ϹǷ� ������ ó���� �ʿ���
        if (blockableEnemies.Contains(enemy))
        {
            blockableEnemies.Remove(enemy);
            if (blockedEnemies.Contains(enemy))
            {
                UnblockEnemy(enemy);
                TryBlockNextEnemy();
            }
        }
    }

    public override void Retreat()
    {
        RecoverInitialCost();
        Die();
    }

    // ���� �� �� ���� ��ġ �ڽ�Ʈ�� ���� ȸ��
    private void RecoverInitialCost()
    {
        int recoverCost = (int)Mathf.Round(currentOperatorStats.DeploymentCost / 2f);
        StageManager.Instance!.RecoverDeploymentCost(recoverCost);
    }

    // ISkill �޼���
    public bool CanUseSkill()
    {
        return IsDeployed && CurrentSP >= MaxSP && !IsSkillOn;
    }

    protected override void InitializeHP()
    {
        MaxHealth = Mathf.Floor(currentOperatorStats.Health);
        CurrentHealth = Mathf.Floor(MaxHealth);
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

        // 2. ���� ���� �ƴ� ������ ���� ���� ���� �� �߿��� ������
        if (enemiesInRange.Count > 0)
        {
            CurrentTarget = enemiesInRange
                .Where(e => e != null && e.gameObject != null) // �ı� �˻� & null �˻� �Բ� ����
                .OrderBy(E => E.GetRemainingPathDistance()) // ����ִ� ��ü �� ���� �Ÿ��� ª�� ������ ����
                .FirstOrDefault(); // ���� ª�� �Ÿ��� ��ü�� ������

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
    public void SetAttackDuration(float? intentionalCooldown = null)
    {
        if (intentionalCooldown.HasValue)
        {
            AttackDuration = intentionalCooldown.Value;
        }
        else
        {
             AttackDuration = AttackSpeed / 3f;
        }
       
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
            AttackCooldown = AttackSpeed;
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
            // ��ų Ȱ��ȭ ������ CurrentSkill�� �����Ѵ�.
            // ���� �ð��� �ִ� ��ų�� CurrentSkill���� StartSkillCoroutine�� �����ų ���̴�.
            CurrentSkill.Activate(this);
        }
    }

    public void StartSkillCoroutine(IEnumerator skillCoroutine)
    {
        // ���� �ڷ�ƾ ����
        if (_activeSkillCoroutine != null)
        {
            StopCoroutine(_activeSkillCoroutine);
        }

        _activeSkillCoroutine = StartCoroutine(skillCoroutine);
    }

    public void EndSkillCoroutine()
    {
        // �ڷ�ƾ�� ���������� �����ٸ� StopCoroutine�� ����� �ʿ� ����
        _activeSkillCoroutine = null;
    }

    // ��ġ ��ġ�� ȸ���� ����� ���� ������ gridPos�� ������
    protected void UpdateAttackableTiles()
    {
        // �ʱ�ȭ�� baseOffset�� ���� ����� ����.
        rotatedOffsets = new List<Vector2Int>(baseOffsets
            .Select(tile => DirectionSystem.RotateGridOffset(tile, FacingDirection))
            .ToList());
        CurrentAttackableGridPos = new List<Vector2Int>();

        // ���� ���۷������� ��ġ�� ������� �� ���� ����
        foreach (Vector2Int offset in rotatedOffsets)
        {
            Vector2Int inRangeGridPosition = operatorGridPos + offset;
            CurrentAttackableGridPos.Add(inRangeGridPosition);
        }
    }

    // ������ ������Ʈ Ǯ���� �ִٸ� ����� ����
    private void CreateObjectPool()
    {
        // ���� ���� ����Ʈ Ǯ ����
        if (OperatorData.meleeAttackEffectPrefab != null)
        {
            meleeAttackEffectTag = OperatorData.entityName + OperatorData.meleeAttackEffectPrefab.name;
            ObjectPoolManager.Instance!.CreatePool(
                meleeAttackEffectTag,
                OperatorData.meleeAttackEffectPrefab
            );
        }

        // �ǰ� ����Ʈ Ǯ ����
        if (OperatorData.hitEffectPrefab != null)
        {
            hitEffectTag = OperatorData.entityName + OperatorData.hitEffectPrefab.name;
            ObjectPoolManager.Instance!.CreatePool(
                hitEffectTag,
                OperatorData.hitEffectPrefab
            );
        }

        // ���Ÿ��� ��� ����ü Ǯ ����
        InitializeProjectilePool();

        // ��ų ����Ʈ Ǯ ����
        CurrentSkill.InitializeSkillObjectPool();
    }

    protected virtual void PlayMeleeAttackEffect(UnitEntity target, AttackSource attackSource)
    {
        Vector3 targetPosition = target.transform.position;
        PlayMeleeAttackEffect(targetPosition, attackSource);
    }

    protected virtual void PlayMeleeAttackEffect(Vector3 targetPosition, AttackSource attackSource)
    {
        // ����Ʈ ó��
        if (OperatorData.meleeAttackEffectPrefab != null && meleeAttackEffectTag != null)
        {
            // ����Ʈ�� ���� ������ combatVFXController���� ����

            GameObject? effectObj = ObjectPoolManager.Instance!.SpawnFromPool(
                   meleeAttackEffectTag,
                   transform.position, // ����Ʈ ���� ��ġ
                   Quaternion.identity
           );
           
            if (effectObj != null)
            {
                CombatVFXController? combatVFXController = effectObj.GetComponent<CombatVFXController>();
                if (combatVFXController != null)
                {
                    combatVFXController.Initialize(attackSource, targetPosition, meleeAttackEffectTag);
                }
            }
        }
    }

    private void RemoveObjectPool()
    {
        if (meleeAttackEffectTag != null)
        {
            ObjectPoolManager.Instance!.RemovePool(meleeAttackEffectTag);
        }

        if (hitEffectTag != null)
        {
            ObjectPoolManager.Instance!.RemovePool(hitEffectTag);
        }

        RemoveProjectilePool();
        CurrentSkill.CleanupSkill();
    }

    public void InitializeProjectilePool()
    {
        if (AttackRangeType == AttackRangeType.Ranged && 
            OperatorData.projectilePrefab != null)
        {
            projectileTag = $"{OperatorData.entityName}_Projectile";
            ObjectPoolManager.Instance!.CreatePool(projectileTag, OperatorData.projectilePrefab, 5);
        }
    }

    protected void RemoveProjectilePool()
    {
        if (AttackRangeType == AttackRangeType.Ranged 
            && projectileTag != null)
        {
            ObjectPoolManager.Instance!.RemovePool(projectileTag);
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

    // ���� ǥ�� UI ����
    private void CreateDirectionIndicator()
    {
        // �ڽ� ������Ʈ�� ��
        DirectionIndicator indicator = Instantiate(StageUIManager.Instance!.directionIndicator, transform).GetComponent<DirectionIndicator>();
        indicator.Initialize(this);

        // ���۷����Ͱ� �ı��� �� �Բ� �ı��ǹǷ� ���������� �������� �ʾƵ� ��
    }

    protected override float CalculateActualDamage(AttackType attacktype, float incomingDamage)
    {
        float actualDamage = 0; // �Ҵ���� �ʼ�

        switch (attacktype)
        {
            case AttackType.Physical:
                actualDamage = incomingDamage - currentOperatorStats.Defense;
                break;
            case AttackType.Magical:
                actualDamage = incomingDamage * (1 - currentOperatorStats.MagicResistance / 100);
                break;
            case AttackType.True:
                actualDamage = incomingDamage;
                break;
        }

        return Mathf.Max(actualDamage, 0.05f * incomingDamage); // ���� ������� 5%�� ���Բ� ����
    }

    public void OnEnemyEnteredAttackRange(Enemy enemy)
    {
        if (!enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Add(enemy);
        }   
    }

    public void OnEnemyExitedAttackRange(Enemy enemy)
    {
        // ������ ���� ���� ���� �ش� ���� �ִ°��� �˻�
        foreach (var gridPos in CurrentAttackableGridPos)
        {
            Tile? targetTile = MapManager.Instance!.GetTile(gridPos.x, gridPos.y);
            if (targetTile != null && targetTile.EnemiesOnTile.Contains(enemy))
            {
                return;
            }
        }

        // ���� �������� ������ ��Ż�� ��쿡 ����
        enemiesInRange.Remove(enemy);
        if (CurrentTarget == enemy)
        {
            CurrentTarget = null; // ���� Ÿ���� ���� ��� null�� ����
        }
    }

    // ���� ���� Ÿ�ϵ鿡 �� ���۷����͸� ���
    private void RegisterTiles()
    {
        foreach (Vector2Int eachPos in CurrentAttackableGridPos)
        {
            Tile? targetTile = MapManager.Instance!.GetTile(eachPos.x, eachPos.y);
            if (targetTile != null)
            {
                targetTile.RegisterOperator(this);
            }
        }
    }

    // ���� ���� Ÿ�ϵ鿡 �� ���۷����͸� ��� ����
    private void UnregisterTiles()
    {
        foreach (Vector2Int eachPos in CurrentAttackableGridPos)
        {
            Tile? targetTile = MapManager.Instance!.GetTile(eachPos.x, eachPos.y);
            if (targetTile != null)
            {
                targetTile.UnregisterOperator(this);
            }
        }
    }

    protected void OnDestroy()
    {
        // Die �޼��忡�� ���������� �����ϰ�
        Enemy.OnEnemyDespawned -= HandleEnemyDespawn;

        if (_activeSkillCoroutine != null)
        {
            // ������Ʈ �ı� �� ���� ���̴� ��ų �ڷ�ƾ�� ������Ų��
            StopCoroutine(_activeSkillCoroutine);
            _activeSkillCoroutine = null;
        }

        if (IsDeployed && MapManager.Instance != null)
        {
            UnregisterTiles();
        }

        if (ObjectPoolManager.Instance != null)
        {
            RemoveObjectPool();
        }

        if (operatorUIInstance != null)
        {
            Destroy(operatorUIInstance);
        }
    }

    public void SetMovementSpeed(float newMovementSpeed) { }

}
