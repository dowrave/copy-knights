using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Operator, Enemy, Barricade ���� Ÿ�� ���� ���ֵ�� ���õ� ��ƼƼ
/// </summary>
public abstract class UnitEntity : MonoBehaviour, ITargettable, IFactionMember
{
    [SerializeField]
    private UnitData unitData;
    public UnitData Data => unitData;

    private UnitStats currentStats; // ������Ƽ�� �������� ����. 
    public Faction Faction { get; protected set; }

    public Tile CurrentTile { get; protected set; }
    public GameObject Prefab { get; protected set; }

    // ���� ����
    public float CurrentHealth
    {
        get => currentStats.Health;
        set
        {
            currentStats.Health = Mathf.Clamp(value, 0, MaxHealth); // 0 ~ �ִ� ü�� ���̷� �� ����
            OnHealthChanged?.Invoke(currentStats.Health, MaxHealth);
        }
    }
    public float MaxHealth { get; set; } // �ִ� ü�µ� ���� �� ����

    // �� ��ü�� �����ϴ� ��ƼƼ ���
    protected List<ICombatEntity> attackingEntities = new List<ICombatEntity>();

    // �̺�Ʈ
    public event System.Action<float, float> OnHealthChanged;

    public void Initialize(UnitData unitData)
    {
        this.unitData = unitData;
        currentStats = unitData.stats;

        InitializeUnitProperties();
    }

    // Data, Stat�� ��ƼƼ���� �ٸ��� ������ �ڽ� �޼��忡�� ������ ���
    protected virtual void InitializeUnitProperties()
    {
        InitializeHP();

        // ���� ��ġ�� ������� �� Ÿ�� ����
        UpdateCurrentTile();

        Prefab = Data.prefab;
    }

    public virtual void TakeDamage(AttackType attackType, float damage)
    {
        TakeDamage(attackType, damage, null);
    }
    
    /// <summary>
    /// �ǰ� ����� ���, ü�� ����
    /// </summary>
    public virtual void TakeDamage(AttackType attacktype, float damage, UnitEntity attacker = null)
    {
        float actualDamage = CalculateActualDamage(attacktype, damage);
        CurrentHealth = Mathf.Max(0, CurrentHealth - actualDamage);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

        if (CurrentHealth <= 0)
        {
            Die(); // �ڽ� �޼��忡�� �������̵��ߴٸ� �������̵��� �޼��尡 ȣ��
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
                actualDamage = incomingDamage - currentStats.Defense;
                break;
            case AttackType.Magical: 
                actualDamage = incomingDamage * (1 - currentStats.MagicResistance / 100);
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

    public virtual void AddAttackingEntity(ICombatEntity attacker)
    {
        if (!attackingEntities.Contains(attacker))
        {
            attackingEntities.Add(attacker);
        }
    }

    /// <summary>
    /// �� ��ü�� �����ϴ� ��ü ����Ʈ attackingEntites���� ��ü ����
    /// </summary>
    public virtual void RemoveAttackingEntity(ICombatEntity attacker)
    {
        attackingEntities.Remove(attacker);
    }


    protected virtual void Die()
    {
        // �������� ������ Ÿ�� ����
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
        float actualHealAmount = CurrentHealth - oldHealth; // ���� ����

        ObjectPoolManager.Instance.ShowFloatingText(transform.position, actualHealAmount, true);

        if (healer is Operator healerOperator)
        {
            StatisticsManager.Instance.UpdateHealingDone(healerOperator, actualHealAmount);
        }
    }
}
