using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using Skills.Base;
using static ICombatEntity;

public class Operator : DeployableUnitEntity, ICombatEntity, ISkill, IRotatable, ICrowdControlTarget
{
    public new OperatorData BaseData { get; protected set; } 
    public new OperatorStats currentStats; // 일단 public으로 구현

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

    public float AttackCooldown { get; protected set; }
    public float AttackDuration { get; protected set; }

    // 위치와 회전
    private Vector2Int operatorGridPos;
    private List<Vector2Int> baseOffsets; // 기본 오프셋
    private List<Vector2Int> rotatedOffsets; // 회전 반영 오프셋
    public List<Vector2Int> CurrentAttacakbleGridPos { get; set; } // 회전 반영 공격 범위(gridPosition), public set은 스킬 때문에

    // 공격 범위 내에 있는 적들 
    protected List<Enemy> enemiesInRange = new List<Enemy>();

    public Vector3 FacingDirection { get; protected set; } = Vector3.left;

    // 저지 관련
    protected List<Enemy> blockedEnemies = new List<Enemy>();
    public IReadOnlyList<Enemy> BlockedEnemies => blockedEnemies;

    public int DeploymentOrder { get; protected set; } // 배치 순서
    //protected bool isDeployed = false; // 배치 완료 시 true

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

    // 원거리 공격 오브젝트 풀 옵션
    protected string projectileTag;

    // 이펙트 풀 태그
    private string meleeAttackEffectTag;
    private string hitEffectTag;

    // 스킬 관련
    public BaseSkill CurrentSkill { get; private set; }
    public bool IsSkillOn { get; private set; }

    // 이벤트들
    public event System.Action<float, float> OnSPChanged;
    public event System.Action OnStatsChanged;
    public event System.Action<Operator> OnOperatorDied;

    public virtual void Initialize(OwnedOperator ownedOp)
    {
        // 기본 데이터 초기화
        BaseData = ownedOp.BaseData;
        CurrentSP = BaseData.initialSP;

        // 현재 상태 반영
        currentStats = ownedOp.CurrentStats;

        // 회전 반영
        baseOffsets = new List<Vector2Int>(ownedOp.CurrentAttackableGridPos); // 왼쪽 방향 기준

        // 스킬 설정
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
            UpdateCrowdControls(); // CC 효과 갱신

            if (activeCC.Any(cc => cc is StunEffect)) return;
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

    }


    // 인터페이스만 계승, PerformAttack으로 전달
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

            // 수동 발동인 스킬만 스킬 실행 가능 상태를 띄움
            if (!CurrentSkill.autoActivate)
            {
                bool isSkillReady = CurrentSP >= MaxSP;
                operatorUIScript.SetSkillIconVisibility(isSkillReady);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 오퍼레이터와의 충돌
        Enemy collidedEnemy = other.GetComponent<Enemy>();

        if (collidedEnemy != null && 
            CanBlockEnemy(collidedEnemy.BlockCount) && // 이 오퍼레이터가 이 적을 저지할 수 있을 때 
            collidedEnemy.BlockingOperator == null) // 해당 적을 저지 중인 아군 오퍼레이터가 없을 때 
        {
            BlockEnemy(collidedEnemy); // 적을 저지
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

    // --- 저지 관련 메서드들
    // 지금 들어오는 Enemy가 차지하는 저지 수를 점검해야 해서 이렇게 구현함 (단일 개체 3저지인 적도 있을 거니까)
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
        // 배치되어야 Die가 가능
        if (!IsDeployed) return;

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

        // 오퍼레이터 사망 이벤트 발생
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

    // 공격 대상인 적이 죽었을 때 작동함. 저지 해제와 별개로 구현
    public void OnTargetLost(UnitEntity unit)
    {
        // 공격 대상에서 제거
        if (CurrentTarget == unit)
        {
            // 범위 내 적 리스트에서 제거
            if (unit is Enemy enemy)
            {
                enemiesInRange.Remove(enemy); // 안하면 리스트에 파괴된 오브젝트가 남아서 0번 인덱스를 캐치하지 못함
                Debug.Log($"{enemy} 사망, {BaseData.entityName}의 enemiesInRange의 길이  : {enemiesInRange.Count}");
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

    /// 현재 타겟의 유효성 검사
    protected virtual void ValidateCurrentTarget()
    {
        if (CurrentTarget == null) return;
        if (blockedEnemies.Contains(CurrentTarget)) return;

        // 범위에서 벗어난 경우
        if (!IsCurrentTargetInRange())
        {
            CurrentTarget.RemoveAttackingEntity(this);
            CurrentTarget = null;
        }
    }
    
    // Target이 공격 범위 내의 타일에 있는지 체크
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
                Destroy(deployEffect, 1.2f); // 1초 후 파괴. 
            }
        }

        IsPreviewMode = false;
        SetDeploymentOrder();
        operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);
        SetDirection(FacingDirection);
        UpdateAttackableTiles();
        CreateOperatorUI();
        CurrentSP = currentStats.StartSP;

        // 이펙트 오브젝트 풀 생성
        CreateObjectPool();
    }

    public override void Retreat()
    {
        // 수동 퇴각 시 최초 배치 코스트의 절반 회복
        int recoverCost = (int)Mathf.Round(currentStats.DeploymentCost / 2f);
        StageManager.Instance.RecoverDeploymentCost(recoverCost);

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
        if (CurrentTarget != null)
        {
            CurrentTarget.RemoveAttackingEntity(this);
            CurrentTarget = null;
        }
    }

    // ICombatEntity 메서드들
    public void NotifyTarget()
    {
        if (CurrentTarget != null)
        {
            CurrentTarget.AddAttackingEntity(this);
        }
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

    // 배치 위치와 회전을 고려한 공격 범위의 gridPos을 설정함
    protected void UpdateAttackableTiles()
    {
        // 초기화를 baseOffset의 깊은 복사로 했음.
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

    private void PlayMeleeAttackEffect(UnitEntity target, AttackSource attackSource)
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

    // 지속 시간이 있는 스킬을 켜거나 끌 때 호출됨
    public void SetSkillOnState(bool skillOnState)
    {
        IsSkillOn = skillOnState;
    }



    protected override float CalculateActualDamage(AttackType attacktype, float incomingDamage)
    {
        float actualDamage = 0; // 할당까지 필수

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
        return Mathf.Max(actualDamage, 0.05f * incomingDamage); // 들어온 대미지의 5%는 들어가게끔 보장
    }

    protected void OnDestroy()
    {
        RemoveObjectPool();
    }
    public void SetMovementSpeed(float newMovementSpeed) { }

}
