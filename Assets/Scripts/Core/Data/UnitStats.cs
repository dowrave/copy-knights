[System.Serializable]
public class UnitStats
{
    public float health;
    public float defense;
    public float magicResistance;

    // 기본 생성자
    public UnitStats(float health, float defense, float magicResistance)
    {
        this.health = health;
        this.defense = defense;
        this.magicResistance = magicResistance;
    }
}



