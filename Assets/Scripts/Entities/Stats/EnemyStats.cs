using UnityEngine;

[System.Serializable]
public struct EnemyStats
{
    [SerializeField] private UnitStats _baseStats;
    [SerializeField] private float _movementSpeed;
    [SerializeField] private float _attackPower;
    [SerializeField] private float _baseAttackCooldown;
    [SerializeField] private float _attackRange;
    [SerializeField] private int _blockSize; // 자신이 차지하는 저지 수

    public UnitStats BaseStats
    {
        get => _baseStats;
        set => _baseStats = value;
    }

    public float MovementSpeed => _movementSpeed;
    public float AttackPower => _attackPower;
    public float BaseAttackCooldown => _baseAttackCooldown;
    public float AttackRange => _attackRange;
    public int BlockSize => _blockSize;

    public float Health => _baseStats.Health;
    public float Defense => _baseStats.Defense;
    public float MagicResistance => _baseStats.MagicResistance;
}