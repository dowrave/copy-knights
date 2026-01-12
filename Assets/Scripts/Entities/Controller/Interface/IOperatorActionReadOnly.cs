using UnityEngine;
using System.Collections.Generic;

public interface IOperatorActionReadOnly: IActionReadOnly
{
    public Operator Owner { get; }
    public IReadOnlyList<Vector2Int> CurrentActionableGridPos { get; }
}