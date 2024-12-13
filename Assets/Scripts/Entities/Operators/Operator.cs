using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Skills.Base;

public class Operator : DeployableUnitEntity, ICombatEntity, ISkill, IRotatable
{
    public OperatorData BaseData { get; protected set; } 
    
    [HideInInspector] public new OperatorStats currentStats;

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
            UpdateDirectionIndicator(facingDirection);
        }
    }

    // 저지 관련
    protected List<Enemy> blockedEnemies = new List<Enemy>(); // 저지 중인 적들. 공격 대상 선정 때문에 남겨둔다.
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
    protected int initialPoolSize = 5;
    protected string projectileTag;

    // 스킬 관련
    public Skill ActiveSkill { get; set; }
    public bool IsSkillActive { get; protected set; } = false;
    public float SkillDuration { get; protected set; } = 0f;
    public float RemainingSkillDuration { get; protected set; } = 0f;

    // 이벤트들
    public event System.Action<float, float> OnSPChanged;
    public event System.Action OnStatsChanged; 

    // 필드 끝 --------------------------------------------------------------------------------------------

    protected override void Awake()
    {
        base.Awake();
    }
    
    /// <summary>
    /// 성장 정보를 담은 초기화 방식
    /// </summary>
    public virtual void Initialize(OwnedOperator ownedOp)
    {
        // 기본 데이터 초기화
        BaseData = ownedOp.BaseData;
        CurrentSP = BaseData.initialSP;

        // 현재 상태 반영
        currentStats = ownedOp.currentStats;
        CurrentAttackbleTiles = new List<Vector2Int>(ownedOp.currentAttackableTiles);

        // 스킬 설정
        // 나중에 메인 메뉴에서 스킬을 선택할 수 있을 걸 감안하면, OwnedOperator에서 설정하는 게 더 맞는 것 같다
        ActiveSkill = ownedOp.selectedSkill;
        CurrentSP = ownedOp.currentStats.StartSP;
        MaxSP = ActiveSkill?.SPCost ?? 0f;

        IsPreviewMode = true;

        if (modelObject == null)
        {
            InitializeVisual();
        }

        CreateDirectionIndicator();

        // 원거리 투사체 오브젝트 풀 초기화
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

        // DeployableUnitData 초기화 (만약 SerializeField로 설정되어 있다면 이미 할당되어 있음)
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

    protected virtual void Update()
    {
        if (IsDeployed)
        {
            UpdateAttackTimings();

            if (AttackDuration > 0) return;

            SetCurrentTarget(); // CurrentTarget 설정
            ValidateCurrentTarget(); 

            if (CanAttack())
            {
                Attack(CurrentTarget, AttackType, AttackPower);
            }

            RecoverSP();
            if (ActiveSkill.AutoActivate && CurrentSP == MaxSP)
            {
                UseSkill();
            }
        }
    }

    protected void OnDestroy()
    {
        if (AttackRangeType == AttackRangeType.Ranged)
        {
            CleanupProjectilePool();
        }
    }

    // 인터페이스만 계승, 이 안에서는 대미지 팝업 요소만 추가해서 PerformAttack으로 전달
    public virtual void Attack(UnitEntity target, AttackType attackType, float damage)
    {
        bool showDamagePopup = false;
        PerformAttack(target, attackType, damage, showDamagePopup);
    }

    protected void PerformAttack(UnitEntity target, AttackType attackType, float damage, bool showDamagePopup)
    {
        if (ActiveSkill != null)
        {
            ActiveSkill.OnAttack(this, ref damage, ref showDamagePopup);    
        }

        switch (BaseData.attackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target, attackType, damage, showDamagePopup);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target, attackType, damage, showDamagePopup);
                break;
        }
    }

    protected virtual void PerformMeleeAttack(UnitEntity target, AttackType attackType, float damage, bool showDamagePopup)
    {
        SetAttackTimings();
        target.TakeDamage(attackType, damage, this);

        if (showDamagePopup)
        {
            ObjectPoolManager.Instance.ShowFloatingText(target.transform.position, damage, false);
        }
    }

    protected virtual void PerformRangedAttack(UnitEntity target, AttackType attackType, float damage, bool showDamagePopup)
    {
        SetAttackTimings();
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
                    projectile.Initialize(this, target, attackType, damage, showDamagePopup, projectileTag);
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
            Vector2Int rotatedOffset = RotateOffset(offset, facingDirection);
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


    // Operator 회전
    public Vector2Int RotateOffset(Vector2Int offset, Vector3 direction)
    {
        if (direction == Vector3.left) return offset;
        if (direction == Vector3.right) return new Vector2Int(-offset.x, -offset.y);
        if (direction == Vector3.forward) return new Vector2Int(-offset.y, offset.x); // 2차원(y평면)으로 보면 위쪽
        if (direction == Vector3.back) return new Vector2Int(offset.y, -offset.x); // 2차원 기준 아래쪽
        return offset;
    }


    protected Vector2Int WorldToRelativeGridPosition(Vector3 worldPosition)
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

    // --- 저지 관련 메서드들

   
    public bool CanBlockEnemy(int enemyBlockCount)
    {
        // 현재 저지 중인 적 + 지금 저지하려는 적이 차지하는 저지 수가 최대 저지수 이하
        // currentStats을 쓴 이유는 저지수가 올라가는 스킬 등도 있을 수 있기 때문에
        return nowBlockingCount + enemyBlockCount <= currentStats.MaxBlockableEnemies;
    }

    // 저지 가능하다면 현 저지수 + 1
    public void TryBlockEnemy(Enemy enemy)
    {
        int enemyBlockCount = enemy.Data.blockCount;
        if (CanBlockEnemy(enemyBlockCount))
        {
            blockedEnemies.Add(enemy);
            nowBlockingCount += enemyBlockCount;
        }
    }

    public void UnblockEnemy(Enemy enemy)
    {
        Debug.LogWarning("적 저지 해제");
        blockedEnemies.Remove(enemy);
        nowBlockingCount -= enemy.Data.blockCount;
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
        if (IsDeployed == false || ActiveSkill == null) { return;  }

        float oldSP = CurrentSP;

        if (ActiveSkill.AutoRecover)
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
        StatisticsManager.Instance.UpdateDamageTaken(this, damage);
    }

    protected override void Die()
    {
       

        // 사망 후 작동해야 하는 로직이 있을 듯?
        UnblockAllEnemies();

        // 오브젝트 파괴
        //Destroy(deployableBarUI.gameObject); // 하단 체력/SP 바
        Destroy(operatorUIInstance.gameObject);
        OnSPChanged = null;
        Destroy(directionIndicator.gameObject); // 방향 표시기

        base.Die();

        // 하단 UI 활성화
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

    /// <summary>
    /// 방향 표시 UI 생성
    /// </summary>
    protected void CreateDirectionIndicator()
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

            // x축 회전 : 바닥에 눕히기 / z축 중심으로 -angle만큼 회전시키면 방향이 맞음(테스트 완료)
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

        foreach (Vector2Int offset in BaseData.attackableTiles)
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
    /// 현재 타겟의 유효성 검사 : CurrentTarget이 공격 범위 내에 없다면 제거함
    /// </summary>
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
            Debug.Log($"현재 타겟 해제 동작 후: {CurrentTarget}");
        }
        
    }

    /// <summary>
    /// CurrentTarget이 이동했을 때, 공격범위 내에 있는지 체크
    /// </summary>
    protected bool IsCurrentTargetInRange()
    {
        Vector2Int targetGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(CurrentTarget.transform.position);
        Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);

        foreach (Vector2Int offset in BaseData.attackableTiles)
        {
            Vector2Int rotatedOffset = RotateOffset(offset, facingDirection);
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
        IsPreviewMode = false;
        SetDeploymentOrder();
        SetDirection(facingDirection);
        UpdateAttackbleTiles();
        CreateOperatorUI();
        ShowDirectionIndicator(true);
        CurrentSP = currentStats.StartSP;
    }

    public override void Retreat()
    {
        // 코스트 회수 로직 필요


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

    /// <summary>
    /// 공격 대상 설정 로직
    /// </summary>
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

    /// <summary>
    /// 공격 대상 제거 로직
    /// </summary>
    public void RemoveCurrentTarget()
    {
        if (CurrentTarget == null) return;

        CurrentTarget.RemoveAttackingEntity(this);
        CurrentTarget = null;
    }

    /// <summary>
    /// CurrentTarget에게 자신이 공격하고 있음을 알림
    /// </summary>
    public void NotifyTarget()
    {
        CurrentTarget.AddAttackingEntity(this);
    }

    /// <summary>
    /// 투사체 풀을 만듦. 오퍼레이터마다의 고유한 이름이 모두 다르므로 풀의 태그 이름도 모두 다름
    /// </summary>
    public void InitializeProjectilePool()
    {
        projectileTag = $"{BaseData.entityName}_Projectile";
        ObjectPoolManager.Instance.CreatePool(projectileTag, BaseData.projectilePrefab, initialPoolSize);
    }

    protected void CleanupProjectilePool()
    {
        if (!string.IsNullOrEmpty(projectileTag))
        {
            ObjectPoolManager.Instance.RemovePool(projectileTag);
        }
    }

    // ICombatEntity 메서드들

    /// <summary>
    /// Update에 구현, 
    /// 두 값 모두 있을 때에만 작동한다.
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


    // 공격 모션 시간, 공격 쿨타임 시간 설정
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
            IsCurrentTargetInRange(); // 공격 범위 내에 있음
    }

    public void UseSkill()
    {
        if (CanUseSkill() && ActiveSkill != null)
        {
            ActiveSkill.Activate(this);
            UpdateOperatorUI();
        }
    }

    protected void UpdateAttackbleTiles()
    {
        CurrentAttackbleTiles = BaseData.attackableTiles
            .Select(tile => RotateOffset(tile, FacingDirection))
            .ToList();
    }

    // 스킬 사용 시 SP Bar 관련 설정
    public void StartSkillDurationDisplay(float duration)
    {
        IsSkillActive = true;
        SkillDuration = duration;
        RemainingSkillDuration = duration;
        UpdateOperatorUI();
    }

    public void UpdateSkillDurationDisplay(float remainingPercentage)
    {
        CurrentSP = MaxSP * remainingPercentage;
        UpdateOperatorUI();
    }

    public void EndSkillDurationDisplay()
    {
        IsSkillActive = false;
        SkillDuration = 0f;
        RemainingSkillDuration = 0f;
        CurrentSP = 0;
        UpdateOperatorUI();
    }

    protected void UpdateOperatorUI()
    {
        if (operatorUIScript != null)
        {
            operatorUIScript.UpdateUI();
        }
    }
}
