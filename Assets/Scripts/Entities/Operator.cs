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

    // ICombatEntity 필드
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

    // 공격 범위 내에 있는 적들 
    List<Enemy> enemiesInRange = new List<Enemy>();

    // IRotatble 필드
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

    // 저지 관련
    private List<Enemy> blockedEnemies = new List<Enemy>(); // 저지 중인 적들. Awake 이전에 초기화됨.
    public IReadOnlyList<Enemy> BlockedEnemies => blockedEnemies.AsReadOnly();

    public int DeploymentOrder { get; private set; } // 배치 순서
    private bool isDeployed = false; // 배치 완료 시 true
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
    //private DeployableBarUI deployableBarUI; // 체력, SP
    private SpriteRenderer directionIndicator; // 방향 표시 UI

    // 원거리 공격 오브젝트 풀 옵션
    protected int initialPoolSize = 5;
    protected string projectileTag; 


    // 스킬 관련
    private List<Skill> skills;
    private Skill activeSkill;
    public Skill ActiveSkill => activeSkill;

    // 이벤트들
    public event System.Action<float, float> OnSPChanged;
    public event System.Action OnStatsChanged; 

    // 필드 끝 --------------------------------------------------------

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
            Debug.LogError("Data가 null임!!!");
        }
    }


    // 오퍼레이터 관련 설정들 초기화
    private void InitializeOperatorProperties()
    {

        CreateDirectionIndicator(); // 방향 표시기 생성

        // 사용하는 스킬 설정
        skills = Data.skills;
        SetActiveSkill(operatorData.defaultSkillIndex);

        // MaxSP값 설정
        if (activeSkill != null)
        {
            MaxSP = activeSkill.SPCost;
        }

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

            SetCurrentTarget(); // CurrentTarget 설정
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
            // 투사체 생성 위치
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


    // 공격 타일에 있는 적들을 반환함
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
                // 타일 위의 적들을 보는 로직은 Tile.cs에 구현됨
                List<Enemy> enemiesOnTile = targetTile.GetEnemiesOnTile();
                enemiesInRange.AddRange(enemiesOnTile);
            }
        }

        enemiesInRange = enemiesInRange.Distinct().ToList(); // 중복 제거해서 반환
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

    // --- 저지 관련 메서드들

    // 이 오퍼레이터가 적을 저지할 수 있는 상태인가?
    public bool CanBlockEnemy()
    {
        return blockedEnemies.Count < currentStats.MaxBlockableEnemies;
    }

    // 저지 가능하다면 현 저지수 + 1
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
        Debug.LogWarning("적 저지 해제");
        blockedEnemies.Remove(enemy);
    }

    public void UnblockAllEnemies()
    {
        blockedEnemies.Clear();
    }

    // SP 로직 추가
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
        // 사망 후 작동해야 하는 로직이 있을 듯?
        UnblockAllEnemies();

        // 오브젝트 파괴
        //Destroy(deployableBarUI.gameObject); // 하단 체력/SP 바
        Destroy(operatorUIInstance.gameObject);
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
    /// 현재 타겟의 유효성 검사 : CurrentTarget이 공격 범위 내에 없다면 제거함
    /// </summary>
    private void ValidateCurrentTarget()
    {
        if (CurrentTarget == null) return;

        // 범위에서 벗어난 경우
        if (!IsCurrentTargetInRange())
        {
            CurrentTarget.RemoveAttackingEntity(this);
            CurrentTarget = null;
        }
        
    }

    /// <summary>
    /// CurrentTarget이 공격범위 내에 있는지 체크
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

    // ISkill 메서드
    public bool CanUseSkill()
    {
        return IsDeployed && CurrentSP >= MaxSP;
    }

    /// <summary>
    /// Data, Stat이 엔티티마다 다르기 때문에 자식 메서드에서 재정의가 항상 필요
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
    /// 공격 대상 설정 로직
    /// </summary>
    public void SetCurrentTarget()
    {
        // 1. 저지 중일 때 -> 저지 중인 적
        if (blockedEnemies.Count > 0)
        {
            CurrentTarget = blockedEnemies[0];
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

    // 스킬 관련
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
            // SP 소모 및 쿨다운 로직 추가
        }
    }

    private void UpdateAttackbleTiles()
    {
        CurrentAttackbleTiles = Data.attackableTiles
            .Select(tile => RotateOffset(tile, FacingDirection))
            .ToArray();
    }
}
