using System.Collections.Generic;
using Skills.Base;
using UnityEngine;

public class AreaHasteHealController: FieldEffectController
{
    protected override void CheckTargetsInField()
    {
        // 이전에 힐을 받던 대상들 중 범위를 벗어난 경우 처리
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

        // 새로 범위에 들어온 아군 찾기
        foreach (Vector2Int tilePos in affectedTiles)
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
        // 아군에게는 초기 효과 없음
        if (!affectedTargets.ContainsKey(target))
        {
            affectedTargets[target] = new List<CrowdControl>();
        }
    }

    protected override void ApplyPeriodicEffect()
    {
        foreach (var target in affectedTargets.Keys)
        {
            if (target != null && caster != null)
            {
                ICombatEntity.AttackSource healSource =
                    new ICombatEntity.AttackSource(transform.position, true, hitEffectPrefab);
                target.TakeHeal(caster, healSource, amountPerTick);
            }
        }
    }
}
