// UnitEntity의 StatController에서 관리하는 타입들
public enum StatType
{
    None, // 이 값이면 오류 발생

    // UnitStats(공통)
    MaxHP,
    Defense,
    MagicResistance,

    // DeployableUnitStats
    DeploymentCost,
    RedeployTime,

    // 공격 관련 Operator, Enemy에서 모두 사용하는 스탯들
    AttackPower,
    AttackSpeed,

    // OpereatorStats
    MaxBlockCount,
    SPRecoveryRate,

    // EnemyStats
    AttackRange, // Operator의 공격 범위는 타일 오프셋들로 관리됨
    MovementSpeed,
    BlockSize // Enemy가 차지하는 저지 수(MaxBlockCount를 초과하면 저지당하지 않고 지나감)
}