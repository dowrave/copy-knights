[System.Serializable]
public class UnitStats
{
    public float health;
    public float defense;
    public float magicResistance;

    // �⺻ ������
    public UnitStats(float health, float defense, float magicResistance)
    {
        this.health = health;
        this.defense = defense;
        this.magicResistance = magicResistance;
    }
}



