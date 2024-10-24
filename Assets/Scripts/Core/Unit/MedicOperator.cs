
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MedicOperator : Operator
{
    private UnitEntity currentTargetOperator;
    private List<DeployableUnitEntity> targetsInRange = new List<DeployableUnitEntity>();

    public override void Initialize(OperatorData operatorData)
    {
        base.Initialize(operatorData);
    }

    protected override void Update()
    {
        base.Update(); // 수정된 ValidateCurrentTarget 내용이 반영된다.
    }


    protected override void ValidateCurrentTarget()
    {
       // 오버라이드하지만 비워둔다. 여기서는 별도로 넣지 않음. SetCurrentTarget에서 그 동작을 다하는 것 같기 때문.
    }

    public override void SetCurrentTarget()
    {
        GetTargetsInRange();

        if (targetsInRange.Count > 0)
        {
            CurrentTarget = targetsInRange
                .OrderBy(target => target.CurrentHealth)
                .FirstOrDefault();
        }
        else
        {
            CurrentTarget = null; 
        }
    }

    /// <summary>
    /// 사거리 내의 힐을 받을 대상들을 수집
    /// </summary>
    private void GetTargetsInRange()
    {

        targetsInRange.Clear();

        Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);

        foreach (Vector2Int offset in Data.attackableTiles)
        {
            Vector2Int rotatedOffset = RotateOffset(offset, facingDirection);
            Vector2Int targetGridPos = operatorGridPos + rotatedOffset;

            Tile targetTile = MapManager.Instance.CurrentMap.GetTile(targetGridPos.x, targetGridPos.y);
            if (targetTile != null)
            {
                // 타일에 있는 CurrentDeployable & 아군일 때 가져옴
                if (targetTile.OccupyingDeployable != null)
                {
                    DeployableUnitEntity deployable = targetTile.OccupyingDeployable;

                    if (deployable.Faction == this.Faction && // 같은 소속이고
                        deployable.CurrentHealth < deployable.MaxHealth) // 체력이 닳아 있을 때 
                    {
                        targetsInRange.Add(targetTile.OccupyingDeployable);
                    }
                }
            }
        }
    }

    public override void Attack(UnitEntity target, AttackType attackType, float damage)
    {
        Heal(target, attackType, damage);
    }

    private void Heal(UnitEntity target, AttackType attackType, float healValue)
    {
        // 여기서 Ranged 여부가 결정하는 건 투사체의 유무임
        // 즉 Melee여도 원거리를 가질 수 있음 -- 사정거리랑 공격 타입을 별도로 구현했기 때문에 이런 현상이 발생했다.
        if (Data.attackRangeType == AttackRangeType.Ranged)
        {
            base.PerformRangedAttack(target, attackType, healValue, true);
        }
        else
        {
            target.TakeHeal(healValue, this);
        }
    }
}


