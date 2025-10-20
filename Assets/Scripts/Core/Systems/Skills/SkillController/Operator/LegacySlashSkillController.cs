using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LegacySlashSkillController : MonoBehaviour
{
    private Operator attacker = default!; // 죽은 경우에 대한 별도 처리가 없어서 일단 이렇게 구현
    private float effectDuration;
    private float effectSpeed;
    private Vector3 opDirection;
    private float damageMultiplier;
    private List<Vector2Int> baseAttackRange = new List<Vector2Int>();
    private GameObject hitEffectPrefab = default!;
    private string hitEffectTag = string.Empty;

    // 파티클 시스템 관련
    [SerializeField] private ParticleSystem mainEffect = default!;
    private ParticleSystem.Particle[] particles = System.Array.Empty<ParticleSystem.Particle>();

    // 대미지 적용 적 추적
    private HashSet<Enemy> damagedEnemies = new HashSet<Enemy>();

    // 공격 가능 타일 좌표
    private HashSet<Vector2Int> attackableGridPositions = new HashSet<Vector2Int>();

    private bool isInitialized = false;

    public void Initialize(Operator op, Vector3 dir, float spd, float duration, float dmgMult, List<Vector2Int> attackRange, GameObject hitEffectPrefab, string hitEffectTag)
    {
        attacker = op;
        effectDuration = duration;
        effectSpeed = spd;
        damageMultiplier = dmgMult;
        opDirection = dir;
        baseAttackRange = attackRange;
        this.hitEffectPrefab = hitEffectPrefab;
        this.hitEffectTag = hitEffectTag;

        InitializeParticleSystem();
        SetRotationByDirection();
        CalculateAttackableGridPositions();

        Destroy(gameObject, effectDuration);
    }

    private void InitializeParticleSystem()
    {
        if (mainEffect == null)
        {
            mainEffect = GetComponentInChildren<ParticleSystem>();
        }

        particles = new ParticleSystem.Particle[mainEffect.main.maxParticles];

        SetParticleDurationAndSpeed();

        if (!mainEffect.isPlaying)
        {
            mainEffect.Play(); // 이거 없으면 파티클 충돌 감지 안됨
        }

        isInitialized = true;
    }

    private void SetParticleDurationAndSpeed()
    {
        var mainModule = mainEffect.main;
        var velocityModule = mainEffect.velocityOverLifetime;

        mainModule.startLifetime = effectDuration;
        velocityModule.enabled = true;
        velocityModule.speedModifier = effectSpeed;
    }

    private void SetRotationByDirection()
    {
        float yRotation = 0f;
        if (opDirection == Vector3.forward) yRotation = 270f;
        else if (opDirection == Vector3.right) yRotation = 0f;
        else if (opDirection == Vector3.back) yRotation = 90f;
        else if (opDirection == Vector3.left) yRotation = 180f;

        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private void CalculateAttackableGridPositions()
    {
        Vector2Int operatorGridPos = MapManager.Instance!.ConvertToGridPosition(attacker.transform.position);
        attackableGridPositions.Add(operatorGridPos);

        foreach (Vector2Int baseOffset in baseAttackRange)
        {
            Vector2Int rotatedOffset = PositionCalculationSystem.RotateGridOffset(baseOffset, opDirection);
            Vector2Int targetPos = operatorGridPos + rotatedOffset;
            attackableGridPositions.Add(targetPos);
        }
    }

    private void LateUpdate()
    {
        if (!isInitialized || mainEffect == null || !mainEffect.isPlaying) return;

        int numParticlesAlive = mainEffect.GetParticles(particles);

        CheckParticleCollisions(numParticlesAlive);
    }

    private void CheckParticleCollisions(int particleCount)
    {
        for (int i = 0; i < particleCount; i++)
        {
            // 파티클의 월드 좌표
            Vector3 particleWorldPos = transform.TransformPoint(particles[i].position);

            // 파티클 주변 영역 체크
            Collider[] colliders = Physics.OverlapSphere(particleWorldPos, 0.5f);

            foreach (Collider col in colliders)
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null && !damagedEnemies.Contains(enemy) && IsEnemyInRangeTile(enemy))
                {
                    damagedEnemies.Add(enemy);

                    float damage = attacker.AttackPower * damageMultiplier;
                    AttackSource attackSource = new AttackSource(
                        attacker: attacker,
                        position: transform.position,
                        damage: damage,
                        type: attacker.AttackType,
                        isProjectile: true,
                        hitEffectTag: hitEffectTag,
                        showDamagePopup: false
                    );

                    enemy.TakeDamage(attackSource);
                }
            }
        }
    }

    private bool IsEnemyInRangeTile(Enemy enemy)
    {
        foreach (Vector2Int gridPos in attackableGridPositions)
        {
            Tile? eachTile = MapManager.Instance!.GetTile(gridPos.x, gridPos.y);
            if (eachTile != null && eachTile.EnemiesOnTile.Contains(enemy))
                return true;
        }
        return false;
    }

    private void OnDestroy()
    {
        damagedEnemies.Clear();
    }
}
