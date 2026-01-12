using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using Skills.Base;


public class Operator : DeployableUnitEntity, ICombatEntity, ISkill
{
    [SerializeField] protected OperatorData _operatorData;
    public OperatorData OperatorData => _operatorData;

    private OpActionController _action;
    private OpBlockController _block;

    public OwnedOperator OwnedOp {get; protected set;} 

    // ICombatEntity 필드
    protected AttackType _currentAttackType;
    public override AttackType AttackType
    {
        get { return _currentAttackType; }
        set
        {
            _currentAttackType = value;
        }
    }
    public AttackRangeType AttackRangeType => OperatorData.AttackRangeType;

    // 스탯의 세터는 UnitEntity.()StatModifier 관련 메서드들로 진행 - 계수가 필요하므로 프로퍼티로 구현 못 함
    public override float AttackPower { get => Stat.GetStat(StatType.AttackPower); }
    public override float AttackSpeed { get => Stat.GetStat(StatType.AttackSpeed); }
    public override float Defense { get => Stat.GetStat(StatType.Defense); }
    public override float MagicResistance { get => Stat.GetStat(StatType.MagicResistance); }
    public int MaxBlockableEnemies { get => (int)Stat.GetStat(StatType.MaxBlockCount); }
    public float SPRecoveryRate { get => Stat.GetStat(StatType.SPRecoveryRate); }

    public float ActionCooldown => _action.ActionCooldown;
    public float ActionDuration => _action.ActionDuration;

    public IReadOnlyList<Vector2Int> CurrentActionableGridPos => _action.CurrentActionableGridPos;

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

    // 저지 관련
    public IReadOnlyList<Enemy> BlockableEnemies => _block.BlockableEnemies;
    public IReadOnlyList<Enemy> BlockedEnemies => _block.BlockedEnemies;

    // 공격 범위 내에 있는 적들 
    protected List<Enemy> enemiesInRange = new List<Enemy>();

    // 배치 순서

    // 현재 공격 대상
    public UnitEntity? CurrentTarget => _action.CurrentTarget;

    // UI 필드
    protected GameObject operatorUIInstance = default!;
    protected OperatorUI? operatorUI;
    public OperatorUI? OperatorUI => operatorUI;
    protected DirectionIndicator _directionIndicator;

    // 스킬 관련
    public OperatorSkill CurrentSkill { get; protected set; } = default!;
    protected bool _isSkillOn;
    public bool IsSkillOn
    {
        get => _isSkillOn;
        protected set
        {
            if (_isSkillOn != value)
            {
                _isSkillOn = value;
                OnSkillStateChanged?.Invoke();
            }
        }
    }
    protected Coroutine _activeSkillCoroutine; // 지속시간이 있는 스킬에 해당하는 코루틴

    // 현재 오퍼레이터의 육성 상태 - Current를 별도로 붙이지는 않겠음
    public OperatorElitePhase ElitePhase { get; protected set; }
    public int Level { get; protected set; }

    // 이벤트들
    public event System.Action<float, float> OnSPChanged = delegate { };
    public event System.Action OnStatsChanged = delegate { };
    public event System.Action OnSkillStateChanged = delegate { };

    protected override void Awake()
    {
        base.Awake();

        if (OperatorData.OperatorClass == OperatorClass.Medic)
        {
            _action = new OpHealController();
        }
        else if (OperatorData.OperatorClass == OperatorClass.DualBlade)
        {
            _action = new DualBladeAttackController();
        }
        else
        {
            _action = new OpAttackController();
        }

        _block = new OpBlockController();

        CreateDirectionIndicator();
        CreateOperatorUI();
    }

    public override void Initialize(DeployableInfo opInfo)
    {
        DeployableInfo = opInfo;

        if (opInfo.ownedOperator == null) 
        {
            Logger.LogError("오퍼레이터의 ownedOperator 정보가 없음!");
            return;
        }

        OwnedOp = opInfo.ownedOperator;
        if (_operatorData == null) _operatorData = OwnedOp.OperatorData;
        base.Initialize();
    }

    // base.Initialize 템플릿 메서드 1
    protected override void ApplyUnitData()
    {
        // 데이터를 이용해 스탯 초기화
        _stat.Initialize(OwnedOp);
        _health.Initialize();
        _deployment.Initialize(_operatorData); // _action보다 먼저
        _block.Initialize(this); // _action보다 먼저
        _action.Initialize(this);

        _currentAttackType = _operatorData.AttackType;
    }

    // base.Initialize 템플릿 메서드 2
    protected override void SpecificVisualLogic()
    {
        _visual.AssignColorToRenderers(OperatorData.PrimaryColor, OperatorData.SecondaryColor);
    }

    // base.Initialize 템플릿 메서드 3
    protected override void OnInitialized()
    {
        CurrentSP = OperatorData.InitialSP;

        // 스킬 설정
        if (DeployableInfo.skillIndex.HasValue)
        {
            CurrentSkill = OwnedOp.UnlockedSkills[DeployableInfo.skillIndex.Value];
        }
        else
        {
            throw new System.InvalidOperationException("인덱스가 없어서 CurrentSkill이 지정되지 않음");
        }

        MaxSP = CurrentSkill?.SPCost ?? 0f;

        ElitePhase = OwnedOp.CurrentPhase;
        Level = OwnedOp.CurrentLevel;

        _deployment.SetDeployState(false);
    }


