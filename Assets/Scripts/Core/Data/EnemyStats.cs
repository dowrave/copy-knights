using UnityEngine;

[System.Serializable]
public class EnemyStats : UnitStats
{
    public float movementSpeed = 1f;
    public float attackPower = 100f;
    public float attackSpeed = 1f;
    public float attackRange = 0f; // 원거리만 값 설정

    public EnemyStats(float health, float defense, float magicResistance, float movementSpeed, float attackSpeed, float attackPower, float attackRange)
        :base(health, defense, magicResistance)
    {
        this.movementSpeed = movementSpeed;
        this.attackRange = attackRange;
        this.attackSpeed = attackSpeed;
        this.attackPower = attackPower;
    }
}
