using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using System;
using Skills.Base;

// Operator, Enemy, Barricade 등의 타일 위의 유닛들과 관련된 엔티티
public abstract class UnitEntity : MonoBehaviour, ITargettable, IFactionMember, ICrowdControlTarget
{
    public Faction Faction { get; protected set; }

    private UnitData _unitData;
    public UnitData UnitData => _unitData;

    // 세부 컨트롤러
    protected HealthController _health; 
    protected StatController _stat;
    protected BuffController _buff;

    // 컨트롤러 프로퍼티
    public IReadableHealthController Health => _health;
    public IReadableStatController Stat => _stat;
    public IReadableBuffController Buff => _buff; 

    // 이 개체를 공격하는 엔티티 목록
    protected List<ICombatEntity> attackingEntities = new List<ICombatEntity>();

    // 콜라이더는 자식으로 분리
    // 부모 오브젝트에서 콜라이더를 관리할 경우, 여러 개의 콜라이더 처리 시에 문제가 생긴다
    // 콜라이더는 용도에 따라 자식 오브젝트로 따로 둬야 한다.
    [SerializeField] protected BodyColliderController bodyColliderController;

    // 이 객체가 갖고 있는 메쉬 렌더러들
    [SerializeField] protected List<Renderer> renderers;
    protected List<Material> materialInstances = new List<Material>();
    protected Dictionary<Renderer, Color> originalEmissionColors = new Dictionary<Renderer, Color>();
    protected static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor"); // URP Lit 기준
    private Coroutine _flashCoroutine; // 피격 시 머티리얼 색 변하는 코루틴
    protected float flashDuration = .15f;
    protected Color flashColor = new Color(.3f, .3f, .3f, 1);

    // 사라지는 애니메이션 관련
    protected float currentAlpha = 1f;
    protected float endAlpha = 0f;
    protected float fadeDuration = .3f;
    
    protected MaterialPropertyBlock propBlock; // 모든 렌더러에 재사용 가능
    
    // 버프 관련
    // protected List<Buff> activeBuffs = new List<Buff>();
    // public ActionRestriction Restrictions { get; private set; } = ActionRestriction.None;

    // ICrowdControlTarget 인터페이스 구현
    // public virtual 
    public virtual float MovementSpeed { get; }
    public virtual void SetMovementSpeed(float newMovementSpeed) {}

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
    public event Action<UnitEntity> OnDeathAnimationCompleted = delegate { }; // 체력이 다해 죽었을 때 정상적인 이벤트 실행 
    public event Action<UnitEntity> OnDestroyed = delegate { }; // 어떤 경로로든 이 객체가 파괴될 때 실행, 위와 같이 쓰겠다면 중첩을 방지할 플래그를 쓰자.

    protected virtual void Awake()
    {
        // 메쉬 색상 설정
        propBlock = new MaterialPropertyBlock();

        // 시스템 생성(껍데기만 생성)
        _stat = new StatController();
        _health = new HealthController(_stat);

        // 사망 로직 연결
        _health.OnDeath += Die;

        // 갖고 있는 렌더러들 설정
        if (renderers.Count == 0)
        {
            Logger.LogError("UnitEntity에 할당된 Renderer가 없음");
            return;
        }
        else
        {
            foreach (var renderer in renderers)
            {
                Color originalColor = Color.black;
                // URP - Lit 사용한다고 가정
                if (renderer.sharedMaterial.IsKeywordEnabled("_EMISSION"))
                {
                    Logger.Log("셰이더의 Emission 키워드 사용 가능");
                    renderer.GetPropertyBlock(propBlock);
                    if (propBlock.GetColor(EmissionColorID) != Color.clear)
                    {
                        originalColor = propBlock.GetColor(EmissionColorID);
                    }
                    else
                    {
                        originalColor = renderer.sharedMaterial.GetColor(EmissionColorID);
                    }
                }

                originalEmissionColors.Add(renderer, originalColor);
            }
        }

        SetPoolTag();

        // 콜라이더가 켜지는 시점은 자식 클래스들에서 수동으로 구현함
    }

