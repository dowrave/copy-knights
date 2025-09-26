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

    protected GameObject prefab;
    public GameObject Prefab => prefab;
    public ShieldSystem shieldSystem = default!;

    // 스탯 관련
    private float _currentHealth;
    public float CurrentHealth
    {
        get => _currentHealth;
        protected set
        {
            _currentHealth = Mathf.Clamp(value, 0, MaxHealth); // 0 ~ 최대 체력 사이로 값 유지
            OnHealthChanged?.Invoke(_currentHealth, MaxHealth, shieldSystem.CurrentShield);
        }
    }

    public float MaxHealth { get; protected set; }

    // 이 개체를 공격하는 엔티티 목록
    protected List<ICombatEntity> attackingEntities = new List<ICombatEntity>();

    // 콜라이더는 자식으로 분리
    // 부모 오브젝트에서 콜라이더를 관리할 경우, 여러 개의 콜라이더 처리 시에 문제가 생긴다
    // 콜라이더는 용도에 따라 자식 오브젝트로 따로 둬야 한다.
    [SerializeField] protected BodyColliderController bodyColliderController;

    // 이 객체가 갖고 있는 메쉬 렌더러들
    [SerializeField] protected Renderer primaryRenderer; 
    [SerializeField] protected Renderer secondaryRenderer; 
    protected MaterialPropertyBlock propBlock; // 모든 렌더러에 재사용 가능

    // 버프 관련
    protected List<Buff> activeBuffs = new List<Buff>();

    public ActionRestriction Restrictions { get; private set; } = ActionRestriction.None;

    // ICrowdControlTarget 인터페이스 구현
    // public virtual 
    public virtual float MovementSpeed { get; }
    public virtual void SetMovementSpeed(float newMovementSpeed) {}

    // 스킬 관련
    protected HashSet<Vector2Int> currentSkillRange = new HashSet<Vector2Int>(); // 스킬 범위
    public Vector2Int LastSkillCenter { get; protected set; } // 마지막으로 사용한 스킬의 중심 위치. 범위 재계산 여부를 결정하기 위한 필드.

    // 이펙트 태그
    protected string? meleeAttackEffectTag;
    protected string hitEffectTag = string.Empty;
    protected string muzzleTag = string.Empty;
    public string HitEffectTag => hitEffectTag;

    // 이벤트
    public event Action<float, float, float> OnHealthChanged = delegate { };
    public event Action<Buff, bool> OnBuffChanged = delegate { }; // onCrowdControlChanged 대체 
    public event Action<UnitEntity> OnDeathAnimationCompleted = delegate { }; // 체력이 다해 죽었을 때 정상적인 이벤트 실행 
    public event Action<UnitEntity> OnDestroyed = delegate { }; // 어떤 경로로든 이 객체가 파괴될 때 실행, 위와 같이 쓰겠다면 중첩을 방지할 플래그를 쓰자.

    protected virtual void Awake()
    {
        // 메쉬 색상 설정
        propBlock = new MaterialPropertyBlock();

        // 쉴드 시스템 설정
        shieldSystem = new ShieldSystem();
        shieldSystem.OnShieldChanged += (shield, onShieldDepleted) =>
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, shield);
        };

        // 콜라이더가 켜지는 시점은 자식 클래스들에서 수동으로 구현함
    }

    public virtual void SetPrefab() { }

    public virtual void AddAttackingEntity(ICombatEntity attacker)
    {
        if (!attackingEntities.Contains(attacker))
        {
            attackingEntities.Add(attacker);
        }
    }

    protected virtual void Update()
    {
        foreach (var buff in activeBuffs.ToArray())
        {
            buff.OnUpdate();
        }
    }

    // 이 개체를 공격하는 적을 제거
    public virtual void RemoveAttackingEntity(ICombatEntity attacker)
    {
        attackingEntities.Remove(attacker);
    }


    protected virtual void Die()
    {
        PlayDeathAnimation();
    }

    protected void PlayDeathAnimation()
    {
        if (primaryRenderer == null)
        {
            OnDeathAnimationCompleted?.Invoke(this);
            Destroy(gameObject);
            return;
        }

        // 시퀀스로 만들어 여러 개의 애니메이션을 하나의 그룹으로 묶어서 관리한다
        Sequence deathSequence = DOTween.Sequence();

        List<Material> materialInstances = new List<Material>();
        List<Renderer> renderers = new List<Renderer>();
        if (primaryRenderer != null) renderers.Add(primaryRenderer);
        if (secondaryRenderer != null) renderers.Add(secondaryRenderer);

        foreach (Renderer renderer in renderers)
        {
            // 1. 머티리얼 인스턴스로 만들어 동일한 머티리얼을 사용하는 다른 객체에 영향을 주지 않게 한다
            Material materialInstance = new Material(renderer.material);
            renderer.material = materialInstance;
            materialInstances.Add(materialInstance);

            // 2. 투명 렌더링 모드로 전환
            SetMaterialToTransparent(materialInstance);

            // 3. 각 렌더러의 머티리얼에 대한 페이드 아웃 트윈을 생성
            Tween fadeTween = materialInstance.DOFade(0f, 0.2f);

            // 4. 시퀀스에 조인함. 
            deathSequence.Join(fadeTween);
        }

        deathSequence.OnComplete(() =>
        {
            OnDeathAnimationCompleted?.Invoke(this);

            foreach (Material mat in materialInstances)
            {
                Destroy(mat);
            }

            Destroy(gameObject);
        });

        // 시퀀스의 실행 시점은 위에서 모든 애니메이션이 등록됨 -> DOTween이 시퀀스를 감지함 -> 실행시킴이라서
        // 이 메서드가 호출되면 별도로 실행시킬 필요 없이 다음 프레임부터 시작된다.
    }

    // 머티리얼을 투명하게 설정하는 메서드 (URP Lit을 쓴다고 가정)
    private void SetMaterialToTransparent(Material material)
    {
        // URP Lit 셰이더를 Transparent 모드로 변경
        material.SetFloat("_Surface", 1f);      // 1 = Transparent
        material.SetFloat("_Blend", 0f);        // 0 = Alpha
        material.SetFloat("_AlphaClip", 0f);    // 알파 클리핑 비활성화

        // 블렌딩 모드 설정
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);       // 깊이 쓰기 비활성화

        // 렌더 큐를 투명 객체용으로 변경
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // 키워드 설정
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        material.DisableKeyword("_ALPHATEST_ON");
    }

    protected abstract void InitializeHP();

    public virtual void TakeHeal(AttackSource attackSource)
    {
        float oldHealth = CurrentHealth;
        CurrentHealth += attackSource.Damage;
        float actualHealAmount = Mathf.FloorToInt(CurrentHealth - oldHealth); // 실제 힐량

        if (attackSource.Attacker is MedicOperator medic && medic.OperatorData.hitEffectPrefab != null)
        {
            PlayGetHitEffect(attackSource);
            
            if (actualHealAmount > 0)
            {
                ObjectPoolManager.Instance!.ShowFloatingText(transform.position, actualHealAmount, true);
            }
        }



        if (attackSource.Attacker is Operator healerOperator)
        {
            StatisticsManager.Instance!.UpdateHealingDone(healerOperator.OperatorData, actualHealAmount);
        }
    }

    protected virtual void AssignColorToRenderers(Color primaryColor, Color secondaryColor)
    {
        if (primaryRenderer != null)
        {
            primaryRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", primaryColor); // URP Lit 기준
            primaryRenderer.SetPropertyBlock(propBlock);
        }
            
        if (secondaryRenderer != null)
        {
            secondaryRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", secondaryColor); // URP Lit 기준
            secondaryRenderer.SetPropertyBlock(propBlock);
        }
    }

    protected virtual void PlayGetHitEffect(AttackSource attackSource)
    {
        GameObject sourceHitEffectPrefab = attackSource.HitEffectPrefab;
        string sourceHitEffectTag = attackSource.HitEffectTag;

        if (sourceHitEffectPrefab == null || sourceHitEffectTag == string.Empty) return;

        string attackerName;

        if (attackSource.Attacker is Operator op)
        {
            OperatorData opData = op.OperatorData;
            attackerName = opData.entityName;
            if (sourceHitEffectPrefab == null)
            {
                sourceHitEffectPrefab = opData.hitEffectPrefab;
            }
        }
        else if (attackSource.Attacker is Enemy enemy)
        {
            EnemyData enemyData = enemy.BaseData;
            attackerName = enemyData.EntityName;
            if (sourceHitEffectPrefab == null)
            {
                sourceHitEffectPrefab = enemyData.HitEffectPrefab;
            }
        }
        else
        {
            Debug.LogError("이펙트 없음");
            return;
        }

        if (sourceHitEffectPrefab != null)
        {
            Vector3 effectPosition = transform.position;

            // 풀에서 이펙트 오브젝트 가져오기
            if (sourceHitEffectTag == string.Empty)
            {
                Debug.LogWarning("[TakeDamage] hitEffectTag 값이 null임");
            }

            GameObject? hitEffect = ObjectPoolManager.Instance!.SpawnFromPool(sourceHitEffectTag, effectPosition, Quaternion.identity);

            if (hitEffect != null)
            {
                CombatVFXController hitVFXController = hitEffect.GetComponent<CombatVFXController>();
                hitVFXController.Initialize(attackSource, this, sourceHitEffectTag);
            }
        }
    }

    // 스킬 등으로 인한 현재 체력 변경 시 이 메서드를 사용
    public void ChangeCurrentHealth(float newCurrentHealth)
    {
        CurrentHealth = Mathf.Floor(newCurrentHealth);
    }
    public void ChangeMaxHealth(float newMaxHealth)
    {
        MaxHealth = Mathf.Floor(newMaxHealth);
    }

    public void AddBuff(Buff buff)
    {
        // 스턴의 경우 새로 걸리면 기존 스턴은 제거됨
        if (buff is StunBuff stunBuff)
        {
            Buff existingStun = activeBuffs.FirstOrDefault(b => b is StunBuff);
            if (existingStun != null)
            {
                RemoveBuff(existingStun);
            }
        }

        activeBuffs.Add(buff);
        buff.OnApply(this, buff.caster);
        OnBuffChanged?.Invoke(buff, true); // 이벤트 호출 
    }

    public void RemoveBuff(Buff buff)
    {
        if (activeBuffs.Contains(buff))
        {
            buff.OnRemove(); // 만약 연결된 다른 버프들이 있다면 여기서 먼저 제거됨
            if (activeBuffs.Remove(buff))
            {
                OnBuffChanged?.Invoke(buff, false);
            }
        }
    }

    // 버프 중복 적용 방지를 위한 버프 타입 헬퍼 메서드 추가
    public bool HasBuff<T>() where T : Buff
    {
        return activeBuffs.Any(b => b is T);
    }

    public T? GetBuff<T>() where T : Buff
    {
        return activeBuffs.FirstOrDefault(b => b is T) as T;
    }

    protected virtual void RemoveAllBuffs()
    {
        foreach (var buff in activeBuffs.ToList())
        {
            RemoveBuff(buff);
        }
    }

    public virtual void RemoveBuffFromSourceSkill(OperatorSkill sourceSkill)
    {
        var buffsToRemove = activeBuffs.Where(b => b.SourceSkill == sourceSkill).ToList();
        foreach (var buff in activeBuffs.ToList())
        {
            RemoveBuff(buff);
        }
    }


    public virtual void TakeDamage(AttackSource source)
    {
        // 현재 체력이 0 이하라면 실행되지 않는다
        // 중복해서 실행되는 경우를 방지함
        if (CurrentHealth <= 0) return;

        // 방어력 / 마법 저항력이 고려된 실제 들어오는 대미지
        float actualDamage = Mathf.Floor(CalculateActualDamage(source.Type, source.Damage));

        // 쉴드를 깎고 남은 대미지
        float remainingDamage = shieldSystem.AbsorbDamage(actualDamage);

        // 체력 계산
        CurrentHealth = Mathf.Max(0, CurrentHealth - remainingDamage);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, shieldSystem.CurrentShield);

        // 피격 이펙트 재생 - 프리팹, 태그 모두 있을 때만 실행됨
        PlayGetHitEffect(source);

        // 대미지 팝업
        if (source.ShowDamagePopup)
        {
            ObjectPoolManager.Instance.ShowFloatingText(transform.position, remainingDamage, false);
        }

        // 피격 시의 추가 동작
        OnDamageTaken(source.Attacker, actualDamage);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void OnDamageTaken(UnitEntity attacker, float actualDamage) { } // 피격 시에 추가로 실행할 게 있을 때 사용할 메서드 

    // 콜라이더의 활성화 여부 결정
    protected virtual void SetColliderState(bool enabled)
    {
        if (bodyColliderController != null) bodyColliderController.SetColliderState(enabled);
    }

    public void AddRestriction(ActionRestriction restirction)
    {
        Restrictions |= restirction; // 비트 OR 연산으로 플래그 추가
    }

    public void RemoveRestriction(ActionRestriction restirction)
    {
        Restrictions &= ~restirction; // AND, NOT 연산으로 플래그 제거
    }
    public bool HasRestriction(ActionRestriction restirction)
    {
        return (Restrictions & restirction) != 0; // 겹치는 비트가 있으면 true, 없으면 false.
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


    public virtual void OnBodyTriggerEnter(Collider other) { }
    public virtual void OnBodyTriggerExit(Collider other) {}


    protected abstract float CalculateActualDamage(AttackType attacktype, float incomingDamage);
    public void ActivateShield(float amount) => shieldSystem.ActivateShield(amount);
    public void DeactivateShield() => shieldSystem.DeactivateShield();
    public float GetCurrentShield() => shieldSystem.CurrentShield;

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
