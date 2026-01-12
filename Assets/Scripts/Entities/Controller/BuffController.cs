using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using Skills.Base;

public class BuffController: IBuffReadOnly
{
    private UnitEntity _owner;
    private List<Buff> _activeBuffs = new List<Buff>();
    public IReadOnlyList<Buff> ActiveBuffs => _activeBuffs;

    // 상태이상 제약
    public ActionRestriction Restrictions {get; private set;} = ActionRestriction.None;

    // 이벤트
    public event Action<Buff, bool> OnBuffChanged = delegate { };

    public BuffController(UnitEntity owner)
    {
        _owner = owner;
    }

    public void AddBuff(Buff buff)
    {
        // 스턴의 경우 새로 걸리면 기존 스턴은 제거됨
        if (buff is StunBuff stunBuff)
        {
            Buff existingStun = _activeBuffs.FirstOrDefault(b => b is StunBuff);
            if (existingStun != null)
            {
                RemoveBuff(existingStun);
            }
        }

        _activeBuffs.Add(buff);
        buff.OnApply(_owner, buff.caster);
        RecalculateRestrictions();
        OnBuffChanged?.Invoke(buff, true); // 이벤트 호출 
    }

    public void RemoveBuff(Buff buff)
    {
        if (_activeBuffs.Contains(buff))
        {
            buff.OnRemove(); // 만약 연결된 다른 버프들이 있다면 여기서 먼저 제거됨
            
            if (_activeBuffs.Remove(buff))
            {
                RecalculateRestrictions();
                OnBuffChanged?.Invoke(buff, false);
            }
        }
    }

    public void RemoveBuffFromSourceSkill(OperatorSkill sourceSkill)
    {
        var buffsToRemove = _activeBuffs.Where(b => b.SourceSkill == sourceSkill).ToList();
        foreach (var buff in _activeBuffs.ToList())
        {
            RemoveBuff(buff);
        }
    }

    public void UpdateBuffs()
    {
        foreach (var buff in _activeBuffs.ToArray())
        {
            buff.OnUpdate();
        }
    }

    public void RemoveAllBuffs()
    {
        foreach (var buff in _activeBuffs.ToList())
        {
            RemoveBuff(buff);
        }
    }

    // 모든 버프를 순회하며 제약을 합침(OR 연산)
    private void RecalculateRestrictions()
    {
        Restrictions = ActionRestriction.None; // 초기화

        foreach (var buff in _activeBuffs)
        {
            Restrictions |= buff.Restriction;
        }

        // 유닛의 영구적인 제약이 있다면 추가로 OR 연산
        // Restrictions |= _owner.PermanentRestrictions;
    }

    // 헬퍼 메서드
    public bool HasBuff<T>() where T : Buff => _activeBuffs.Any(b => b is T);
    public T? GetBuff<T>() where T : Buff => _activeBuffs.FirstOrDefault(b => b is T) as T;

    // Restriction 관리
    public bool HasRestriction(ActionRestriction restirction) => (Restrictions & restirction) != 0; // 겹치는 비트가 있으면 true, 없으면 false.
}