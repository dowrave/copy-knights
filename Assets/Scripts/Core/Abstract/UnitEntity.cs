using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Operator, Enemy, Barricade ���� Ÿ�� ���� ���ֵ�� ���õ� ��ƼƼ
/// </summary>
public abstract class UnitEntity : MonoBehaviour, ITargettable
{
    private UnitData data;
    public UnitData Data => data;
    private UnitStats currentStats;
    public Tile CurrentTile { get; protected set; }
    public GameObject Prefab { get; protected set; }

    public string Name => data.entityName;
    // ���� ����
    protected float currentHealth;
    public float CurrentHealth
    {
        get => currentHealth;
        protected set
        {
            currentHealth = Mathf.Clamp(value, 0, MaxHealth); // 0 ~ �ִ� ü�� ���̷� �� ����
            OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        }
    }
    public float MaxHealth { get; protected set; } // �ִ� ü�µ� ���� �� ����
    public float Defense { get => currentStats.defense; protected set => currentStats.defense = value; }
    public float MagicResistance { get => currentStats.magicResistance; protected set => currentStats.magicResistance = value; }

    // �� ��ü�� �����ϴ� ��ƼƼ ���
    protected List<ICombatEntity> attackingEntities = new List<ICombatEntity>();

    // �̺�Ʈ
    public event System.Action<float, float> OnHealthChanged;

    public virtual void Initialize(UnitData unitData)
    {
        InitializeData(unitData); // �ڽ� Ŭ�����鿡�� �������ϸ� �ڽ� Ŭ������ �޼��带 �����
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

        Prefab = data.prefab; // ������ ����

    }

    /// <summary>
    /// �ִ�ü�� �ʱ�ȭ
    /// </summary>
    protected virtual void InitializeMaxHealth()
    {
        MaxHealth = currentStats.health;
        CurrentHealth = MaxHealth;
    }
    
    /// <summary>
    /// �ǰ� ����� ���, ü�� ����
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
                actualDamage = incomingDamage - Defense;
                break;
            case AttackType.Magical: 
                actualDamage = incomingDamage * (1 - MagicResistance / 100);
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

    public virtual void RemoveAttackingEntity(ICombatEntity attacker)
    {
        attackingEntities.Remove(attacker);
    }


    protected virtual void Die()
    {
        Destroy(gameObject);
    }
}
