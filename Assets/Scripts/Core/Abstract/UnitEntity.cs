using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Operator, Enemy, Barricade 등의 타일 위의 유닛들과 관련된 엔티티
/// </summary>
public abstract class UnitEntity : MonoBehaviour, ITargettable, IFactionMember
{
    [SerializeField]
    private UnitData unitData;
    public UnitData Data => unitData;

    private UnitStats currentStats; // 프로퍼티로 구현하지 않음. 
    public Faction Faction { get; protected set; }

    public Tile CurrentTile { get; protected set; }
    public GameObject Prefab { get; protected set; }

    // 스탯 관련
    public float CurrentHealth
    {
        get => currentStats.Health;
        set
        {
            currentStats.Health = Mathf.Clamp(value, 0, MaxHealth); // 0 ~ 최대 체력 사이로 값 유지
            OnHealthChanged?.Invoke(currentStats.Health, MaxHealth);
        }
    }
    public float MaxHealth { get; set; } // 최대 체력도 변할 수 있음

    // 이 개체를 공격하는 엔티티 목록
    protected List<ICombatEntity> attackingEntities = new List<ICombatEntity>();

    // 이벤트
    public event System.Action<float, float> OnHealthChanged;

    public void Initialize(UnitData unitData)
    {
        this.unitData = unitData;
        currentStats = unitData.stats;

        InitializeUnitProperties();
    }

    // Data, Stat이 엔티티마다 다르기 때문에 자식 메서드에서 재정의 고려
    protected virtual void InitializeUnitProperties()
    {
        InitializeHP();

        // 현재 위치를 기반으로 한 타일 설정
        UpdateCurrentTile();

        Prefab = Data.prefab;
    }

    public virtual void TakeDamage(AttackType attackType, float damage)
    {
        TakeDamage(attackType, damage, null);
    }
    
    /// <summary>
    /// 피격 대미지 계산, 체력 갱신
    /// </summary>
    public virtual void TakeDamage(AttackType attacktype, float damage, UnitEntity attacker = null)
    {
        float actualDamage = CalculateActualDamage(attacktype, damage);
        CurrentHealth = Mathf.Max(0, CurrentHealth - actualDamage);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0)
        {
            Die(); // 자식 메서드에서 오버라이드했다면 오버라이드한 메서드가 호출
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
        Destroy(gameObject);
    }

    protected virtual void InitializeHP()
    {
        MaxHealth = currentStats.Health;
        CurrentHealth = MaxHealth;
    }

    public virtual void TakeHeal(float healAmount, UnitEntity healer = null)
    {
        float oldHealth = CurrentHealth;
        CurrentHealth += healAmount; 
        float actualHealAmount = CurrentHealth - oldHealth; // 실제 힐량

        ObjectPoolManager.Instance.ShowFloatingText(transform.position, actualHealAmount, true);

        if (healer is Operator healerOperator)
        {
            StatisticsManager.Instance.UpdateHealingDone(healerOperator, actualHealAmount);
        }
    }
}
