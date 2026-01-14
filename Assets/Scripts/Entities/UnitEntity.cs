using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Skills.Base;

// Operator, Enemy, Barricade 등의 타일 위의 유닛들과 관련된 엔티티
public abstract class UnitEntity : MonoBehaviour, IFactionMember
{
    public Faction Faction { get; protected set; }

    private UnitData _unitData;
    public UnitData UnitData => _unitData;

    // 세부 컨트롤러
    protected HealthController _health; 
    protected StatController _stat;
    protected BuffController _buff;

    // MonoBehaviour로 설정하는 컨트롤러들
    [SerializeField] protected BodyColliderController _collider;
    [SerializeField] protected VisualController _visual;

    // 컨트롤러 프로퍼티
    public IHealthReadOnly Health => _health;
    public IStatReadOnly Stat => _stat;
    public IBuffReadOnly Buff => _buff; 

    // 이 개체를 공격하는 엔티티 목록
    protected List<ICombatEntity> attackingEntities = new List<ICombatEntity>();

    // 스킬 관련
    protected HashSet<Vector2Int> currentSkillRange = new HashSet<Vector2Int>(); // 스킬 범위
    public Vector2Int LastSkillCenter { get; protected set; } // 마지막으로 사용한 스킬의 중심 위치. 범위 재계산 여부를 결정하기 위한 필드.

    // 유닛 자신의 풀 태그
    public string PoolTag {get; protected set;}

    // 이펙트 태그
    protected string? meleeAttackEffectTag;
    protected string hitEffectTag = string.Empty;
    protected string muzzleTag = string.Empty;
    public string HitEffectTag => hitEffectTag;

    // 이벤트
    // public event Action<Buff, bool> OnBuffChanged = delegate { }; // onCrowdControlChanged 대체 
    public event Action<UnitEntity> OnDeathStarted = delegate { }; // 사망 판정 발생 시 발생하는 이벤트
    public event Action<UnitEntity> OnDisabled = delegate { }; // 어떤 경로로든 이 객체가 비활성화 / 파괴될 때 실행

    protected virtual void Awake()
    {
        // 시스템 생성(껍데기만 생성)
        _stat = new StatController();
        _health = new HealthController(_stat);
        _buff = new BuffController(this);

        _collider ??= GetComponentInChildren<BodyColliderController>();
        if (_collider == null) Logger.LogError($"{gameObject.name}의 _collider가 설정되지 않음");
        _visual ??= GetComponentInChildren<VisualController>();
        if (_visual == null) Logger.LogError($"{gameObject.name}의 _visual이 설정되지 않음");

        // 사망 로직 연결
        _health.OnDeath += HandleOnDeath;

        SetPoolTag();
    }

    public virtual void Initialize()
    {
        // SO 필드 할당은 Initialize의 SO 파라미터를 받는 Wrapper 메서드를 만들어서 할당

        // SO를 이용한 초기화
        ApplyUnitData();

        InitializeVisual();

        // 추가 로직
        OnInitialized();
    }


    // SO 필드를 이용 및 다른 컨트롤러를 이용한 초기화
    // virtual로 구현하면 중첩 등에 의해 헷갈릴 여지가 있어서 abstract로 구현함
    protected abstract void ApplyUnitData();
    protected virtual void OnInitialized() { } // 별도의 초기화 로직 



    #region Visual API
    protected void InitializeVisual()
    {
        CommonVisualLogic();
        SpecificVisualLogic();
    }

    protected void CommonVisualLogic()
    {
        _visual.Initialize(this);
        _visual.OnDeathAnimationComplete += ReturnToPool;
    }

    protected virtual void SpecificVisualLogic(){ }
    #endregion


    #region Death Logic
    // 사망 시 처리할 로직
    protected virtual void HandleOnDeath() { }

    // 사망 애니메이션
    protected void DieWithAnimation()
    {
        OnDeathStarted?.Invoke(this);
        _visual.PlayDeathAnimation();
    }

    // 사망 애니메이션을 쓰지 않을 경우 사용(Retreat 등)
    protected void DieInstantly()
    {
        OnDeathStarted?.Invoke(this);
        ReturnToPool();
    }
    #endregion

    protected virtual void Update()
    {
        _buff.UpdateBuffs();
    }

    protected void ReturnToPool()
    {
        if (PoolTag != string.Empty || PoolTag != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(PoolTag, gameObject);
        }
        else
        {
            Logger.LogError($"{gameObject.name}의 ReturnToPool이 정상적으로 동작하지 않았음");
        }
        
    }

    public virtual void TakeHeal(AttackSource attackSource)
    {
        float healAmount = _health.ProcessHeal(attackSource);

        // 태그 값이 있다면
        if (!string.IsNullOrEmpty(attackSource.HitEffectTag))
        {
            _visual.PlayGetHitVFX(attackSource, this);
        }

        // 힐 값 표시
        if (healAmount > 0)
        {
            ObjectPoolManager.Instance!.ShowFloatingText(transform.position, healAmount, true);
        }

        // 통계 패널에 값 전달
        if (attackSource.Attacker is Operator healerOperator)
        {
            StatisticsManager.Instance!.UpdateHealingDone(healerOperator.OperatorData, healAmount);
        }
    }

