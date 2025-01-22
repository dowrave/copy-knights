using System.Collections.Generic;
using Skills.Base;
using UnityEngine;

public class AreaHasteHealController: FieldEffectController
{
    protected override void CheckTargetsInField()
    {
        // ������ ���� �޴� ���� �� ������ ��� ��� ó��
        foreach (var target in affectedTargets.Keys)
        {
            if (!IsTargetInField(target))
            {
                foreach (var effect in affectedTargets[target])
                {
                    target.RemoveCrowdControl(effect);
                }
                affectedTargets.Remove(target);
            }
        }

        // ���� ������ ���� �Ʊ� ã��
        foreach (Vector2Int tilePos in affectedTiles)
        {
            Tile tile = MapManager.Instance.GetTile(tilePos.x, tilePos.y);
            if (tile?.OccupyingDeployable != null &&
                tile.OccupyingDeployable.Faction == Faction.Ally)
            {
                DeployableUnitEntity ally = tile.OccupyingDeployable;
                if (!affectedTargets.ContainsKey(ally))
                {
                    ApplyInitialEffect(ally);
                }
            }
        }
    }

    protected override void ApplyInitialEffect(UnitEntity target)
    {
        // �Ʊ����Դ� �ʱ� ȿ�� ����
        if (!affectedTargets.ContainsKey(target))
        {
            affectedTargets[target] = new List<CrowdControl>();
        }
    }

    protected override void ApplyPeriodicEffect()
    {
        foreach (var target in affectedTargets.Keys)
        {
            if (target != null)
            {
                ICombatEntity.AttackSource healSource =
                    new ICombatEntity.AttackSource(transform.position, true, hitEffectPrefab);
                target.TakeHeal(caster, healSource, amountPerTick);
            }
        }
    }
}
