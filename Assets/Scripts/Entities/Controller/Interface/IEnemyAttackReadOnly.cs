using System.Collections.Generic;
using UnityEngine;

public interface IEnemyAttackReadOnly
{
    float AttackCooldown { get; }
    float AttackDuration { get; }
    
    Barricade TargetBarricade { get; }
    Operator BlockingOperator { get; }
    IReadOnlyList<UnitEntity> TargetsInRange { get; }
    bool StopAttacking { get; }
    UnitEntity? CurrentTarget { get; }
}