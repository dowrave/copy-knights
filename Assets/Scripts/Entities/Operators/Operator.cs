using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using Skills.Base;
using static ICombatEntity;
using Unity.VisualScripting;

public class Operator : DeployableUnitEntity, ICombatEntity, ISkill, IRotatable
{
    public OperatorData OperatorData { get; protected set; } = default!;
    [HideInInspector] 
    public OperatorStats currentOperatorStats; // 일단 public으로 구현

    public string EntityName => OperatorData.entityName;

    // ICombatEntity 필드
    private AttackType _currentAttackType;
    public override AttackType AttackType
    {
        get { return _currentAttackType; }
        set
        {
            _currentAttackType = value;
        }
    }
    public AttackRangeType AttackRangeType => OperatorData.attackRangeType;

    public override float AttackPower
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
    public override float AttackSpeed
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


    public override float Defense
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

    public override float MagicResistance
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

    // ICrowdControlTarget 필드
    public float MovementSpeed => 0f;
    public Vector3 Position => transform.position;

    public float AttackCooldown { get; protected set; }
    public float AttackDuration { get; protected set; }

    // 위치와 회전
    private Vector2Int operatorGridPos;
    private List<Vector2Int> baseOffsets = new List<Vector2Int>(); // 기본 오프셋
    private List<Vector2Int> rotatedOffsets = new List<Vector2Int>(); // 회전 반영 오프셋
    public List<Vector2Int> CurrentAttackableGridPos { get; set; } = new List<Vector2Int>(); // 회전 반영 공격 범위(gridPosition), public set은 스킬 때문에

    // 공격 범위 내에 있는 적들 
    protected List<Enemy> enemiesInRange = new List<Enemy>();

    public Vector3 FacingDirection { get; protected set; } = Vector3.left;

    // 저지 관련
    protected List<Enemy> blockedEnemies = new List<Enemy>(); // 실제로 저지 중인 적들
    public IReadOnlyList<Enemy> BlockedEnemies => blockedEnemies;
    protected int currentBlockCount; // 현재 저지 수

    protected List<Enemy> blockableEnemies = new List<Enemy>(); // 콜라이더가 겹쳐서 저지가 가능한 적들
    public IReadOnlyList<Enemy> BlockableEnemies => blockableEnemies;


    public int DeploymentOrder { get; protected set; } = 0;// 배치 순서

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

    // 원거리 공격 오브젝트 풀 옵션
    protected string? projectileTag;

    // 스킬 관련
    public OperatorSkill CurrentSkill { get; private set; } = default!;
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
    private Coroutine _activeSkillCoroutine; // 지속시간이 있는 스킬에 해당하는 코루틴

    // 현재 오퍼레이터의 육성 상태 - Current를 별도로 붙이지는 않겠음
    public OperatorGrowthSystem.ElitePhase ElitePhase { get; private set; }
    public int Level { get; private set; }

    // 이벤트들
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

            // 기본 데이터 초기화
            OperatorData = ownedOp.OperatorProgressData;
            CurrentSP = OperatorData.initialSP;

            SetPrefab();
            
            // 현재 상태 반영
            currentOperatorStats = ownedOp.CurrentStats;

            // 회전 반영
            baseOffsets = new List<Vector2Int>(ownedOp.CurrentAttackableGridPos); // 왼쪽 방향 기준

            // 스킬 설정
            if (opInfo.skillIndex.HasValue)
            {
                CurrentSkill = ownedOp.UnlockedSkills[opInfo.skillIndex.Value];
            }
            else
            {
                throw new System.InvalidOperationException("인덱스가 없어서 CurrentSkill이 지정되지 않음");
            }

            MaxSP = CurrentSkill?.SPCost ?? 0f;

            ElitePhase = ownedOp.currentPhase;
            Level = ownedOp.currentLevel;

