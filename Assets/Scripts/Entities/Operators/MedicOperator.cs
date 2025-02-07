
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ICombatEntity;

public class MedicOperator : Operator
{
    private UnitEntity currentTargetOperator;
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

        Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);

        foreach (Vector2Int offset in BaseData.attackableTiles)
        {
            Vector2Int rotatedOffset = DirectionSystem.RotateGridOffset(offset, FacingDirection);
            Vector2Int targetGridPos = operatorGridPos + rotatedOffset;

            Tile targetTile = MapManager.Instance.CurrentMap.GetTile(targetGridPos.x, targetGridPos.y);
            if (targetTile != null)
            {
                // Ÿ�Ͽ� �ִ� CurrentDeployable & �Ʊ��� �� ������
                if (targetTile.OccupyingDeployable != null)
                {
                    DeployableUnitEntity deployable = targetTile.OccupyingDeployable;

                    if (deployable.Faction == this.Faction && // ���� �Ҽ��̰�
                        deployable.CurrentHealth < deployable.MaxHealth) // ü���� ��� ���� �� 
                    {
                        targetsInRange.Add(targetTile.OccupyingDeployable);
                    }
                }
            }
        }
    }

    public override void Attack(UnitEntity target, float damage)
    {
        Heal(target, damage);
    }

    private void Heal(UnitEntity target,  float healValue)
    {
        // ���⼭ Ranged ���ΰ� �����ϴ� �� ����ü�� ������
        // �� Melee���� ���Ÿ��� ���� �� ���� -- �����Ÿ��� ���� Ÿ���� ������ �����߱� ������ �̷� ������ �߻��ߴ�.
        if (BaseData.attackRangeType == AttackRangeType.Ranged)
        {
            base.PerformRangedAttack(target, healValue, true);
        }
        else
        {
            AttackSource attackSource = new AttackSource(transform.position, false, BaseData.HitEffectPrefab);
            target.TakeHeal(this, attackSource, healValue);
        }
    }
}


