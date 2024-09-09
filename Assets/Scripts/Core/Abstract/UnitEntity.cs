using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Operator, Enemy, Barricade ���� Ÿ�� ���� ���ֵ�� ���õ� ��ƼƼ
/// </summary>
public abstract class UnitEntity : MonoBehaviour
{
    public UnitData data;
    protected UnitStats currentStats;
    protected Tile CurrentTile { get; private set; }


    public virtual void Initialize(UnitData unitData)
    {
        data = unitData;
        InitializeStats();
        UpdateCurrentTile();
    }

    protected void InitializeStats()
    {
        currentStats = data.baseStats;
    }
    
    public virtual void TakeDamage(AttackType attacktype, float damage)
    {
        float actualDamage = CalculateActualDamage(attacktype, damage);
        currentStats.health = Mathf.Max(0, currentStats.health - actualDamage);
        if (currentStats.health <= 0)
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
        float actualDamage = 0;

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
