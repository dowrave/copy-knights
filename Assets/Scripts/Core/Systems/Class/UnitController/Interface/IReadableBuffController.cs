using System;
using System.Collections.Generic;

// UnitEntity 외부에서 접근할 수 있는 프로퍼티 / 메서드 정의
public interface IReadableBuffController
{
    IReadOnlyList<Buff> ActiveBuffs { get; }
    ActionRestriction Restrictions { get; } 

    bool HasBuff<T>() where T : Buff;
    T GetBuff<T>() where T : Buff;

    event Action<Buff, bool> OnBuffChanged;
}