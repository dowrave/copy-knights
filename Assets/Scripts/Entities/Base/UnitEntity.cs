using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using static ICombatEntity;

/// <summary>
/// Operator, Enemy, Barricade 등의 타일 위의 유닛들과 관련된 엔티티
/// </summary>
public abstract class UnitEntity : MonoBehaviour, ITargettable, IFactionMember
{
    public UnitData BaseData { get; private set; }

    private UnitStats currentStats; // 프로퍼티로 구현하지 않음. 
    public Faction Faction { get; protected set; }

    public Tile CurrentTile { get; protected set; }
    public GameObject Prefab { get; protected set; }

    public ShieldSystem shieldSystem;

    // 스탯 관련
    public float CurrentHealth
    {
        get => currentStats.Health;
        set
        {
            currentStats.Health = Mathf.Clamp(value, 0, MaxHealth); // 0 ~ 최대 체력 사이로 값 유지
            OnHealthChanged?.Invoke(currentStats.Health, MaxHealth, shieldSystem.CurrentShield);
        }
    }
    public float MaxHealth { get; set; }


    // 이 개체를 공격하는 엔티티 목록
    protected List<ICombatEntity> attackingEntities = new List<ICombatEntity>();

    // 이벤트
    public event System.Action<float, float, float> OnHealthChanged;
    public event System.Action OnDestroyed;

    protected virtual void Awake()
    {
        shieldSystem = new ShieldSystem();
        shieldSystem.OnShieldChanged += (shield, onShieldDepleted) =>
        {
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, shield);
        };
    }

    public void Initialize(UnitData unitData)
    {
        this.BaseData = unitData;
        currentStats = unitData.stats;

        InitializeUnitProperties();
    }

    // Data, Stat이 엔티티마다 다르기 때문에 자식 메서드에서 재정의 고려
    protected virtual void InitializeUnitProperties()
    {
        InitializeHP();

        // 현재 위치를 기반으로 한 타일 설정
        UpdateCurrentTile();

        Prefab = BaseData.prefab;
    }

    /// <summary>
    /// 피격 대미지 계산, 체력 갱신
    /// </summary>
    public virtual void TakeDamage(UnitEntity attacker, AttackSource attackSource, float damage)
    {
        if (attacker is ICombatEntity iCombatEntity)
        {
            // 방어 / 마법 저항력이 고려된 실제 들어오는 대미지
            float actualDamage = CalculateActualDamage(iCombatEntity.AttackType, damage);

            // 쉴드를 깎고 남은 대미지
            float remainingDamage = shieldSystem.AbsorbDamage(actualDamage);

            // 체력 계산
            CurrentHealth = Mathf.Max(0, CurrentHealth - remainingDamage);
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth, shieldSystem.CurrentShield);

            PlayGetHitEffect(attacker, attackSource);

            if (CurrentHealth <= 0)
            {
                Die(); // 자식 메서드에서 오버라이드했다면 오버라이드한 메서드가 호출
            }
        }

    }

    /// <summary>
    /// 대미지 계산 로직
    /// </summary>
    protected virtual float CalculateActualDamage(AttackType attacktype, float incomingDamage)
    {
        float actualDamage = 0; // 할당해야 return문에서 오류가 안남

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

    /// <summary>
    /// 현재 위치한 타일 설정
    /// </summary>
    protected virtual void UpdateCurrentTile()
    {
        Vector3 position = transform.position;
        Tile newTile = MapManager.Instance.GetTileAtPosition(position);

        if (newTile != CurrentTile)
        {
            CurrentTile = newTile;
        }
    }

    public virtual void AddAttackingEntity(ICombatEntity attacker)
    {
        if (!attackingEntities.Contains(attacker))
        {
            attackingEntities.Add(attacker);
        }
    }

    /// <summary>
    /// 이 개체를 공격하는 개체 리스트 attackingEntites에서 개체 제거
    /// </summary>
    public virtual void RemoveAttackingEntity(ICombatEntity attacker)
    {
        attackingEntities.Remove(attacker);
    }


    protected virtual void Die()
    {
        // 공격중인 적들의 타겟 제거
        foreach (ICombatEntity entity in attackingEntities)
        {
            entity.RemoveCurrentTarget();
        }
        OnDestroyed?.Invoke();
        Destroy(gameObject);
    }

    protected virtual void InitializeHP()
    {
        MaxHealth = currentStats.Health;
        CurrentHealth = MaxHealth;
    }

    public virtual void TakeHeal(UnitEntity healer, AttackSource attackSource, float healAmount)
    {
        float oldHealth = CurrentHealth;
        CurrentHealth += healAmount; 
        float actualHealAmount = CurrentHealth - oldHealth; // 실제 힐량

        if (healer is MedicOperator medic && medic.BaseData.hitEffectPrefab != null)
        {
            PlayGetHitEffect(medic, attackSource);
        }
        
        ObjectPoolManager.Instance.ShowFloatingText(transform.position, actualHealAmount, true);


        if (healer is Operator healerOperator)
        {
            StatisticsManager.Instance.UpdateHealingDone(healerOperator, actualHealAmount);
        }
    }

    protected virtual void PlayGetHitEffect(UnitEntity attacker, AttackSource attackSource)
    {
        GameObject hitEffectPrefab;
        string entityName;

        if (attacker is Operator op)
        {
            OperatorData opData = op.BaseData;
            hitEffectPrefab = opData.hitEffectPrefab;
            entityName = opData.entityName;

        }
        else if (attacker is Enemy enemy)
        {
            EnemyData enemyData = enemy.BaseData;
            hitEffectPrefab = enemyData.hitEffectPrefab;
            entityName = enemyData.entityName;
        }
        else
        {
            Debug.LogError("이펙트를 발견하지 못함");
            return;
        }

        if (hitEffectPrefab != null)
        {
            Vector3 effectPosition = transform.position;

            // 풀에서 이펙트 오브젝트 가져오기
            string effectTag = ObjectPoolManager.Instance.EFFECT_PREFIX + entityName;
            GameObject hitEffect = ObjectPoolManager.Instance.SpawnFromPool(effectTag, effectPosition, Quaternion.identity);

            // VFX 컴포넌트 재생
            VisualEffect vfx = hitEffect.GetComponent<VisualEffect>();
            float effectLifetime = 1f;
            
            if (vfx != null)
            {
                // 방향 프로퍼티가 노출된 이펙트는 방향을 계산
                if (vfx.HasVector3("AttackDirection"))
                {
                    Vector3 attackDirection = (transform.position - attackSource.Position).normalized;
                    vfx.SetVector3("AttackDirection", attackDirection);
                }

                if (vfx.HasFloat("LifeTime"))
                {
                    int lifeTimeID = Shader.PropertyToID("Lifetime");
                    effectLifetime = vfx.GetFloat(lifeTimeID);
                }

                vfx.Play();
            }

            StartCoroutine(ReturnEffectToPool(effectTag, hitEffect, effectLifetime));
        }
    }

    private IEnumerator ReturnEffectToPool(string tag, GameObject effect, float lifeTime = 1f)
    {
        yield return new WaitForSeconds(lifeTime); // 이펙트가 나타날 시간은 줘야 함

        if (effect != null)
        {
            ObjectPoolManager.Instance.ReturnToPool(tag, effect);
        }
    }

    public void ActivateShield(float amount) => shieldSystem.ActivateShield(amount);
    public void DeactivateShield() => shieldSystem.DeactivateShield();
    public float GetCurrentShield() => shieldSystem.CurrentShield;
}
