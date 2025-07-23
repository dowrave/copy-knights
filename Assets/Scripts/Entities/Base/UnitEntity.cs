using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using System;

// Operator, Enemy, Barricade 등의 타일 위의 유닛들과 관련된 엔티티
public abstract class UnitEntity : MonoBehaviour, ITargettable, IFactionMember, ICrowdControlTarget
{
    public Faction Faction { get; protected set; }

    public GameObject Prefab { get; protected set; } = default!;
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

    // 이 개체를 공격하는 엔티티 목록 : 이 개체에 변화가 생겼을 때 알리기 위해 필요함(사망, 은신 등등)
    protected List<ICombatEntity> attackingEntities = new List<ICombatEntity>();

    // 콜라이더는 자식으로 분리
    // 부모 오브젝트에서 콜라이더를 관리할 경우, 여러 개의 콜라이더 처리 시에 문제가 생긴다
    // 콜라이더는 용도에 따라 자식 오브젝트로 따로 둬야 한다.
    [SerializeField] protected BodyColliderController bodyColliderController;

    // 버프 관련
    protected List<Buff> activeBuffs = new List<Buff>();

    public ActionRestriction Restrictions { get; private set; } = ActionRestriction.None;

    // ICrowdControlTarget 인터페이스 구현
    // public virtual 
    public virtual float MovementSpeed { get; }
    public virtual void SetMovementSpeed(float newMovementSpeed) {}

    // 이벤트
    public event Action<float, float, float> OnHealthChanged = delegate { };
    public event Action<Buff, bool> OnBuffChanged = delegate { }; // onCrowdControlChanged 대체 
    public event Action<UnitEntity> OnDeathAnimationCompleted = delegate { }; // 체력이 다해 죽었을 때 정상적인 이벤트 실행 
    public event Action<UnitEntity> OnDestroyed = delegate { }; // 어떤 경로로든 이 객체가 파괴될 때 실행, 위와 같이 쓰겠다면 중첩을 방지할 플래그를 쓰자.

    protected virtual void Awake()
    {
        // 콜라이더가 켜지는 시점은 자식 클래스들에서 수동으로 구현함

        // 쉴드 시스템 설정
        shieldSystem = new ShieldSystem();
        shieldSystem.OnShieldChanged += (shield, onShieldDepleted) =>
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, shield);
        };

    }


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
        Renderer renderer = GetComponentInChildren<Renderer>();

        // 동일한 머티리얼을 사용하는 모든 객체에 적용되는 걸 막고자 머티리얼 인스턴스를 만들고 진행한다.
        if (renderer != null)
        {
            Material materialInstance = new Material(renderer.material);
            renderer.material = materialInstance;

            SetMaterialToTransparent(materialInstance);

            // DOTween 사용하여 검정으로 변한 뒤 투명해지는 애니메이션 적용
            // materialInstance.DOColor(Color.black, 0f);
            materialInstance.DOFade(0f, 0.2f).OnComplete(() =>
            {
                OnDeathAnimationCompleted?.Invoke(this); // 사망할 것임을 알리는 이벤트
                Destroy(materialInstance); // 메모리 누수 방지
                Destroy(gameObject);
            });
        }
        else
        {
            // 렌더러가 없어도 콜백과 파괴는 실행된다.
            OnDeathAnimationCompleted?.Invoke(this);
            Destroy(gameObject);
        }
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
        }

        ObjectPoolManager.Instance!.ShowFloatingText(transform.position, actualHealAmount, true);


        if (attackSource.Attacker is Operator healerOperator)
        {
            StatisticsManager.Instance!.UpdateHealingDone(healerOperator.OperatorData, actualHealAmount);
        }
    }

    protected virtual void PlayGetHitEffect(AttackSource attackSource)
    {
        GameObject hitEffectPrefab = attackSource.HitEffectPrefab;
        string hitEffectTag = attackSource.HitEffectTag;
        string attackerName;

        if (attackSource.Attacker is Operator op)
        {
            OperatorData opData = op.OperatorData;
            attackerName = opData.entityName;
            if (hitEffectPrefab == null)
            {
                hitEffectPrefab = opData.hitEffectPrefab;
            }
        }
        else if (attackSource.Attacker is Enemy enemy)
        {
            EnemyData enemyData = enemy.BaseData;
            attackerName = enemyData.entityName;
            if (hitEffectPrefab == null)
            {
                hitEffectPrefab = enemyData.hitEffectPrefab;
            }
        }
        else
        {
            Debug.LogError("이펙트 없음");
            return;
        }

        if (hitEffectPrefab != null)
        {
            Vector3 effectPosition = transform.position;

            // 풀에서 이펙트 오브젝트 가져오기
            if (hitEffectTag == string.Empty)
            {
                hitEffectTag = attackerName + hitEffectPrefab.name;
            }
            GameObject? hitEffect = ObjectPoolManager.Instance!.SpawnFromPool(hitEffectTag, effectPosition, Quaternion.identity);
            if (hitEffect != null)
            {
                CombatVFXController hitVFXController = hitEffect.GetComponent<CombatVFXController>();
                hitVFXController.Initialize(attackSource, this, hitEffectTag);
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


    public virtual void TakeDamage(AttackSource source, bool playGetHitEffect = true)
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

        // 피격 이펙트 재생
        if (playGetHitEffect)
        {
            PlayGetHitEffect(source);
        }

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

    public virtual void OnBodyTriggerEnter(Collider other) { }
    public virtual void OnBodyTriggerExit(Collider other) {}


    protected abstract float CalculateActualDamage(AttackType attacktype, float incomingDamage);
    public void ActivateShield(float amount) => shieldSystem.ActivateShield(amount);
    public void DeactivateShield() => shieldSystem.DeactivateShield();
    public float GetCurrentShield() => shieldSystem.CurrentShield;

    public void OnDestroy()
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
