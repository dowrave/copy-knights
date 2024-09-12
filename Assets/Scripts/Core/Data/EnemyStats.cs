using UnityEngine;

[System.Serializable]
public struct EnemyStats
{
    public UnitStats baseStats;
    public float movementSpeed;
    public float attackPower;
    public float attackSpeed;
    public float attackRange;

    // UnitStats의 프로퍼티들
    public float Health
    {
        get => baseStats.health;
        set => baseStats.health = value;
    }

    public float Defense
    {
        get => baseStats.defense;
        set => baseStats.defense = value;
    }

    public float MagicResistance
    {
        get => baseStats.magicResistance;
        set => baseStats.magicResistance = value;
    }
}