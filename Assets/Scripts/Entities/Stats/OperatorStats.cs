
using UnityEngine;

[System.Serializable] // 타입 자체를 "직렬화 가능하다"고 등록하는 어트리뷰트. 내부 필드는 또 별개로 설정해줘야 함
public struct OperatorStats
{
    [SerializeField] private DeployableUnitStats _deployableUnitStats;
    [SerializeField] private float _attackPower;
    [SerializeField] private float _baseAttackCooldown; // 기본 공격 쿨다운
    [SerializeField] private int _maxBlockableEnemies;
    [SerializeField] private float _spRecoveryRate;

    public OperatorStats(
        DeployableUnitStats deployableUnitStats,
        float attackPower,
        float baseAttackCooldown,
        int maxBlockableEnemies,
        float spRecoveryRate
        )
    {
        _deployableUnitStats = deployableUnitStats;
        _attackPower = attackPower;
        _baseAttackCooldown = baseAttackCooldown;
        _maxBlockableEnemies = maxBlockableEnemies;
        _spRecoveryRate = spRecoveryRate;
    }

    // 편의 생성자
    public OperatorStats(
        float health,
        float defense,
        float magicResistance,
        int deploymentCost,
        float redeployTime,
        float attackPower,
        float baseAttackCooldown,
        int maxBlockableEnemies,
        float spRecoveryRate
        )
    {
        _deployableUnitStats = new DeployableUnitStats(new UnitStats(health, defense, magicResistance), deploymentCost, redeployTime);
        _attackPower = attackPower;
        _baseAttackCooldown = baseAttackCooldown;
        _maxBlockableEnemies = maxBlockableEnemies;
        _spRecoveryRate = spRecoveryRate;
    }

    public float AttackPower => _attackPower; 
    public float BaseAttackCooldown => _baseAttackCooldown;
    public int MaxBlockableEnemies => _maxBlockableEnemies;
    public float SPRecoveryRate => _spRecoveryRate;
    public float Health => _deployableUnitStats.Health;
    public float Defense => _deployableUnitStats.Defense;
    public float MagicResistance => _deployableUnitStats.MagicResistance;
    public int DeploymentCost => _deployableUnitStats.DeploymentCost;
    public float RedeployTime => _deployableUnitStats.RedeployTime;
}