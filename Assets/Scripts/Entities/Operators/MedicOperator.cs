
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ICombatEntity;

public class MedicOperator : Operator
{
    private List<DeployableUnitEntity> targetsInRange = new List<DeployableUnitEntity>();

    protected override void ValidateCurrentTarget()
    {
       // �������̵������� ����д�. SetCurrentTarget���� �� ������ ���ϴ� �� ���� ����.
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


    // ��Ÿ� ���� ���� ���� ������ ����
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

        // ���� ��� �ð� ����
        SetAttackDuration();

        // ���� ��ٿ� ����
        SetAttackCooldown();
    }

    private void Heal(UnitEntity target,  float healValue)
    {
        // ���⼭ Ranged ���ΰ� �����ϴ� �� ����ü�� ������
        // �� Melee���� ���Ÿ��� ���� �� ���� -- �����Ÿ��� ���� Ÿ���� ������ �����߱� ������ �̷� ������ �߻��ߴ�.
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


