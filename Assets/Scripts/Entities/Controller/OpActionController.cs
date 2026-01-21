using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public abstract class OpActionController: IOperatorActionReadOnly
{
    protected Operator _owner;
    public Operator Owner => _owner;

    public float ActionCooldown { get; protected set; } // 공격 쿨다운
    public float ActionDuration { get; protected set; } // 공격 모션 시간
    public UnitEntity CurrentTarget { get; protected set; }

    protected List<Vector2Int> _currentActionableGridPos;
    public IReadOnlyList<Vector2Int> CurrentActionableGridPos => _currentActionableGridPos;


    public void Initialize(Operator op)
    {
        _owner = op;
    }

    public virtual void OnDeploy()
    {
        _currentActionableGridPos = GetActionableGridPos();
    }

    // 현재 gridPos + 회전 방향을 토대로 행동 가능한 그리드 포지션을 계산
    public List<Vector2Int> GetActionableGridPos()
    {
        List<Vector2Int> newActionableGridPos = new List<Vector2Int>(); 

        // 0,0 기준 왼쪽 방향의 행동 가능 범위
        List<Vector2Int> baseOffsets = _owner.OwnedOp.BaseActionableOffsets;

        // 0,0 기준 회전된 방향의 행동 가능 범위
        List<Vector2Int> rotatedOffsets = new List<Vector2Int>(baseOffsets
            .Select(basePos => PositionCalculationSystem.RotateGridOffset(basePos, _owner.FacingDirection.Value))
            .ToList());

        // 현재 gridPos 및 facingDirection을 기준으로 한 행동 가능 범위
        foreach (Vector2Int offset in rotatedOffsets)
        {
            Vector2Int inRangeGridPosition = _owner.GridPos + offset;
            newActionableGridPos.Add(inRangeGridPosition);
        }

        return newActionableGridPos;
    }

    public void SetActionableGridPos(List<Vector2Int> newActionableGridPos)
    {
        _currentActionableGridPos.Clear();
        foreach (var newGridPos in newActionableGridPos)
        {
            _currentActionableGridPos.Add(newGridPos);
        }
    }

    public void OnUpdate()
    {
        UpdateActionTimes(); // 공격 모션 시간, 쿨다운 갱신

        if (_owner.HasRestriction(ActionRestriction.CannotAction)) return;

        // 모션 중에는 타겟 변경 & 공격 불가능
        if (ActionDuration > 0f) return; 

        SetCurrentTarget(); // CurrentTarget 선정
        ValidateCurrentTarget(); // CurrentTarget의 유효성 검사

        if (CanAction())
        {
            PerformAction(CurrentTarget, _owner.AttackPower);
        }
    }

    // 공격 모션 & 쿨다운
    public void UpdateActionTimes()
    {
        // 공격 모션 지속 시간
        if (ActionDuration > 0f)
        {
            ActionDuration -= Time.deltaTime;
        }
        
        // 쿨다운 = 다음 공격 가능 시간
        if (ActionCooldown > 0f)
        {
            ActionCooldown -= Time.deltaTime;
        }
    }

    protected virtual void SetCurrentTarget() { }
    protected virtual void ValidateCurrentTarget() { }

    // 쿨타임 돌리기 + 다른 로직을 실행해야 하는 버프 체크
    // 실제 동작은 PerformActualAction으로 실행(템플릿 메서드)
    public virtual bool PerformAction(UnitEntity target, float value, bool showPopup = false)
    {
        // Action 관련 쿨다운 설정
        SetActionDuration();
        SetActionCooldown();

        // 공격 로직을 수정하는 Buff가 있다면 해당 동작을 수행
        // 예시) 평타 중단
        var modifyBuff = _owner.ActiveBuffs.FirstOrDefault(b => b.ModifiesAttackAction);
        if (modifyBuff != null)
        {
            modifyBuff.PerformChangedAction(_owner, target);
            return false; // 여기까지만 진행한다는 의미로 false
        }

        return true; // 더 진행할 수 있으면 true
    }

    public virtual void PerformActualAction(UnitEntity target, float value, bool showPopup = false) { }

    public abstract void ResetStates();
    public abstract void OnTargetDespawn(UnitEntity target);

    protected bool CanAction()
    {
        if (_owner.HasRestriction(ActionRestriction.CannotAttack)) return false;

        return _owner.IsDeployed &&
            CurrentTarget != null &&
            ActionCooldown <= 0 &&
            ActionDuration <= 0;
    }

    public void SetActionDuration(float? intentionalCooldown = null)
    {
        if (intentionalCooldown.HasValue)
        {
            ActionDuration = intentionalCooldown.Value;
        }
        else
        {
            ActionDuration = _owner.AttackSpeed / 3f;
        }
    }

    public void SetActionCooldown(float? intentionalCooldown = null)
    {
        if (intentionalCooldown.HasValue)
        {
            ActionCooldown = intentionalCooldown.Value;
        }
        else
        {
            ActionCooldown = _owner.AttackSpeed;
        }
    }

    public virtual void OnEnemyEnteredRange(Enemy enemy) { }
    public virtual void OnEnemyExitedRange(Enemy enemy) { }
    public virtual void OnDisabled() { }
}