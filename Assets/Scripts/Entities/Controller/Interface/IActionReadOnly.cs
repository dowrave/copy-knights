using System.Collections.Generic;
using UnityEngine;


// Attack, Heal의 읽기 전용 인터페이스
public interface IActionReadOnly
{
    public UnitEntity CurrentTarget { get; }
    public float ActionCooldown { get; }
    public float ActionDuration { get; }
}