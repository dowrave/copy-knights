using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ArcaneFieldController : MonoBehaviour
{
    private Operator caster;
    private Vector2Int centerPosition;
    private List<Vector2Int> affectedPattern;
    private float damagePerTick;
    private float slowAmount;
    private float fieldDuration; // 필드 지속 시간
    private float damageInterval; 

    private float elapsedTime = 0f;
    private float lastDamageTime = 0f;

    private HashSet<Vector2Int> affectedTiles = new HashSet<Vector2Int>();
    private Dictionary<Enemy, SlowEffect> affectedEnemies = new Dictionary<Enemy, SlowEffect>();

    public void Initialize(Operator op, Vector2Int center, List<Vector2Int> pattern, float damagePerTick, float slow, float fieldDuration, float damageInterval)
    {
        caster = op;
        centerPosition = center;
        affectedPattern = pattern;
        this.damagePerTick = damagePerTick;
        slowAmount = slow;
        this.fieldDuration = fieldDuration;
        this.damageInterval = damageInterval;

        CalculateAffectedTiles();
        CreateFieldEffects();
        CheckForEnemies();
    }

    private void CalculateAffectedTiles()
    {
        foreach (Vector2Int offset in affectedPattern)
        {
            Vector2Int tilePos = centerPosition + offset;
            if (MapManager.Instance.CurrentMap.IsValidGridPosition(tilePos.x, tilePos.y))
            {
                affectedTiles.Add(tilePos);
            }
        }
    }

    private void Update()
    {
        if (elapsedTime >= fieldDuration)
        {
            Destroy(gameObject);
            return;
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
                Debug.Log($"damagePerTick : {damagePerTick}");
                enemy.TakeDamage(caster, attackSource, damagePerTick);
            }
        }

        lastDamageTime = Time.time;
    }

    private void CreateFieldEffects()
    {
        // 장판 시각 효과 생성 로직
    }

    private void OnDestroy()
    {
        foreach (var pair in affectedEnemies)
        {
            if (pair.Key != null)
            {
                pair.Key.RemoveCrowdControl(pair.Value);
            }
        }
        affectedEnemies.Clear();
    }
}
