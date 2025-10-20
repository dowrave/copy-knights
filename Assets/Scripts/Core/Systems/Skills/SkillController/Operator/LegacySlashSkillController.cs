using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LegacySlashSkillController : MonoBehaviour
{
    private Operator attacker = default!; // ���� ��쿡 ���� ���� ó���� ��� �ϴ� �̷��� ����
    private float effectDuration;
    private float effectSpeed;
    private Vector3 opDirection;
    private float damageMultiplier;
    private List<Vector2Int> baseAttackRange = new List<Vector2Int>();
    private GameObject hitEffectPrefab = default!;
    private string hitEffectTag = string.Empty;

    // ��ƼŬ �ý��� ����
    [SerializeField] private ParticleSystem mainEffect = default!;
    private ParticleSystem.Particle[] particles = System.Array.Empty<ParticleSystem.Particle>();

    // ����� ���� �� ����
    private HashSet<Enemy> damagedEnemies = new HashSet<Enemy>();

    // ���� ���� Ÿ�� ��ǥ
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
            mainEffect.Play(); // �̰� ������ ��ƼŬ �浹 ���� �ȵ�
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
            // ��ƼŬ�� ���� ��ǥ
            Vector3 particleWorldPos = transform.TransformPoint(particles[i].position);

            // ��ƼŬ �ֺ� ���� üũ
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
