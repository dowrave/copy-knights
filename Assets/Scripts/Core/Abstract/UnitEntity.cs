using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Operator, Enemy, Barricade 등의 타일 위의 유닛들과 관련된 엔티티
/// </summary>
public abstract class UnitEntity : MonoBehaviour, ITargettable
{
    private UnitData data;
    public UnitData Data => data;
    private UnitStats currentStats;
    public Tile CurrentTile { get; protected set; }
    public GameObject Prefab { get; protected set; }

    public string Name => data.entityName;
    // 스탯 관련
    protected float currentHealth;
    public float CurrentHealth
    {
        get => currentHealth;
        protected set
        {
            currentHealth = Mathf.Clamp(value, 0, MaxHealth); // 0 ~ 최대 체력 사이로 값 유지
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }
    }
    public float MaxHealth { get; protected set; } // 최대 체력도 변할 수 있음
    public float Defense { get => currentStats.defense; protected set => currentStats.defense = value; }
    public float MagicResistance { get => currentStats.magicResistance; protected set => currentStats.magicResistance = value; }

    // 이 개체를 공격하는 엔티티 목록
    protected List<ICombatEntity> attackingEntities = new List<ICombatEntity>();

    // 이벤트
    public event System.Action<float, float> OnHealthChanged;

    public virtual void Initialize(UnitData unitData)
    {
        InitializeData(unitData); // 자식 클래스들에서 재정의하면 자식 클래스의 메서드를 사용함
        InitializeUnitProperties();
    }

    protected virtual void InitializeData(UnitData unitData)
    {
        data = unitData;
        currentStats = data.stats;
    }

    protected void InitializeUnitProperties()
    {
        InitializeMaxHealth();
        UpdateCurrentTile();

        Prefab = data.prefab; // 프리팹 설정

    }

    /// <summary>
    /// 최대체력 초기화
    /// </summary>
    protected virtual void InitializeMaxHealth()
    {
        MaxHealth = currentStats.health;
        CurrentHealth = MaxHealth;
    }
    
    /// <summary>
    /// 피격 대미지 계산, 체력 갱신
    /// </summary>
    public virtual void TakeDamage(AttackType attacktype, float damage)
    {
        float actualDamage = CalculateActualDamage(attacktype, damage);
        CurrentHealth = Mathf.Max(0, CurrentHealth - actualDamage);
        OnHealthChanged.Invoke(currentHealth, MaxHealth);

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// 대미지 계산 로직
    /// </summary>
    /// <param name="attacktype">공격 타입 : 물리, 마법, 트루</param>
    /// <param name="damage">들어온 대미지</param>
    /// <returns>대미지 계산 결과</returns>
    protected virtual float CalculateActualDamage(AttackType attacktype, float incomingDamage)
    {
        float actualDamage = 0; // 할당해야 return문에서 오류가 안남

        switch (attacktype)
        {
            case AttackType.Physical:
                actualDamage = incomingDamage - Defense;
                break;
            case AttackType.Magical: 
                actualDamage = incomingDamage * (1 - MagicResistance / 100);
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

    public virtual void RemoveAttackingEntity(ICombatEntity attacker)
    {
        attackingEntities.Remove(attacker);
    }


    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
