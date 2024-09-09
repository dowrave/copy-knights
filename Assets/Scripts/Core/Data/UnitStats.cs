/// UnitEntity�� ���Ǵ� ���� �⺻���� ����
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
        this.magicResistance = magicResistance; // 0 ~ 100 ������ ��. 
    }

}


