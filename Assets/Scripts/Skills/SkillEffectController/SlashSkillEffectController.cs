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

    // 파티클 시스템 관련
    [SerializeField] private ParticleSystem mainEffect;
    private ParticleSystem.Particle[] particles;

    // 대미지 적용 적 추적
    private HashSet<Enemy> damagedEnemies = new HashSet<Enemy>();

    // 공격 가능 타일 좌표
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
            mainEffect.Play(); // 이거 없으면 아래에서 파티클 감지 못함
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

        // 이펙트 방향 설정
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private void CalculateAttackableGridPositions()
    {
        // 오퍼레이터의 현재 그리드 위치
        Vector2Int operatorGridPos = MapManager.Instance.ConvertToGridPosition(attacker.transform.position);
        attackableGridPositions.Add(operatorGridPos); // 오퍼레이터 위치도 공격 범위에 포함

        // 오퍼레이터의 방향에 따라 기본 공격 범위(Left 기준)를 회전
        foreach (Vector2Int baseOffset in baseAttackRange)
        {
            Vector2Int rotatedOffset;
            rotatedOffset = DirectionSystem.RotateGridOffset(baseOffset, direction);

            Vector2Int targetPos = operatorGridPos + rotatedOffset;
            attackableGridPositions.Add(targetPos);
        }
    }

    // 움직임이 진행된 다음에 동작.
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

    // 타일 조건을 체크
    private bool EnemyInRangeTile(Enemy enemy)
    {
        return attackableGridPositions.Contains(enemy.CurrentTile.GridPosition);
    }

    private void OnDestroy()
    {
        damagedEnemies.Clear();
    }

}
