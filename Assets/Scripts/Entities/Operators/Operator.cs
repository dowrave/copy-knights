using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using Skills.Base;
using static ICombatEntity;

public class Operator : DeployableUnitEntity, ICombatEntity, ISkill, IRotatable, ICrowdControlTarget
{
    public new OperatorData BaseData { get; protected set; } 
    [HideInInspector] public new OperatorStats currentStats; // 일단 public으로 구현

    // ICombatEntity 필드
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

    // ICrowdControlTarget 필드
    public float MovementSpeed => 0f;
    public Vector3 Position => transform.position; 
    public void SetMovementSpeed(float newMovementSpeed) { }

    public float AttackCooldown { get; protected set; }
    public float AttackDuration { get; protected set; }
   
    public List<Vector2Int> CurrentAttackbleTiles { get; set; }

    // 공격 범위 내에 있는 적들 
    protected List<Enemy> enemiesInRange = new List<Enemy>();

    // IRotatble 필드
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

    // 저지 관련
    protected List<Enemy> blockedEnemies = new List<Enemy>();
    protected int nowBlockingCount = 0;

    public int DeploymentOrder { get; protected set; } // 배치 순서
    protected bool isDeployed = false; // 배치 완료 시 true
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
    protected SpriteRenderer directionIndicator; // 방향 표시 UI

    // 원거리 공격 오브젝트 풀 옵션
    protected string projectileTag;

    // 이펙트 풀 태그
    string meleeAttackEffectTag;
    string hitEffectTag;

    // 스킬 관련
    public BaseSkill CurrentSkill { get; private set; }
    public bool IsSkillOn { get; private set; }

    // 이벤트들
    public event System.Action<float, float> OnSPChanged;
    public event System.Action OnStatsChanged; 

    // 필드 끝 --------------------------------------------------------------------------------------------
    public virtual void Initialize(OwnedOperator ownedOp)
    {
        // 기본 데이터 초기화
        BaseData = ownedOp.BaseData;
        CurrentSP = BaseData.initialSP;

        // 현재 상태 반영
        currentStats = ownedOp.currentStats;
        CurrentAttackbleTiles = new List<Vector2Int>(ownedOp.currentAttackableTiles);

        // 스킬 설정
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

            SetCurrentTarget(); // CurrentTarget 설정
            ValidateCurrentTarget(); 

            if (CanAttack())
            {
                // 공격 형식을 바꿔야 하는 경우 스킬의 동작을 따라감
                if (ShouldModifyAttackAction())
                {
                    CurrentSkill.PerformChangedAttackAction(this);
                }
                // 아니라면 평타
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


    // 인터페이스만 계승, 이 안에서는 대미지 팝업 요소만 추가해서 PerformAttack으로 전달
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
            // 투사체 생성 위치
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

        // 공격 범위(타일들)에 있는 적들을 수집합니다
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

    // --- 저지 관련 메서드들
    public bool CanBlockEnemy(int enemyBlockCount)
    {
        return nowBlockingCount + enemyBlockCount <= currentStats.MaxBlockableEnemies;
    }

    // 저지 가능하다면 현 저지수 + 1
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

    // SP 자동회복 로직 추가
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
        // 사망 후 동작 로직
        UnblockAllEnemies();

        // 오브젝트 파괴
        Destroy(operatorUIInstance.gameObject);
        
        OnSPChanged = null;

        // 이펙트 풀 정리
        if (BaseData.hitEffectPrefab != null)
        {
            ObjectPoolManager.Instance.RemovePool("Effect_" + BaseData.entityName);
        } 

        // 하단 UI 활성화
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

    // 공격 대상인 적이 죽었을 때 작동함. 저지 해제와 별개로 구현
    public void OnTargetLost(Enemy enemy)
    {
        // 공격 대상에서 제거
        if (CurrentTarget == enemy)
        {
            CurrentTarget = null;
        }

        // 범위 내 적 리스트에서 제거
        enemiesInRange.Remove(enemy); // 안하면 리스트에 파괴된 오브젝트가 남아서 0번 인덱스를 캐치하지 못함
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

    /// 현재 타겟의 유효성 검사
    protected virtual void ValidateCurrentTarget()
    {
        if (CurrentTarget == null)
        {
            return;
        }
           
        // 범위에서 벗어난 경우
        if (!IsCurrentTargetInRange())
        {
            CurrentTarget.RemoveAttackingEntity(this);
            CurrentTarget = null;
        }
    }

    /// CurrentTarget이 이동했을 때, 공격범위 내에 있는지 체크
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
                Destroy(deployEffect, 1.2f); // 1초 후 파괴. 
            }
        }

        // 이펙트 파괴를 기다리지 않고 동작함
        IsPreviewMode = false;
        SetDeploymentOrder();
        SetDirection(facingDirection);
        UpdateAttackbleTiles();
        CreateOperatorUI();
        CurrentSP = currentStats.StartSP;

        // 이펙트 오브젝트 풀 생성
        CreateObjectPool();
    }

