using UnityEngine;

/// <summary>
/// Operator, Enemy, Barricade 등의 타일 위의 유닛들과 관련된 엔티티
/// </summary>
public abstract class UnitEntity : MonoBehaviour
{
    private UnitData data;
    private UnitStats currentStats;
    protected Tile CurrentTile { get; private set; }

    private float currentHealth;
    public float CurrentHealth
    {
        get => currentHealth;
        protected set
        {
            currentHealth = Mathf.Clamp(value, 0, MaxHealth); // 0 ~ 최대 체력 사이로 값 유지
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }
    }
    public float MaxHealth { get; private set; } // 최대 체력도 변할 수 있음

    public event System.Action<float, float> OnHealthChanged;


    public virtual void Initialize(UnitData unitData)
    {
        InitializeData(unitData); 
        InitializeMaxHealth();
        UpdateCurrentTile();
    }

    protected virtual void InitializeData(UnitData unitData)
    {
        data = unitData;
        currentStats = data.stats;
    }

    /// <summary>
    /// 최대체력 초기화
    /// </summary>
    protected virtual void InitializeMaxHealth()
    {
        MaxHealth = currentStats.health;
        CurrentHealth = MaxHealth;
    }
    
    public virtual void TakeDamage(AttackType attacktype, float damage)
    {
        float actualDamage = CalculateActualDamage(attacktype, damage);
        CurrentHealth = Mathf.Max(0, CurrentHealth - actualDamage);
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
                actualDamage = incomingDamage - currentStats.defense;
                break;
            case AttackType.Magical: 
                actualDamage = incomingDamage * (1 - currentStats.magicResistance / 100);
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

    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
