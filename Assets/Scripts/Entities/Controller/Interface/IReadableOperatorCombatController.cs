using System.Collections.Generic;
using UnityEngine;

public interface IReadableOperatorAttackController
{  
    float AttackCooldown { get; }
    float AttackDuration { get; }
    UnitEntity CurrentTarget { get; }
    
    public IReadOnlyList<Enemy> EnemiesInRange { get; }
    public IReadOnlyList<Vector2Int> CurrentAttackableGridPos { get; }
}