    public virtual void AddAttackingEntity(ICombatEntity attacker)
    {
        if (!attackingEntities.Contains(attacker))
        {
            attackingEntities.Add(attacker);
        }
    }

    public virtual void Initialize()
    {
        // SO 설정, 스탯 초기화(구현은 자식 클래스에서)
        InitializeUnitData();

        // 체력 초기화
        _health.Initialize();

        // 시각적 초기화
        InitializeVisuals();

        // 추가 로직
        OnInitialized();
    }

    // 해당 데이터 SO 할당 + _statContainer 초기화
    protected abstract void InitializeUnitData();
    protected virtual void OnInitialized() { } // 별도의 초기화 로직 

    protected virtual void Update()
    {
        _buff.UpdateBuffs();
    }

    // 이 개체를 공격하는 적을 제거
    public virtual void RemoveAttackingEntity(ICombatEntity attacker)
    {
        attackingEntities.Remove(attacker);
    }

    // 사망 애니메이션을 쓰지 않을 경우 이걸 사용
    protected void DieInstantly()
    {
        OnDeathStarted?.Invoke(this);

        OnDeathAnimationCompleted?.Invoke(this);
        if (PoolTag != string.Empty || PoolTag != null)
        {
            Logger.Log($"{PoolTag}에서 풀로 돌아가는 동작 수행됨");
            ObjectPoolManager.Instance.ReturnToPool(PoolTag, gameObject);
        }
        return;
        
    }

    protected virtual void Die()
    {
        PlayDeathAnimation();
    }

    protected void PlayDeathAnimation()
    {
        OnDeathStarted?.Invoke(this);

        if (renderers.Count == 0)
        {
            OnDeathAnimationCompleted?.Invoke(this);
            if (PoolTag != string.Empty)
            {
                Logger.Log($"{PoolTag}에서 풀로 돌아가는 동작 수행됨");
                ObjectPoolManager.Instance.ReturnToPool(PoolTag, gameObject);
            }
            return;
        }

        DOTween.To(() => currentAlpha, x => currentAlpha = x, endAlpha, fadeDuration) // 
            .OnUpdate(() =>
            {
                foreach (Renderer renderer in renderers)
                {
                    renderer.GetPropertyBlock(propBlock);
                    propBlock.SetFloat("_FadeAmount", currentAlpha);
                    renderer.SetPropertyBlock(propBlock);
                }
            })
            .OnComplete(() =>
            {
                OnDeathAnimationCompleted?.Invoke(this);
                if (PoolTag != string.Empty)
                {
                    Logger.Log($"{PoolTag}에서 풀로 돌아가는 동작 수행됨");
                    ObjectPoolManager.Instance.ReturnToPool(PoolTag, gameObject);
                }
            }
        );
    }

    protected void InitializeVisuals()
    {
        foreach (Renderer renderer in renderers)
        {
            renderer.GetPropertyBlock(propBlock);
            propBlock.SetFloat("_FadeAmount", 1f);
            renderer.SetPropertyBlock(propBlock);
        }
    }

    protected virtual void SetPoolTag() {}

    // protected abstract void InitializeHP();

