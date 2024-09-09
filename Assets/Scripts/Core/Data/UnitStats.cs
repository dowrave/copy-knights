/// UnitEntity에 사용되는 가장 기본적인 스탯
[System.Serializable]
public struct UnitStats
{
    public float health;
    public float defense;
    public float magicResistance;

    public UnitStats(float health, float defense, float magicResistance)
    {
        this.health = health;
        this.defense = defense;
        this.magicResistance = magicResistance; // 0 ~ 100 사이의 값. 
    }

}


