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
        IReadOnlyCollection<Vector2Int> affectedTiles,
        float fieldDuration,
        float amountPerTick,
        float interval,
        GameObject hitEffectPrefab,
        string hitEffectTag,
        float slowAmount
        )
    {
        base.Initialize(caster, affectedTiles, fieldDuration, amountPerTick, interval, hitEffectPrefab, hitEffectTag);
        this.slowAmount = slowAmount;

        StartCoroutine(FieldRoutine(fieldDuration, interval));
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
                    target.RemoveBuff(effect);
                }
                affectedTargets.Remove(target);
            }
        }

        // 새로 진입한 적 처리
        foreach (Vector2Int tilePos in skillRangeGridPositions)
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
        if (target is Enemy enemy && caster != null)
        {
            // 슬로우 지속 시간은 장판이 사라지거나 벗어날 때까지
            var slowBuff = new SlowBuff(float.PositiveInfinity, slowAmount);
            enemy.AddBuff(slowBuff);
  
            affectedTargets[enemy] = new List<Buff> { slowBuff };
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
                    damage: caster.AttackPower * tickDamageRatio,
                    type: caster.AttackType,
                    isProjectile: true,
                    hitEffectPrefab: hitEffectPrefab,
                    hitEffectTag: hitEffectTag,
                    showDamagePopup: false
                );

                enemy.TakeDamage(attackSource);
            }
        }
    }
}
