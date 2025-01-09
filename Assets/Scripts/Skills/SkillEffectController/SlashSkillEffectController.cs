using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class SlashSkillEffectController : MonoBehaviour
{
    private Operator attacker;
    private float lifetime;
    private Vector3 direction;
    private float damageMultiplier;
    private List<Vector2Int> baseAttackRange; 

    // ��ƼŬ �ý��� ����
    [SerializeField] private ParticleSystem mainEffect;
    private ParticleSystem.Particle[] particles;

    // ����� ���� �� ����
    private HashSet<Enemy> damagedEnemies = new HashSet<Enemy>();

    // ���� ���� Ÿ�� ��ǥ
    private HashSet<Vector2Int> attackableGridPositions = new HashSet<Vector2Int>();

    private bool isInitialized = false;

    public void Initialize(Operator op, Vector3 dir, float spd, float life, float dmgMult, List<Vector2Int> attackRange)
    {
        attacker = op;
        lifetime = life;
        damageMultiplier = dmgMult;
        direction = dir;
        baseAttackRange = attackRange;

        InitializeParticleSystem();
        SetRotationByDirection();
        CalculateAttackableGridPositions();

        Destroy(gameObject, lifetime);
    }

    private void InitializeParticleSystem()
    {
        if (mainEffect == null)
        {
            mainEffect = GetComponentInChildren<ParticleSystem>();
        }
        particles = new ParticleSystem.Particle[mainEffect.main.maxParticles];

        if (!mainEffect.isPlaying)
        {
            mainEffect.Play(); // �̰� ������ �Ʒ����� ��ƼŬ ���� ����
        }

        isInitialized = true;
    }

    private void SetRotationByDirection()
    {
        float yRotation = 0f;
        if (direction == Vector3.forward) yRotation = 270f;
        else if (direction == Vector3.right) yRotation = 0f;
        else if (direction == Vector3.back) yRotation = 90f;
        else if (direction == Vector3.left) yRotation = 180f;

        // ����Ʈ ���� ����
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private void CalculateAttackableGridPositions()
    {
        // ���۷������� ���� �׸��� ��ġ
        Vector2Int operatorGridPos = MapManager.Instance.ConvertToGridPosition(attacker.transform.position);
        attackableGridPositions.Add(operatorGridPos); // ���۷����� ��ġ�� ���� ������ ����

        // ���۷������� ���⿡ ���� �⺻ ���� ����(Left ����)�� ȸ��
        foreach (Vector2Int baseOffset in baseAttackRange)
        {
            Vector2Int rotatedOffset;
            rotatedOffset = DirectionSystem.RotateGridOffset(baseOffset, direction);

            Vector2Int targetPos = operatorGridPos + rotatedOffset;
            attackableGridPositions.Add(targetPos);
        }
    }

    // �������� ����� ������ ����.
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
            Collider[] colliders = Physics.OverlapSphere(particleWorldPos, 0.25f);

            foreach (Collider col in colliders)
            {
                Enemy enemy = col.GetComponent<Enemy>();

                if (enemy != null && !damagedEnemies.Contains(enemy) && EnemyInRangeTile(enemy))
                {
                    damagedEnemies.Add(enemy);

                    float damage = attacker.AttackPower * damageMultiplier;
                    ICombatEntity.AttackSource attackSource = new ICombatEntity.AttackSource(particleWorldPos, true);

                    enemy.TakeDamage(attacker, attackSource, damage);
                }
            }
        }
    }

    // Ÿ�� ������ üũ
    private bool EnemyInRangeTile(Enemy enemy)
    {
        return attackableGridPositions.Contains(enemy.CurrentTile.GridPosition);
    }

    private void OnDestroy()
    {
        damagedEnemies.Clear();
    }

}
