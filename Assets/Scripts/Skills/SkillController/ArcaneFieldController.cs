using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 실제 ArcaneField에 들어오고 나가는 처리를 담당. VFX는 여기서 처리하지 않음.
public class ArcaneFieldController : FieldEffectController
{
    private float slowAmount;

    private float lastDamageTime = 0f;

    private Dictionary<Enemy, SlowEffect> affectedEnemies = new Dictionary<Enemy, SlowEffect>();

    public virtual void Initialize(
        Operator caster,
        Vector2Int centerPosition,
        HashSet<Vector2Int> affectedTiles,
        float fieldDuration,
        float amountPerTick,
        float amountInterval,
        float slowAmount
        )
    {
        base.Initialize(caster, centerPosition, affectedTiles, fieldDuration, amountPerTick, amountInterval);

        this.slowAmount = slowAmount;
    }

    protected override void Update()
    {
        base.Update();

        CheckForEnemies();

        if (Time.time >= lastDamageTime + interval)
        {
            ApplyDamageToEnemies();
        }
    }

    private void CheckForEnemies()
    {
        // 장판을 벗어난 적 효과 해제 및 제거
        foreach (Enemy enemy in affectedEnemies.Keys.ToList())
        {
            if (enemy == null || !IsEnemyInField(enemy))
            {
                if (affectedEnemies.TryGetValue(enemy, out SlowEffect effect))
                {
                    enemy.RemoveCrowdControl(effect);
                    affectedEnemies.Remove(enemy);
                }
            }
        }

        // 새로 장판에 진입한 적 추가
        foreach (Vector2Int tilePos in affectedTiles)
        {
            Tile tile = MapManager.Instance.GetTile(tilePos.x, tilePos.y);
            if (tile != null)
            {
                foreach (Enemy enemy in tile.GetEnemiesOnTile())
                {
                    if (!affectedEnemies.ContainsKey(enemy))
                    {
                        ApplyEffectsToEnemy(enemy);
                    }
                }
            }
        }
    }

    private bool IsEnemyInField(Enemy enemy)
    {
        if (enemy == null) return false;

        Vector2Int enemyPos = MapManager.Instance.ConvertToGridPosition(enemy.transform.position);
        return affectedTiles.Contains(enemyPos);
    }

    private void ApplyEffectsToEnemy(Enemy enemy)
    {
        var slowEffect = new SlowEffect();
        slowEffect.Initialize(enemy, caster, fieldDuration - elapsedTime, slowAmount);
        enemy.AddCrowdControl(slowEffect);

        affectedEnemies[enemy] = slowEffect;
    }

    private void ApplyDamageToEnemies()
    {
        foreach (Enemy enemy in affectedEnemies.Keys)
        {
            if (enemy != null)
            {
                ICombatEntity.AttackSource attackSource = new ICombatEntity.AttackSource(transform.position, true);
                enemy.TakeDamage(caster, attackSource, amountPerTick);
            }
        }

        lastDamageTime = Time.time;
    }

    protected override void StopAndDestroyEffects()
    {
        foreach (var pair in affectedEnemies)
        {
            if (pair.Key != null)
            {
                pair.Key.RemoveCrowdControl(pair.Value);
            }
        }
        affectedEnemies.Clear();
        base.StopAndDestroyEffects();
    }
}