            SetDeployState(false);
        }
        else
        {
            Debug.LogError("오퍼레이터의 ownedOperator 정보가 없음!");
        }
    }
    
    public override void SetPrefab()
    {
        prefab = OperatorData.prefab;
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
        // 컴포넌트를 파괴하는 게 아니라 오브젝트를 파괴해야 함
        Destroy(operatorUI.gameObject);
    }

    protected override void Update()
    {
        if (IsDeployed && StageManager.Instance!.currentState == GameState.Battle)
        {
            // ----- 상태 갱신 로직. 행동 제약과 무관 -----
            UpdateAttackDuration();
            UpdateAttackCooldown();

            HandleSPRecovery(); // SP 회복

            base.Update(); // 버프 효과의 갱신
            CurrentSkill.OnUpdate(this); // 스킬에서도 감시

            // 행동 불능 상태 체크
            if (HasRestriction(ActionRestriction.CannotAction)) return;

            // ----- 행동 가능 상태의 로직 -----
            // 동작을 아예 못하는 상황과 구분한다. 예를 들면 기절 상태인데 스킬을 자동으로 켤 수는 없음.
            HandleSkillAutoActivate();

            if (AttackDuration > 0) return; // 공격 모션 중에는 타겟 변경/공격 불가능
            // 참고) 공격 모션이 아니지만 쿨다운일 때도 타겟은 계속 설정한다. 그래서 AttackCooldown 조건은 달지 않음.

            SetCurrentTarget(); // CurrentTarget 설정
            ValidateCurrentTarget();

            if (CanAttack())
            {
                // 공격 방식을 바꾸는 버프가 있는지 찾는다 
                Buff? attackModifierBuff = activeBuffs.FirstOrDefault(b => b.ModifiesAttackAction);

                // 공격 형식을 바꿔야 하는 경우 스킬의 동작을 따라감
                if (attackModifierBuff != null)
                {
                    attackModifierBuff.PerformChangedAttackAction(this);
                }
                // 아니라면 평타
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


    // 인터페이스만 계승, PerformAttack으로 전달
    public virtual void Attack(UnitEntity target, float damage)
    {
        bool showDamagePopup = false;
        float polishedDamage = Mathf.Floor(damage);

        // 공격이 나가는 시점에 쿨타임이 돌게 수정
        // 실제 공격을 수행할 때 어떻게 수행되는지가 다르므로 PerformAttack 밖에서 구현한다.
        // 예시) DoubleShot의 경우 PerformAttack 안에서 구현하면 쿨타임이 있는데 스킬이 나가는 이상한 구현이 됨
        SetAttackDuration();
        SetAttackCooldown();

        PerformAttack(target, polishedDamage, showDamagePopup);

    }

    public virtual void PerformAttack(UnitEntity target, float damage, bool showDamagePopup)
    {
        float spBeforeAttack = CurrentSP;
        AttackType finalAttackType = AttackType;

        // 스킬 시스템에서 버프로 변환 중
        // 공격에만 적용되는 버프 적용
        foreach (var buff in activeBuffs)
        {
            buff.OnBeforeAttack(this, ref damage, ref finalAttackType, ref showDamagePopup);
        }



        // 실제 공격 수행
        switch (OperatorData.attackRangeType)
        {
            case AttackRangeType.Melee:
                PerformMeleeAttack(target, damage, showDamagePopup, finalAttackType);
                break;
            case AttackRangeType.Ranged:
                PerformRangedAttack(target, damage, showDamagePopup, finalAttackType);
                break;
        }

        // 공격 후 SP 회복 로직
        if (!CurrentSkill.autoRecover && // 자동회복이 아니면서
            !IsSkillOn &&
            spBeforeAttack != MaxSP) // 스킬이 실려서 나간 공격일 때는 SP 회복 X 
        {
            CurrentSP += 1;
        }

        // 공격 후 동작
        foreach (var buff in activeBuffs.ToList()) // buff가 제거될 수 있기 때문에 복사본으로 안전하게 진행
        {
            buff.OnAfterAttack(this, target);
        }

    }

    protected virtual void PerformMeleeAttack(UnitEntity target, float damage, bool showDamagePopup, AttackType attackType)
    {
        AttackSource attackSource = new AttackSource(
            attacker: this,
            position: transform.position,
            damage: damage,
            type: attackType,
            isProjectile: false,
            hitEffectPrefab: OperatorData.HitEffectPrefab,
            hitEffectTag: hitEffectTag
        );

        PlayMeleeAttackEffect(target, attackSource);
        target.TakeDamage(attackSource);

        // 대미지 팝업 표시
        if (showDamagePopup)
        {
            ObjectPoolManager.Instance!.ShowFloatingText(target.transform.position, damage, false);
        }
    }

    protected virtual void PerformRangedAttack(UnitEntity target, float damage, bool showDamagePopup, AttackType attackType)
    {
        if (OperatorData.projectilePrefab != null)
        {
            // 투사체 생성 위치
            // Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
            Vector3 spawnPosition = transform.position + transform.forward * 0.25f;

            // Debug.Log($"[Operator]Projectile의 생성 위치 : {spawnPosition}");

            if (projectileTag != null)
            {
                GameObject? projectileObj = ObjectPoolManager.Instance!.SpawnFromPool(projectileTag, spawnPosition, Quaternion.identity);
                if (projectileObj != null)
                {
                    Projectile? projectile = projectileObj.GetComponent<Projectile>();
                    if (projectile != null)
                    {
                        PlayMuzzleVFX();
                        projectile.Initialize(this, target, damage, showDamagePopup, projectileTag, OperatorData.hitEffectPrefab, hitEffectTag, AttackType);
                    }
                }
            }
        }
    }

    // 원거리인 경우에만 사용. Muzzle 이펙트를 실행한다.
    private void PlayMuzzleVFX()
    {
        if (OperatorData.muzzleVFXPrefab != null && muzzleTag != string.Empty)
        {
            GameObject muzzleVFXObject = ObjectPoolManager.Instance!.SpawnFromPool(muzzleTag, transform.position, transform.rotation);
            MuzzleVFXController muzzleVFXController = muzzleVFXObject.GetComponentInChildren<MuzzleVFXController>();
            muzzleVFXController.Initialize(muzzleTag);
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

    // SP 자동회복 로직 추가
    protected void HandleSPRecovery()
    {
        if (IsDeployed == false || CurrentSkill == null) { return; }

        // 자동회복일 때만 처리
        if (CurrentSkill.autoRecover)
        {
            float oldSP = CurrentSP;

            // 최대 SP 초과 방지 (이벤트는 자체 발생)
            CurrentSP = Mathf.Min(CurrentSP + currentOperatorStats.SPRecoveryRate * Time.deltaTime, MaxSP);
        }
        
        // 수동회복 스킬은 공격 시에 회복되므로 여기서 처리하지 않음
    }

    public override void OnBodyTriggerEnter(Collider other)
    {
        // 저지 로직은 본체 콜라이더와 충돌할 때만 발생
        BodyColliderController body = other.GetComponent<BodyColliderController>();

        // Enemy일 때만 저지 로직 동작
        if (body != null && body.ParentUnit is Enemy enemy)
        {
            if (!blockableEnemies.Contains(enemy))
            {
                blockableEnemies.Add(enemy);
                TryBlockNextEnemy();
            }
        }
    }

    // 저지 가능한 슬롯이 있을 때 blockableEnemies에서 다음 적을 찾아 저지한다.
    private void TryBlockNextEnemy()
    {
        // 여유가 없다면 리턴
        if (currentBlockCount >= MaxBlockableEnemies) return;

        // 저지 가능한 적 목록을 순회, 리스트 앞쪽 = 먼저 들어온 적
        foreach (Enemy candidateEnemy in blockableEnemies)
        {
            // 이 적을 저지할 수 있는지 확인
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
        // 저지 로직은 본체 콜라이더와 충돌할 때만 발생
        BodyColliderController body = other.GetComponent<BodyColliderController>();

        // 적의 body일 때만 저지 로직 동작
        if (body != null && body.ParentUnit is Enemy enemy)
        {
            blockableEnemies.Remove(enemy);

            // 실제 저지 중이었다면 저지 해제
            if (blockedEnemies.Contains(enemy))
            {
                UnblockEnemy(enemy);
                enemy.UpdateBlockingOperator(null);
            }

            // 다른 적 저지 시도
            TryBlockNextEnemy();
        }
    }

    // 해당 적을 저지할 수 있는가
    private bool CanBlockEnemy(Enemy enemy)
    {
        return enemy != null && 
            IsDeployed &&
            !blockedEnemies.Contains(enemy) && // 이미 저지 중인 적이 아님
            enemy.BlockingOperator == null &&  // 적을 저지하고 있는 오퍼레이터가 없음
            currentBlockCount + enemy.BlockCount <= MaxBlockableEnemies; // 이 적을 저지했을 때 최대 저지 수를 초과하지 않음
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
        // ToList를 쓰는 이유 : 반복문 안에서 리스트를 수정하면 오류가 발생할 수 있다 - 복사본을 순회하는 게 안전하다.
        foreach (Enemy enemy in blockedEnemies.ToList())
        {
            enemy.UpdateBlockingOperator(null);
            UnblockEnemy(enemy);
        }
    }

    // 부모 클래스에서 정의된 TakeDamage에서 사용됨
    protected override void OnDamageTaken(UnitEntity attacker, float actualDamage)
    {
        StatisticsManager.Instance!.UpdateDamageTaken(OperatorData, actualDamage);
    }

    protected override void Die()
    {
        // 배치되어야 Die가 가능
        if (!IsDeployed) return;

        // 공격 범위 타일 해제
        UnregisterTiles();

        // 스킬 유지 중이었다면 코루틴 취소 
        if (_activeSkillCoroutine != null)
        {
            StopCoroutine(_activeSkillCoroutine);
            _activeSkillCoroutine = null;
        }

        // 사망 후 동작 로직
        UnblockAllEnemies();

        // UI 파괴
        DestroyOperatorUI();
        
        // 필요한지 모르겠어서 일단 주석처리
        //OnSPChanged = null;

        // 이펙트 풀 정리
        if (OperatorData.hitEffectPrefab != null)
        {
            ObjectPoolManager.Instance!.RemovePool("Effect_" + OperatorData.entityName);
        }

        // 배치된 요소에서 제거
        DeployableInfo.deployedOperator = null;

        // 오퍼레이터 사망 이벤트 발생
        OnOperatorDied?.Invoke(this);

        // 이벤트 구독 해제
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
        UpdateAttackableTiles(); // 방향에 따른 공격 범위 타일들 업데이트
        RegisterTiles(); // 타일들에 이 오퍼레이터가 공격 타일로 선정했음을 알림

        CreateDirectionIndicator();
        CreateOperatorUI();

        // deployableInfo의 배치된 오퍼레이터를 이것으로 지정
        DeployableInfo.deployedOperator = this;

        // 이펙트 오브젝트 풀 생성
        CreateObjectPool();

        // 배치 이펙트 실행
        PlayDeployVFX();

        // 적 사망 이벤트 구독
        Enemy.OnEnemyDespawned += HandleEnemyDespawn;
    }

    private void PlayDeployVFX()
    {
        // 배치 VFX 실행 
        if (OperatorData.deployEffectPrefab != null)
        {
            GameObject deployEffect = Instantiate(
                OperatorData.deployEffectPrefab,
                transform.position,
                Quaternion.identity
            );
            var ps = deployEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play(true);
                Destroy(deployEffect, 1.5f);
            }
            else
            {
                var vfx = deployEffect.GetComponent<VisualEffect>();
                if (vfx != null)
                {
                    vfx.Play();
                    Destroy(deployEffect, 1.5f);
                }
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

    // 테스트) 적 파괴 이벤트를 받아 오퍼레이터에서의 처리를 작업함
    private void HandleEnemyDespawn(Enemy enemy, DespawnReason reason)
    {
        // 1. 현재 타겟이라면 타겟 해제
        if (CurrentTarget == enemy)
        {
            CurrentTarget = null;
        }

        // 2. 공격 범위 내에 해당 대상이 있다면 범위 내에서 제외
        if (enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Remove(enemy);
        }

        // 3. 저지 가능 대상, 저지 중인 대상일 때 제외
        // OnTriggerExit은 겹친 상태에서의 파괴를 감지하지 못하므로 별도의 처리가 필요함
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

    // 수동 퇴각 시 최초 배치 코스트의 절반 회복
    private void RecoverInitialCost()
    {
        int recoverCost = (int)Mathf.Round(currentOperatorStats.DeploymentCost / 2f);
        StageManager.Instance!.RecoverDeploymentCost(recoverCost);
    }

    // ISkill 메서드
    public bool CanUseSkill()
    {
        return IsDeployed && CurrentSP >= MaxSP && !IsSkillOn;
    }

    protected override void InitializeHP()
    {
        MaxHealth = Mathf.Floor(currentOperatorStats.Health);
        CurrentHealth = Mathf.Floor(MaxHealth);
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

        // 2. 저지 중이 아닐 때에는 공격 범위 내의 적 중에서 공격함
        if (enemiesInRange.Count > 0)
        {
            CurrentTarget = enemiesInRange
                .Where(e => e != null && e.gameObject != null) // 파괴 검사 & null 검사 함께 수행
                .OrderBy(E => E.GetRemainingPathDistance()) // 살아있는 객체 중 남은 거리가 짧은 순서로 정렬
                .FirstOrDefault(); // 가장 짧은 거리의 객체를 가져옴

            // Debug.Log($"");

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

    // 다음 공격까지의 대기 시간
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
        if (HasRestriction(ActionRestriction.CannotAttack)) return false;

        return IsDeployed &&
            CurrentTarget != null &&
            AttackCooldown <= 0 &&
            AttackDuration <= 0;
    }

    public void UseSkill()
    {
        if (CanUseSkill() && CurrentSkill != null)
        {
            // 스킬 활성화 로직은 CurrentSkill에 위임한다.
            // 지속 시간이 있는 스킬은 CurrentSkill에서 StartSkillCoroutine을 실행시킬 것이다.
            CurrentSkill.Activate(this);
        }
    }

    public void StartSkillCoroutine(IEnumerator skillCoroutine)
    {
        // 기존 코루틴 종료
        if (_activeSkillCoroutine != null)
        {
            StopCoroutine(_activeSkillCoroutine);
        }

        _activeSkillCoroutine = StartCoroutine(skillCoroutine);
    }

    public void EndSkillCoroutine()
    {
        // 코루틴이 정상적으로 끝났다면 StopCoroutine은 실행될 필요 없음
        _activeSkillCoroutine = null;
    }

    // 배치 위치와 회전을 고려한 공격 범위의 gridPos을 설정함
    protected void UpdateAttackableTiles()
    {
        // 초기화를 baseOffset의 깊은 복사로 했음.
        rotatedOffsets = new List<Vector2Int>(baseOffsets
            .Select(tile => DirectionSystem.RotateGridOffset(tile, FacingDirection))
            .ToList());
        CurrentAttackableGridPos = new List<Vector2Int>();

        // 현재 오퍼레이터의 위치를 기반으로 한 공격 범위
        foreach (Vector2Int offset in rotatedOffsets)
        {
            Vector2Int inRangeGridPosition = operatorGridPos + offset;
            CurrentAttackableGridPos.Add(inRangeGridPosition);
        }
    }

    // 구현할 오브젝트 풀링이 있다면 여기다 넣음
    private void CreateObjectPool()
    {
        string baseTag = OperatorData.entityName;

        // 근접 공격 이펙트 풀 생성
        if (OperatorData.meleeAttackEffectPrefab != null)
        {
            meleeAttackEffectTag = $"{baseTag}_{OperatorData.meleeAttackEffectPrefab.name}";
            ObjectPoolManager.Instance!.CreatePool(
                meleeAttackEffectTag,
                OperatorData.meleeAttackEffectPrefab
            );
        }

        // 타격 이펙트 풀 생성
        if (OperatorData.hitEffectPrefab != null)
        {
            hitEffectTag = $"{baseTag}_{OperatorData.hitEffectPrefab.name}";
            ObjectPoolManager.Instance!.CreatePool(
                hitEffectTag,
                OperatorData.hitEffectPrefab
            );
        }

        // 원거리인 경우 투사체 풀 생성
        InitializeRangedPool();

        // 스킬에서 사용할 이펙트 풀 생성
        CurrentSkill.InitializeSkillObjectPool(this);
    }

    protected virtual void PlayMeleeAttackEffect(UnitEntity target, AttackSource attackSource)
    {
        Vector3 targetPosition = target.transform.position;
        PlayMeleeAttackEffect(targetPosition, attackSource);
    }

    protected virtual void PlayMeleeAttackEffect(Vector3 targetPosition, AttackSource attackSource)
    {
        GameObject effectPrefab = OperatorData.meleeAttackEffectPrefab;
        string effectTag = meleeAttackEffectTag;

        // [버프 이펙트 적용] 물리 공격 이펙트가 바뀌어야 한다면 바뀐 걸 적용함
        var vfxBuff = activeBuffs.FirstOrDefault(b => b.MeleeAttackEffectOverride);
        if (vfxBuff != null)
        {
            effectPrefab = vfxBuff.MeleeAttackEffectOverride;
            effectTag = vfxBuff.SourceSkill.GetVFXPoolTag(this, effectPrefab);
        }

        // 이펙트 처리
        if (effectPrefab != null && !string.IsNullOrEmpty(effectTag))
        {
            // 이펙트가 보는 방향은 combatVFXController에서 설정

            GameObject? effectObj = ObjectPoolManager.Instance!.SpawnFromPool(
                   effectTag,
                   transform.position, // 이펙트 생성 위치
                   Quaternion.identity
           );

            if (effectObj != null)
            {
                CombatVFXController? combatVFXController = effectObj.GetComponent<CombatVFXController>();
                if (combatVFXController != null)
                {
                    combatVFXController.Initialize(attackSource, targetPosition, effectTag);
                }
            }
        }
    }

    public void InitializeRangedPool()
    {
        if (AttackRangeType == AttackRangeType.Ranged &&
            OperatorData.projectilePrefab != null)
        {
            projectileTag = $"{OperatorData.entityName}_Projectile";
            ObjectPoolManager.Instance!.CreatePool(projectileTag, OperatorData.projectilePrefab, 5);
        }

        if (OperatorData.muzzleVFXPrefab != null)
        {
            muzzleTag = $"{OperatorData.entityName}_Muzzle";
            ObjectPoolManager.Instance!.CreatePool(muzzleTag, OperatorData.muzzleVFXPrefab, 5);
            Debug.Log("muzzle 오브젝트 풀 생성됨");
        }
    }

    protected void RemoveProjectilePool()
    {
        if (AttackRangeType == AttackRangeType.Ranged)
        {
            if (projectileTag != null)
            {
                ObjectPoolManager.Instance!.RemovePool(projectileTag);
            }

            if (muzzleTag != null)
            {
                ObjectPoolManager.Instance!.RemovePool(muzzleTag);
            }
        }
        
    }

    // 지속 시간이 있는 스킬을 켜거나 끌 때 호출됨
    public void SetSkillOnState(bool skillOnState)
    {
        IsSkillOn = skillOnState;
    }

    // 방향 표시 UI 생성
    private void CreateDirectionIndicator()
    {
        // 자식 오브젝트로 들어감
        DirectionIndicator indicator = Instantiate(StageUIManager.Instance!.directionIndicator, transform).GetComponent<DirectionIndicator>();
        indicator.Initialize(this);

        // 오퍼레이터가 파괴될 때 함께 파괴되므로 전역변수로 설정하지 않아도 됨
    }

    protected override float CalculateActualDamage(AttackType attacktype, float incomingDamage)
    {
        float actualDamage = 0; // 할당까지 필수

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

        return Mathf.Max(actualDamage, 0.05f * incomingDamage); // 들어온 대미지의 5%는 들어가게끔 보장
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
        // 여전히 공격 범위 내에 해당 적이 있는가를 검사
        foreach (var gridPos in CurrentAttackableGridPos)
        {
            Tile? targetTile = MapManager.Instance!.GetTile(gridPos.x, gridPos.y);
            if (targetTile != null && targetTile.EnemiesOnTile.Contains(enemy))
            {
                return;
            }
        }

        // 공격 범위에서 완전히 이탈한 경우에 제거
        enemiesInRange.Remove(enemy);
        if (CurrentTarget == enemy)
        {
            CurrentTarget = null; // 현재 타겟이 나간 경우 null로 설정
        }
    }

    // 공격 범위 타일들에 이 오퍼레이터를 등록
    private void RegisterTiles()
    {
        foreach (Vector2Int eachPos in CurrentAttackableGridPos)
        {
            Tile? targetTile = MapManager.Instance!.GetTile(eachPos.x, eachPos.y);
            if (targetTile != null)
            {
                targetTile.RegisterOperator(this);

                // 타일 등록 시점에 그 타일에 있는 적의 정보도 Operator에게 전달함
                foreach (Enemy enemy in targetTile.EnemiesOnTile)
                {
                    OnEnemyEnteredAttackRange(enemy);
                }
            }
        }
    }

    // 공격 범위 타일들에 이 오퍼레이터를 등록 해제
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
        // Die 메서드에도 만들어놨지만 안전하게
        Enemy.OnEnemyDespawned -= HandleEnemyDespawn;

        if (_activeSkillCoroutine != null)
        {
            // 오브젝트 파괴 시 실행 중이던 스킬 코루틴을 중지시킨다
            StopCoroutine(_activeSkillCoroutine);
            _activeSkillCoroutine = null;
        }

        if (IsDeployed && MapManager.Instance != null)
        {
            UnregisterTiles();
        }
        
        if (operatorUIInstance != null)
        {
            Destroy(operatorUIInstance);
        }
    }

    public void SetMovementSpeed(float newMovementSpeed) { }

}
