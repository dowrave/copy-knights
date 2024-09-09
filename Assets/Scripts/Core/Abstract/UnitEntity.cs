using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Operator, Enemy, Barricade 등의 타일 위의 유닛들과 관련된 엔티티
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
    /// 대미지 계산 로직
    /// </summary>
    /// <param name="attacktype">공격 타입 : 물리, 마법, 트루</param>
    /// <param name="damage">들어온 대미지</param>
    /// <returns>대미지 계산 결과</returns>
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
