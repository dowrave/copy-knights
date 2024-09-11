using UnityEngine;

/// <summary>
/// Operator, Enemy, Barricade ���� Ÿ�� ���� ���ֵ�� ���õ� ��ƼƼ
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
            currentHealth = Mathf.Clamp(value, 0, MaxHealth); // 0 ~ �ִ� ü�� ���̷� �� ����
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }
    }
    public float MaxHealth { get; private set; } // �ִ� ü�µ� ���� �� ����

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
    /// �ִ�ü�� �ʱ�ȭ
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
    /// ����� ��� ����
    /// </summary>
    /// <param name="attacktype">���� Ÿ�� : ����, ����, Ʈ��</param>
    /// <param name="damage">���� �����</param>
    /// <returns>����� ��� ���</returns>
    protected virtual float CalculateActualDamage(AttackType attacktype, float incomingDamage)
    {
        float actualDamage = 0; // �Ҵ��ؾ� return������ ������ �ȳ�

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

        return Mathf.Max(actualDamage, 0.05f * incomingDamage); // ���� ������� 5%�� ���Բ� ����
    }

    /// <summary>
    /// ���� ��ġ�� Ÿ�� ����
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
