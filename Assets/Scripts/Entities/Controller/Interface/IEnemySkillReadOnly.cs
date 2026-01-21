using System.Collections.Generic;
using UnityEngine;

public interface IEnemySkillReadOnly
{
    IReadOnlyList<Operator> OperatorsInSkillRange { get; }
    float CurrentGlobalCooldown { get; }
}