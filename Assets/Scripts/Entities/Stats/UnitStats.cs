using UnityEngine;

[System.Serializable]
public struct UnitStats
{
    [SerializeField] private float _health;
    [SerializeField] private float _defense;
    [SerializeField] private float _magicResistance;

    public UnitStats(float health, float defense, float magicResistance)
    {
        _health = health;
        _defense = defense;
        _magicResistance = magicResistance; 
    }

    public float Health => _health;
    public float Defense => _defense; 
    public float MagicResistance => _magicResistance;
}