    // Awake에서 동작, 미리 OperatorUI를 만들어둠
    protected void CreateOperatorUI()
    {
        operatorUIInstance = Instantiate(StageUIManager.Instance.OperatorUIPrefab);
        operatorUI = operatorUIInstance.GetComponent<OperatorUI>();

        DisableOperatorUI();
    }
    
    protected void InitializeOperatorUI()
    {
        if (operatorUI != null)
        {
            operatorUI.gameObject.SetActive(true);
            operatorUI.Initialize(this);
        }
    }
    
    protected void DisableOperatorUI()
    {
        operatorUI.gameObject.SetActive(false);
    }

    protected override void Update()
    {
        if (IsDeployed && StageManager.Instance!.CurrentGameState == GameState.Battle)
        {
            // ----- 상태 갱신 로직. 행동 제약과 무관 -----
            HandleSPRecovery(); // SP 회복

            base.Update(); // 버프 효과의 갱신
            _action.OnUpdate();

            CurrentSkill.OnUpdate(this); // 스킬에서도 감시

            // 행동 불능 상태 체크
            if (HasRestriction(ActionRestriction.CannotAction)) return;

            // ----- 행동 가능 상태의 로직 -----
            // 동작을 아예 못하는 상황과 구분한다. 예를 들면 기절 상태인데 스킬을 자동으로 켤 수는 없음.
            HandleSkillAutoActivate();
        }
    }

    protected virtual void HandleSkillAutoActivate()
    {
        if (CurrentSkill != null && CurrentSkill.autoActivate && CanUseSkill())
        {
            UseSkill();
        }
    }

    // 원거리인 경우에만 사용. Muzzle 이펙트를 실행한다.
    protected void PlayMuzzleVFX()
    {
        if (_operatorData.MuzzleVFXPrefab != null && muzzleTag != string.Empty)
        {
            GameObject muzzleVFXObject = ObjectPoolManager.Instance!.SpawnFromPool(_operatorData.MuzzleVFXTag, transform.position, transform.rotation);
            MuzzleVFXController muzzleVFXController = muzzleVFXObject.GetComponentInChildren<MuzzleVFXController>();
            muzzleVFXController.Initialize(muzzleTag);
        }
    }

    // SP 자동회복 로직
    protected void HandleSPRecovery()
    {
        if (IsDeployed == false || CurrentSkill == null) { return; }

        // 자동회복일 때만 처리
        if (CurrentSkill.autoRecover)
        {
            float oldSP = CurrentSP;

            // 최대 SP 초과 방지 (이벤트는 자체 발생)
            CurrentSP = Mathf.Min(CurrentSP + SPRecoveryRate * Time.deltaTime, MaxSP);
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
            _block.OnEnemyEnteredBlockRange(enemy);
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
            _block.OnEnemyExitedBlockRange(enemy);
        }
    }

    // 부모 클래스에서 정의된 TakeDamage에서 사용됨
    protected override void OnDamageTaken(UnitEntity attacker, float actualDamage)
    {
        StatisticsManager.Instance!.UpdateDamageTaken(OperatorData, actualDamage);
    }

    protected override void UndeployAdditionalProcess()
    {
        
    }

    protected override void SetPoolTag()
    {
        PoolTag = _operatorData.UnitTag;
        // Logger.Log($"Pooltag : {PoolTag}로 할당됨");
    }

    public override void OnClick()
    {
        base.OnClick();
        if (IsDeployed)
        {
            HighlightAttackRange();
        }
    }

    // 시각화 기능 - 일단 컨테이너에서 유지
    public void HighlightAttackRange()
    {
        List<Vector2Int> gridPosToHighlight;
        List<Tile> tilesToHighlight = new List<Tile>();

        // 배치되지 않은 경우는 현재 위치 / 방향을 계산해서 받음
        if (!IsDeployed)
        {
            gridPosToHighlight = _action.GetActionableGridPos();
        }
        else
        {
            gridPosToHighlight = new List<Vector2Int>(CurrentActionableGridPos);
        }

        foreach (Vector2Int eachPos in gridPosToHighlight)
        {
            Tile? targetTile = MapManager.Instance!.CurrentMap!.GetTile(eachPos.x, eachPos.y);
            if (targetTile != null)
            {
                tilesToHighlight.Add(targetTile);
            }
        }

        // 템플릿 메서드
        HighlightAttackRanges(tilesToHighlight);
    }

    protected virtual void HighlightAttackRanges(List<Tile> tiles)
    {
        DeployableManager.Instance!.HighlightAttackRanges(tiles, false);
    }

    public override void Deploy(Vector3 position, Vector3? facingDirection = null)
    {
        ClearStates();
        base.Deploy(position, facingDirection); // DeployAdditionalProcess 이후 OnDeploy?.Invoke 호출됨
    }

