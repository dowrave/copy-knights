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

    // 인터페이스가 있는 경우에만 쓸 듯
    protected List<CrowdControl> activeCC = new List<CrowdControl>();

    // 이 개체를 공격하는 엔티티 목록 : 이 개체에 변화가 생겼을 때 알리기 위해 필요함(사망, 은신 등등)
    protected List<ICombatEntity> attackingEntities = new List<ICombatEntity>();

    // 콜라이더는 자식으로 분리
    // 부모 오브젝트에서 콜라이더를 관리할 경우, 여러 개의 콜라이더 처리 시에 문제가 생긴다
    // 콜라이더는 용도에 따라 자식 오브젝트로 따로 둬야 한다.
    [SerializeField] protected BodyColliderController bodyColliderController;

    // 버프 관련
    protected List<Buff> activeBuffs = new List<Buff>();

    // ICrowdControlTarget 인터페이스 구현
    // public virtual 
    public virtual float MovementSpeed { get; }
    public virtual void SetMovementSpeed(float newMovementSpeed) {}

    // 이벤트
    public event Action<float, float, float> OnHealthChanged = delegate { };
    public event Action<CrowdControl, bool> OnCrowdControlChanged = delegate { };
    // public event Action<UnitEntity> OnDestroyed = delegate { };
    public event Action<UnitEntity> OnDeathAnimationCompleted = delegate { };

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
                // OnDestroyed?.Invoke(this); // 위의 이벤트로 통합
                Destroy(materialInstance); // 메모리 누수 방지
                RemoveAllCrowdControls();
                Destroy(gameObject);
            });
        }
        else
        {
            // 렌더러가 없어도 콜백과 파괴는 실행된다.
            OnDeathAnimationCompleted?.Invoke(this);
            // OnDestroyed?.Invoke(this);
            RemoveAllCrowdControls();
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

    public virtual void AddCrowdControl(CrowdControl newCC)
    {
        // 같은 타입의 CC 확인
        CrowdControl existingCC = activeCC.FirstOrDefault(cc => cc.GetType() == newCC.GetType());
        if (existingCC != null)
        {
            RemoveCrowdControl(existingCC);
        }

        activeCC.Add(newCC);

        // CC 추가 이벤트 -> UI 업데이트 등에 사용
        OnCrowdControlChanged?.Invoke(newCC, true);
    }

    public virtual void RemoveCrowdControl(CrowdControl cc)
    {
        if (activeCC.Remove(cc))
        {
            cc.ForceRemove();
            OnCrowdControlChanged?.Invoke(cc, false);
        }
    }

    // CC 효과 갱신
    protected virtual void UpdateCrowdControls()
    {
        for (int i = activeCC.Count - 1; i >= 0; i--)
        {
            var cc = activeCC[i];
            cc.Update();

            if (cc.IsExpired)
            {
                OnCrowdControlChanged?.Invoke(cc, false);
                activeCC.RemoveAt(i);
            }
        }
    }

    protected virtual void RemoveAllCrowdControls()
    {
        foreach (var cc in activeCC.ToList())
        {
            RemoveCrowdControl(cc);
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
        activeBuffs.Add(buff);
        buff.OnApply(this, buff.caster);
    }

    public void RemoveBuff(Buff buff)
    {
        buff.OnRemove();
        activeBuffs.Remove(buff);
    }

    // 버프 중복 적용 방지를 위한 버프 타입 헬퍼 메서드 추가
    public bool HasBuff<T>() where T : Buff
    {
        return activeBuffs.Any(b => b is T);
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

    public virtual void OnBodyTriggerEnter(Collider other) {}
    public virtual void OnBodyTriggerExit(Collider other) {}


    protected abstract float CalculateActualDamage(AttackType attacktype, float incomingDamage);
    public void ActivateShield(float amount) => shieldSystem.ActivateShield(amount);
    public void DeactivateShield() => shieldSystem.DeactivateShield();
    public float GetCurrentShield() => shieldSystem.CurrentShield;

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
