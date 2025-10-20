
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ICombatEntity;

public class MedicOperator : Operator
{
    private List<DeployableUnitEntity> targetsInRange = new List<DeployableUnitEntity>();

    protected override void ValidateCurrentTarget()
    {
       // 오버라이드하지만 비워둔다. SetCurrentTarget에서 그 동작을 다하는 것 같기 때문.
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


    // 사거리 내의 힐을 받을 대상들을 수집
    private void GetTargetsInRange()
    {
        targetsInRange.Clear();

        Map? CurrentMap = MapManager.Instance!.CurrentMap;

        if (CurrentMap != null)
        {
            Vector2Int operatorGridPos = CurrentMap.WorldToGridPosition(transform.position);

            foreach (Vector2Int offset in OperatorData.attackableTiles)
            {
                Vector2Int rotatedOffset = PositionCalculationSystem.RotateGridOffset(offset, FacingDirection);
                Vector2Int targetGridPos = operatorGridPos + rotatedOffset;

                Tile? targetTile = CurrentMap.GetTile(targetGridPos.x, targetGridPos.y);
                if (targetTile != null)
                {
                    DeployableUnitEntity? deployable = targetTile.OccupyingDeployable;
                    if (deployable != null && 
                        deployable.Faction == this.Faction && 
                        deployable.CurrentHealth < deployable.MaxHealth)
                    {
                        targetsInRange.Add(deployable);
                    }
                }
            }

        }
    }

    protected override void HighlightAttackRanges(List<Tile> tiles)
    {
        DeployableManager.Instance!.HighlightAttackRanges(tiles, true);
    }

    public override void Attack(UnitEntity target, float damage)
    {
        float polishedDamage = Mathf.Floor(damage);
        Heal(target, polishedDamage);

        // 공격 모션 시간 설정
        SetAttackDuration();

        // 공격 쿨다운 설정
        SetAttackCooldown();
    }

    private void Heal(UnitEntity target,  float healValue)
    {
        // 여기서 Ranged 여부가 결정하는 건 투사체의 유무임
        // 즉 Melee여도 원거리를 가질 수 있음 -- 사정거리랑 공격 타입을 별도로 구현했기 때문에 이런 현상이 발생했다.
        if (OperatorData.AttackRangeType == AttackRangeType.Ranged)
        {
            base.PerformRangedAttack(target, healValue, AttackType);
        }
        else
        {
            AttackSource attackSource = new AttackSource(
                attacker: this,
                position: transform.position,
                damage: healValue,
                type: AttackType,
                isProjectile: true,
                hitEffectTag: hitEffectTag,
                showDamagePopup: false
            );
            target.TakeHeal(attackSource);
        }
    }
}


