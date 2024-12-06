using UnityEngine;

[CreateAssetMenu(fileName = "New Unit Data", menuName = "Game/Unit Data")]
public class UnitData : ScriptableObject
{
    public string entityName;
    public UnitStats stats;
    public GameObject prefab;
}

[System.Serializable]
public struct UnitStats
{
    [SerializeField] private float _health;
    [SerializeField] private float _defense;
    [SerializeField] private float _magicResistance;

    public float Health
    {
        get => _health;
        set => _health = value;
    }

    public float Defense
    {
        get => _defense;
        set => _defense = value;
    }

    public float MagicResistance
    {
        get => _magicResistance;
        set => _magicResistance = value;
    }
}