    public virtual void TakeDamage(AttackSource source, bool playHitVFX = true)
    {
        if (_health.CurrentHealth <= 0) return;
        
        // 대미지 계산
        float damageTaken = _health.ProcessDamage(source);

        // 시각적 표현 - 공격자의 GetHit, 피격자의 반짝이는 효과 모두 Visual에서 실행
        if (playHitVFX && _visual != null)
        {
            _visual.PlayHitFeedback(source, this);
        }

        // 대미지 팝업 
        if (source.ShowDamagePopup && damageTaken > 0)
        {
            ObjectPoolManager.Instance.ShowFloatingText(transform.position, damageTaken, false);
        }

        // 피격 시의 추가 동작
        OnDamageTaken(source.Attacker, damageTaken);
    }

    // 피격 시에 추가로 실행할 게 있을 때 사용할 메서드 
    protected virtual void OnDamageTaken(UnitEntity attacker, float actualDamage) { } 



    // 해쉬 셋으로 받겠다는 약속이 있다면 굳이 인터페이스로 인풋을 받을 필요는 없다
    public void SetCurrentSkillRange(HashSet<Vector2Int> range)
    {
        this.currentSkillRange = new HashSet<Vector2Int>(range);
    }

    public void SetLastSkillCenter(Vector2Int center)
    {
        LastSkillCenter = center;
    }

    // 이거는 생각좀 해봅시다
    // 보스 스킬에서 사용하는 로직 - 스킬을 시전하는 중에 멈추게 하기
    public void ExecuteSkillSequence(IEnumerator skillCoroutine)
    {
        StartCoroutine(skillCoroutine);
    }

    // 자신을 공격하는 적 추가
    public virtual void AddAttackingEntity(ICombatEntity attacker)
    {
        if (!attackingEntities.Contains(attacker))
        {
            attackingEntities.Add(attacker);
        }
    }

    // 자신을 공격하는 적 제거
    public virtual void RemoveAttackingEntity(ICombatEntity attacker)
    {
        attackingEntities.Remove(attacker);
    }

    protected virtual void SetPoolTag() {} // 이 유닛의 오브젝트 풀 태그
    public virtual void OnBodyTriggerEnter(Collider other) { }
    public virtual void OnBodyTriggerExit(Collider other) {}

    // 연결 메서드들
    // Health
    public float CurrentHealth => Health.CurrentHealth;
    public float MaxHealth => Health.MaxHealth;
    public void ActivateShield(float amount) => _health.ActivateShield(amount);
    public void DeactivateShield() => _health.DeactivateShield();

    // Stat
    public float GetStat(StatType type) => Stat.GetStat(type); 
    public void AddStatModifier(StatType type, float modifier) => _stat.AddModifier(type, modifier);
    public void RemoveStatModifier(StatType type, float modifier) => _stat.RemoveModifier(type, modifier);
    public void AddStatOverride(StatType type, float overrideValue) => _stat.SetOverride(type, overrideValue);
    public void RemoveStatOverride(StatType type) => _stat.RemoveOverride(type);

    // Buff
    public IReadOnlyList<Buff> ActiveBuffs => Buff.ActiveBuffs; 
    public bool HasBuff<T>() where T : Buff => Buff.HasBuff<T>();
    public T? GetBuff<T>() where T : Buff => Buff.GetBuff<T>();
    public bool HasRestriction(ActionRestriction restirction) => (Buff.Restrictions & restirction) != 0;
    public void AddBuff(Buff buff) => _buff.AddBuff(buff);
    public void RemoveBuff(Buff buff) => _buff.RemoveBuff(buff);
    protected void RemoveAllBuffs() => _buff.RemoveAllBuffs();
    public void RemoveBuffFromSourceSkill(OperatorSkill sourceSkill) => _buff.RemoveBuffFromSourceSkill(sourceSkill);

    // 콜라이더의 활성화 여부 결정
    protected virtual void SetColliderState(bool enabled) => _collider.SetState(enabled);

    public IReadOnlyCollection<Vector2Int> GetCurrentSkillRange() => currentSkillRange;

    protected virtual void OnDisable()
    {
        _visual.OnDeathAnimationComplete -= ReturnToPool;
        OnDisabled?.Invoke(this);
    }

    protected void OnDestroy()
    {
        _health.OnDeath -= HandleOnDeath;
    }

    // 자식 클래스에서 구현할 값들 
    // 일일이 형변환시키지 않기 위해서 UnitEntity에서 구현해둠
    public virtual float AttackPower
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public virtual float AttackSpeed // 공격 쿨다운
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public virtual float Defense
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public virtual float MagicResistance
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public virtual AttackType AttackType
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }
}
