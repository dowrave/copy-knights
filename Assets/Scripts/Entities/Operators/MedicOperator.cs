
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ICombatEntity;

public class MedicOperator : Operator
{
    private UnitEntity currentTargetOperator;
    private List<DeployableUnitEntity> targetsInRange = new List<DeployableUnitEntity>();

    public override void Initialize(OwnedOperator ownedOp)
    {
        base.Initialize(ownedOp);
    }

    protected override void Update()
    {
        base.Update(); // ������ ValidateCurrentTarget ������ �ݿ��ȴ�.
    }

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

    /// <summary>
    /// ��Ÿ� ���� ���� ���� ������ ����
    /// </summary>
    private void GetTargetsInRange()
    {

        targetsInRange.Clear();

        Vector2Int operatorGridPos = MapManager.Instance.CurrentMap.WorldToGridPosition(transform.position);

        foreach (Vector2Int offset in BaseData.attackableTiles)
        {
            Vector2Int rotatedOffset = RotateOffset(offset, facingDirection);
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
            AttackSource attackSource = new AttackSource(transform.position, false);
            target.TakeHeal(this, attackSource, healValue);
        }
    }
}


