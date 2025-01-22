using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ���� ArcaneField�� ������ ������ ó���� ���. VFX�� ���⼭ ó������ ����.
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
        float slowAmount
        )
    {
        base.Initialize(caster, centerPosition, affectedTiles, fieldDuration, amountPerTick, amountInterval, hitEffectPrefab);
        this.slowAmount = slowAmount;
    }

    protected override void CheckTargetsInField()
    {
        // �ʵ带 ��� �� ó��
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

        // ���� ������ �� ó��
        foreach (Vector2Int tilePos in affectedTiles)
        {
            Tile tile = MapManager.Instance.GetTile(tilePos.x, tilePos.y);
            if (tile != null)
            {
                foreach (Enemy enemy in tile.GetEnemiesOnTile())
                {
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
            slowEffect.Initialize(enemy, caster, fieldDuration - elapsedTime, slowAmount);
            enemy.AddCrowdControl(slowEffect);

            affectedTargets[enemy] = new List<CrowdControl> { slowEffect };
        }
    }

    protected override void ApplyPeriodicEffect()
    {
        foreach (var target in affectedTargets.Keys)
        {
            if (target != null && target is Enemy enemy)
            {
                ICombatEntity.AttackSource attackSource =
                    new ICombatEntity.AttackSource(transform.position, true, hitEffectPrefab);
                enemy.TakeDamage(caster, attackSource, amountPerTick);
            }
        }
    }
}
