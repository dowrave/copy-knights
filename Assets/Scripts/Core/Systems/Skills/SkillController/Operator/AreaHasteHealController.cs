using System.Collections.Generic;
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
                    target.RemoveBuff(effect);
                }
                affectedTargets.Remove(target);
            }
        }

        // ���� ������ ���� �Ʊ� ã��
        foreach (Vector2Int tilePos in skillRangeGridPositions)
        {
            Tile? tile = MapManager.Instance!.GetTile(tilePos.x, tilePos.y);
            
            if (tile != null &&
                tile.OccupyingDeployable != null &&
                tile.OccupyingDeployable.Faction == Faction.Ally)
            {
                DeployableUnitEntity? ally = tile.OccupyingDeployable;
                if (ally != null && !affectedTargets.ContainsKey(ally))
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
            affectedTargets[target] = new List<Buff>();
        }
    }

    protected override void ApplyPeriodicEffect()
    {
        foreach (var target in affectedTargets.Keys)
        {
            if (target != null && caster != null)
            {
                AttackSource healSource = new AttackSource(
                    attacker: caster,
                    position: transform.position,
                    damage: caster.AttackPower * tickDamageRatio,
                    type: caster.AttackType,
                    isProjectile: true,
                    hitEffectPrefab: hitEffectPrefab,
                    hitEffectTag: hitEffectTag
                );

                target.TakeHeal(healSource);
            }
        }
    }
}