    public virtual void TakeHeal(AttackSource attackSource)
    {
        float healAmount = _health.ProcessHeal(attackSource);

        // 태그 값이 있다면
        if (!string.IsNullOrEmpty(attackSource.HitEffectTag))
        {
            PlayGetHitEffect(attackSource);
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

    protected virtual void AssignColorToRenderers(Color primaryColor, Color secondaryColor)
    {
        if (renderers.Count > 0)
        {
            renderers[0].GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", primaryColor); // URP Lit 기준
            renderers[0].SetPropertyBlock(propBlock);
        }
            
        if (renderers.Count > 1)
        {
            renderers[1].GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", secondaryColor); // URP Lit 기준
            renderers[1].SetPropertyBlock(propBlock);
        }
    }

    protected virtual void PlayGetHitEffect(AttackSource attackSource)
    {
        string sourceHitEffectTag = attackSource.HitEffectTag;
        
        if (sourceHitEffectTag != string.Empty)
        {
            Vector3 effectPosition = transform.position;
            GameObject? hitEffect = ObjectPoolManager.Instance!.SpawnFromPool(sourceHitEffectTag, effectPosition, Quaternion.identity);

            if (hitEffect != null)
            {
                CombatVFXController hitVFXController = hitEffect.GetComponent<CombatVFXController>();
                hitVFXController.Initialize(attackSource, this, attackSource.HitEffectTag);
            }
        }
    }

    public virtual void TakeDamage(AttackSource source, bool playHitVFX = true)
    {
        if (_health.CurrentHealth <= 0) return;
        
        // 대미지 계산
        float damageTaken = _health.ProcessDamage(source);

        // 피격 이펙트 - GetHit이 없더라도 피격당한 오브젝트의 반짝이는 효과
        // 나중에 모델 시스템에서 처리
        if (_flashCoroutine != null)
        {
            StopCoroutine(_flashCoroutine);
            _flashCoroutine = null;
        }
        _flashCoroutine = StartCoroutine(PlayTakeDamageVFX());

        // 피격 이펙트 재생 - 프리팹, 태그 모두 있을 때만 실행됨
        // 나중에 이펙트 시스템에서 처리(이건 정확히 어디서 처리해야 하는지 모르겠다)
        if (playHitVFX)
        {
            PlayGetHitEffect(source);
        }

        // 대미지 팝업
        // 얘가 직접 알린다? 이벤트로 발생시킨다? 
        if (source.ShowDamagePopup && damageTaken > 0)
        {
            ObjectPoolManager.Instance.ShowFloatingText(transform.position, damageTaken, false);
        }

        // 피격 시의 추가 동작
        OnDamageTaken(source.Attacker, damageTaken);
    }

    protected virtual void OnDamageTaken(UnitEntity attacker, float actualDamage) { } // 피격 시에 추가로 실행할 게 있을 때 사용할 메서드 

    // 콜라이더의 활성화 여부 결정
    protected virtual void SetColliderState(bool enabled)
    {
        if (bodyColliderController != null) bodyColliderController.SetColliderState(enabled);
    }

    // 해쉬 셋으로 받겠다는 약속이 있다면 굳이 인터페이스로 인풋을 받을 필요는 없다
    public void SetCurrentSkillRange(HashSet<Vector2Int> range)
    {
        this.currentSkillRange = new HashSet<Vector2Int>(range);
    }

    public void SetLastSkillCenter(Vector2Int center)
    {
        LastSkillCenter = center;
    }

    public IReadOnlyCollection<Vector2Int> GetCurrentSkillRange()
    {
        return currentSkillRange;
    }

    public void ExecuteSkillSequence(IEnumerator skillCoroutine)
    {
        StartCoroutine(skillCoroutine);
    }

    private IEnumerator PlayTakeDamageVFX()
    {
        foreach (Renderer renderer in renderers)
        {
            // 현재 렌더러의 프로퍼티 블록 상태를 가져옴 (다른 프로퍼티 유지를 위해)
            renderer.GetPropertyBlock(propBlock);
            // Emission 색상만 덮어씀
            propBlock.SetColor(EmissionColorID, flashColor);
            renderer.SetPropertyBlock(propBlock);
        }

        yield return new WaitForSeconds(flashDuration);

        foreach (var renderer in renderers)
        {
            // Dictionary에서 해당 렌더러의 원래 색상을 찾아옴
            Color originalColor = originalEmissionColors[renderer];
            
            renderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(EmissionColorID, originalColor);
            renderer.SetPropertyBlock(propBlock);
        }

        _flashCoroutine = null;
    }


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

    protected virtual void OnDestroy()
    {
        OnDestroyed?.Invoke(this);
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
