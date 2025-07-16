using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 실제 ArcaneField에 들어오고 나가는 처리를 담당. VFX는 여기서 처리하지 않음.
public class ArcaneFieldController : FieldEffectController
{
    private float slowAmount;

    public virtual void Initialize(
        Operator caster,
        Vector2Int centerPosition,
        HashSet<Vector2Int> affectedTiles,
        float fieldDuration,
        float amountPerTick,
        float amountInterval,
        GameObject hitEffectPrefab,
        string hitEffectTag,
        float slowAmount
        )
    {
        base.Initialize(caster, centerPosition, affectedTiles, fieldDuration, amountPerTick, amountInterval, hitEffectPrefab, hitEffectTag);
        this.slowAmount = slowAmount;
    }

    protected override void CheckTargetsInField()
    {
        // 필드를 벗어난 적 처리
        foreach (var target in affectedTargets.Keys.ToList())
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

        // 새로 진입한 적 처리
        foreach (Vector2Int tilePos in affectedTiles)
        {
            Tile? tile = MapManager.Instance!.GetTile(tilePos.x, tilePos.y);
            if (tile != null)
            {
                foreach (Enemy enemy in tile.EnemiesOnTile)
                {
                    // 여기서 중복이 이미 방지되고 있음
                    if (!affectedTargets.ContainsKey(enemy))
                    {
                        ApplyInitialEffect(enemy);
                    }
                }
            }
        }
    }

    protected override void ApplyInitialEffect(UnitEntity target)
    {
        if (target is Enemy enemy)
        {
            var slowEffect = new SlowEffect();
            if (caster != null)
            {
                slowEffect.Initialize(enemy, caster, fieldDuration - elapsedTime, slowAmount);
                enemy.AddCrowdControl(slowEffect);
            }

            affectedTargets[enemy] = new List<CrowdControl> { slowEffect };
        }
    }

    protected override void ApplyPeriodicEffect()
    {
        foreach (var target in affectedTargets.Keys)
        {
            if (target != null && target is Enemy enemy && caster != null)
            {
                AttackSource attackSource = new AttackSource(
                    attacker: caster,
                    position: transform.position,
                    damage: amountPerTick,
                    type: caster.AttackType,
                    isProjectile: true,
                    hitEffectPrefab: hitEffectPrefab,
                    hitEffectTag: hitEffectTag
                );

                enemy.TakeDamage(attackSource);
            }
        }
    }
}
