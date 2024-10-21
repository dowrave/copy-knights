
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
        base.Update(); // ������ ValidateCurrentTarget ������ �ݿ��ȴ�.
    }


    protected override void ValidateCurrentTarget()
    {
       // �������̵������� ����д�. ���⼭�� ������ ���� ����. SetCurrentTarget���� �� ������ ���ϴ� �� ���� ����.
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

        foreach (Vector2Int offset in Data.attackableTiles)
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

    public override void Attack(UnitEntity target, AttackType attackType, float damage)
    {
        Heal(target, attackType, damage);
    }

    private void Heal(UnitEntity target, AttackType attackType, float healValue)
    {
        // ���⼭ Ranged ���ΰ� �����ϴ� �� ����ü�� ������
        // �� Melee���� ���Ÿ��� ���� �� ���� -- �����Ÿ��� ���� Ÿ���� ������ �����߱� ������ �̷� ������ �߻��ߴ�.
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


