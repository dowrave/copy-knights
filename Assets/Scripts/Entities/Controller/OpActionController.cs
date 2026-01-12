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

    // 공격 모션 
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
    public virtual void PerformAction(UnitEntity target, float value) { }
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