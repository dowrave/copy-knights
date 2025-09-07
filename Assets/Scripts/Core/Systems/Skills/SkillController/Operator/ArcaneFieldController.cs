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
        // �ʵ带 ��� �� ó��
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

        // ���� ������ �� ó��
        foreach (Vector2Int tilePos in skillRangeGridPositions)
        {
            Tile? tile = MapManager.Instance!.GetTile(tilePos.x, tilePos.y);
            if (tile != null)
            {
                foreach (Enemy enemy in tile.EnemiesOnTile)
                {
                    // ���⼭ �ߺ��� �̹� �����ǰ� ����
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
            // ���ο� ���� �ð��� ������ ������ų� ��� ������
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
