using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArcaneFieldController : MonoBehaviour, IEffectController
{
    private Operator caster;
    private Vector2Int centerPosition;
    private HashSet<Vector2Int> affectedTiles;
    private float damagePerTick;
    private float slowAmount;
    private float fieldDuration; // 필드 지속 시간
    private float damageInterval; 

    private float elapsedTime = 0f;
    private float lastDamageTime = 0f;

    private Dictionary<Enemy, SlowEffect> affectedEnemies = new Dictionary<Enemy, SlowEffect>();

    public void Initialize(Operator op, Vector2Int center, HashSet<Vector2Int> affectedTiles, float damagePerTick, float slow, float fieldDuration, float damageInterval)
    {
        caster = op;
        centerPosition = center;
        this.affectedTiles = affectedTiles;
        this.damagePerTick = damagePerTick;
        slowAmount = slow;
        this.fieldDuration = fieldDuration;
        this.damageInterval = damageInterval;

        CheckForEnemies();
    }

    private void Update()
    {
        if (elapsedTime >= fieldDuration)
        {
            StopAndDestroyEffects();
        }

        elapsedTime += Time.deltaTime;

        CheckForEnemies();

        if (Time.time >= lastDamageTime + damageInterval)
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
                enemy.TakeDamage(caster, attackSource, damagePerTick);
            }
        }

        lastDamageTime = Time.time;
    }

    private void StopAndDestroyEffects()
    {
        foreach (var pair in affectedEnemies)
        {
            if (pair.Key != null)
            {
                pair.Key.RemoveCrowdControl(pair.Value);
            }
        }
        affectedEnemies.Clear();
        Destroy(gameObject);
    }

    public void ForceRemove()
    {
        StopAndDestroyEffects();

    }
}