    public override void Retreat()
    {
        Die();
    }

    // ISkill 메서드
    public bool CanUseSkill()
    {
        return IsDeployed && CurrentSP >= MaxSP;
    }

    protected override void InitializeHP()
    {
        MaxHealth = currentStats.Health;
        CurrentHealth = MaxHealth;
    }


    // 공격 대상 설정 로직
    public virtual void SetCurrentTarget()
    {
        // 1. 저지 중일 때 -> 저지 중인 적
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

        GetEnemiesInAttackRange(); // 공격 범위 내의 적을 얻음

        // 2. 저지 중이 아닐 때에는 공격 범위 내의 적 중에서 공격함
        if (enemiesInRange.Count > 0)
        {
            CurrentTarget = enemiesInRange.OrderBy(E => E.GetRemainingPathDistance()).FirstOrDefault();
            if (CurrentTarget != null)
            {
                NotifyTarget();
            }
            return;
        }
        // 저지 중인 적도 없고, 공격 범위 내의 적도 없다면 현재 타겟은 없음
        CurrentTarget = null;
    }


    // 공격 대상 제거 로직
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

    // ICombatEntity 메서드들
    public void NotifyTarget()
    {
        CurrentTarget.AddAttackingEntity(this);
    }

    // 공격 모션 
    public void UpdateAttackDuration()
    {
        if (AttackDuration > 0f)
        {
            AttackDuration -= Time.deltaTime;
        }
    }

    // 다음 공격 가능 시간
    public void UpdateAttackCooldown()
    {
        if (AttackCooldown > 0f)
        {
            AttackCooldown -= Time.deltaTime;
        }
    }

    // 공격 모션
    public void SetAttackDuration()
    {
        AttackDuration = 0.3f / AttackSpeed;
    }

    // 다음 공격까지의 대기 시간
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
            IsCurrentTargetInRange(); // 공격 범위 내에 있음
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

    // 스킬 사용 시 SP Bar 관련 설정
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


    // 구현할 오브젝트 풀링이 있다면 여기다 넣음
    private void CreateObjectPool()
    {
        // 근접 공격 이펙트 풀 생성
        if (BaseData.meleeAttackEffectPrefab != null)
        {
            meleeAttackEffectTag = BaseData.entityName + BaseData.meleeAttackEffectPrefab.name;
            ObjectPoolManager.Instance.CreatePool(
                meleeAttackEffectTag,
                BaseData.meleeAttackEffectPrefab
            );
        }

        // 피격 이펙트 풀 생성
        if (BaseData.hitEffectPrefab != null)
        {
            hitEffectTag = BaseData.entityName + BaseData.hitEffectPrefab.name;
            ObjectPoolManager.Instance.CreatePool(
                hitEffectTag,
                BaseData.hitEffectPrefab
            );
        }

        // 원거리인 경우 투사체 풀 생성
        InitializeProjectilePool();

        // 스킬 이펙트 풀 생성
        CurrentSkill.InitializeSkillObjectPool();
    }

    private void PlayMeleeAttackEffect(UnitEntity target)
    {
        // 이펙트 처리
        if (BaseData.meleeAttackEffectPrefab != null)
        {
            GameObject effectObj = ObjectPoolManager.Instance.SpawnFromPool(
                   meleeAttackEffectTag,
                   transform.position,
                   Quaternion.identity
           );
            VisualEffect vfx = effectObj.GetComponent<VisualEffect>();

            // 방향이 있다면 방향 계산
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

    // 지속 시간이 있는 스킬을 켜거나 끌 때 호출됨
    public void SetSkillOnState(bool skillOnState)
    {
        IsSkillOn = skillOnState;
    }


    protected void OnDestroy()
    {
        RemoveObjectPool();
    }
}