    // base.Deploy()에 들어가는 로직들
    // _deployment.Deploy() 동작 후 실행됨
    protected override void DeployAdditionalProcess()
    {
        _action.OnDeploy();

        if (_directionIndicator != null && !_directionIndicator.isActiveAndEnabled)
        {
            _directionIndicator.gameObject.SetActive(true);
        }

        // deployableInfo의 배치된 오퍼레이터를 이것으로 지정
        DeployableInfo.deployedOperator = this;

        InitializeOperatorUI();
        InitializeDirectionIndicator();

        // 배치 이펙트 실행
        StartCoroutine(PlayDeployVFX());

        // 적 사망 이벤트 구독
        Enemy.OnEnemyDespawned += HandleEnemyDespawn;
    }

    protected IEnumerator PlayDeployVFX()
    {
        // 배치 VFX 실행 
        if (OperatorData.DeployEffectPrefab != null)
        {
            GameObject deployEffect = ObjectPoolManager.Instance.SpawnFromPool(
                OperatorData.DeployVFXTag,
                transform.position,
                Quaternion.identity
            );

            var ps = deployEffect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play(true);

            }
            else
            {
                var vfx = deployEffect.GetComponent<VisualEffect>();
                if (vfx != null)
                {
                    vfx.Play();
                }
            }
            yield return new WaitForSeconds(1.5f);
            ObjectPoolManager.Instance.ReturnToPool(OperatorData.DeployVFXTag, deployEffect);
        }
    }

    protected void ClearStates()
    {
        _action.ResetStates();
        _block.ResetStates();
    }

    // 적 디스폰 이벤트를 받았을 때의 처리
    protected void HandleEnemyDespawn(Enemy enemy, EnemyDespawnReason reason)
    {
        if (CurrentTarget == enemy)
        {
            _action.OnTargetDespawn(enemy);
        }

        // 저지 처리
        _block.OnEnemyExitedBlockRange(enemy);
    }

    // ISkill 메서드
    public bool CanUseSkill()
    {
        return IsDeployed && CurrentSP >= MaxSP && !IsSkillOn;
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

    public virtual void PlayMeleeAttackEffect(UnitEntity target, AttackSource attackSource)
    {
        Vector3 targetPosition = target.transform.position;
        PlayMeleeAttackEffect(targetPosition, attackSource);
    }

    public virtual void PlayMeleeAttackEffect(Vector3 targetPosition, AttackSource attackSource)
    {
        // 공격 이펙트(명중 이펙트가 아님 유의!!)
        string effectTag = _operatorData.MeleeAttackVFXTag;

        // [버프 이펙트 적용] 물리 공격 이펙트가 바뀌어야 한다면 바뀐 걸 적용함
        // 이 코드의 전제 조건은 "근접 공격 이펙트를 쓰는 다른 버프가 없다"이다. 상황이 바뀌면 코드를 바꿔야 함.
        var vfxBuff = ActiveBuffs.FirstOrDefault(b => b.MeleeAttackVFXOverride != null);
        if (vfxBuff != null)
        {
            effectTag = vfxBuff.SourceSkill.MeleeAttackVFXTag;
        }

        // 이펙트 처리
        if (!string.IsNullOrEmpty(effectTag))
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

    // 지속 시간이 있는 스킬을 켜거나 끌 때 호출됨
    public void SetSkillOnState(bool skillOnState)
    {
        IsSkillOn = skillOnState;
    }

    // 방향 표시 UI 생성
    protected void CreateDirectionIndicator()
    {
        // StageUIManager에 할당된 프리팹을 자식 오브젝트로 생성
        _directionIndicator = Instantiate(StageUIManager.Instance!.DirectionIndicator, transform).GetComponent<DirectionIndicator>();
        _directionIndicator.gameObject.SetActive(false);
    }

    protected void InitializeDirectionIndicator()
    {
        _directionIndicator.Initialize(this);
    }

    // 컨트롤러 public 세터 프로퍼티

    public void OnEnemyEnteredAttackRange(Enemy enemy)
    {
        _action.OnEnemyEnteredRange(enemy);
    }

    public void OnEnemyExitedAttackRange(Enemy enemy)
    {
        _action.OnEnemyExitedRange(enemy);
    }

    public void OnEnemyEnteredBlockRange(Enemy enemy)
    {
        _block.OnEnemyEnteredBlockRange(enemy);
    }

    public void OnEnemyExitedBlockRange(Enemy enemy)
    {
        _block.OnEnemyExitedBlockRange(enemy);
    }

    public void SetActionDuration(float? intentionalCooldown = null)
    {
        _action.SetActionDuration(intentionalCooldown);
    }

    public void SetActionCooldown(float? intentionalCooldown = null)
    {
        _action.SetActionCooldown(intentionalCooldown);
    }

    public void PerformAction(UnitEntity target, float value)
    {
        _action.PerformAction(target, value);
    }

    public void SetActionableGridPos(List<Vector2Int> newGridPositions)
    {
        _action.SetActionableGridPos(newGridPositions);
    }

    protected override void OnDisable()
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
            _action.OnDisabled();
        }

        if (operatorUI != null)
        {
            operatorUI.gameObject.SetActive(false);
        }

        base.OnDisable();
    }
}
