using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SlashSkillController : MonoBehaviour
{
    private Operator attacker = default!; // 죽은 경우에 대한 별도 처리가 없어서 일단 이렇게 구현
    private float lifetime;
    private Vector3 opDirection;
    private float damageMultiplier;
    private List<Vector2Int> baseAttackRange = new List<Vector2Int>();
    private GameObject hitEffectPrefab = default!; 

    // 파티클 시스템 관련
    [SerializeField] private ParticleSystem mainEffect = default!;
    private ParticleSystem.Particle[] particles = System.Array.Empty<ParticleSystem.Particle>();

    // 대미지 적용 적 추적
    private HashSet<Enemy> damagedEnemies = new HashSet<Enemy>();

    // 공격 가능 타일 좌표
    private HashSet<Vector2Int> attackableGridPositions = new HashSet<Vector2Int>();

    private bool isInitialized = false;

    public void Initialize(Operator op, Vector3 dir, float spd, float life, float dmgMult, List<Vector2Int> attackRange, GameObject hitEffectPrefab)
    {
        attacker = op;
        lifetime = life;
        damageMultiplier = dmgMult;
        opDirection = dir;
        baseAttackRange = attackRange;
        this.hitEffectPrefab = hitEffectPrefab;

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

    // 디폴트 방향이 오른쪽 - 오퍼레이터의 방향에 맞춰서 스킬이 나가야 함
    private void SetRotationByDirection()
    {
        float yRotation = 0f;
        if (opDirection == Vector3.forward) yRotation = 270f;
        else if (opDirection == Vector3.right) yRotation = 0f;
        else if (opDirection == Vector3.back) yRotation = 90f;
        else if (opDirection == Vector3.left) yRotation = 180f;

        // 이펙트 방향 설정
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private void CalculateAttackableGridPositions()
    {
        // 오퍼레이터의 현재 그리드 위치
        Vector2Int operatorGridPos = MapManager.Instance!.ConvertToGridPosition(attacker.transform.position);
        attackableGridPositions.Add(operatorGridPos); // 오퍼레이터 위치도 공격 범위에 포함

        // 오퍼레이터의 방향에 따라 기본 공격 범위(Left 기준)를 회전
        foreach (Vector2Int baseOffset in baseAttackRange)
        {
            Vector2Int rotatedOffset;
            rotatedOffset = DirectionSystem.RotateGridOffset(baseOffset, opDirection);

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

                if (enemy != null && !damagedEnemies.Contains(enemy) && IsEnemyInRangeTile(enemy))
                {
                    damagedEnemies.Add(enemy);

                    float damage = attacker.AttackPower * damageMultiplier;
                    ICombatEntity.AttackSource attackSource = new ICombatEntity.AttackSource(particleWorldPos, true, hitEffectPrefab);

                    enemy.TakeDamage(attacker, attackSource, damage);
                }
            }
        }
    }

    // 타일 조건을 체크
    private bool IsEnemyInRangeTile(Enemy enemy)
    {
        foreach (Vector2Int gridPos in attackableGridPositions)
        {
            Tile? eachTile = MapManager.Instance!.GetTile(gridPos.x, gridPos.y);
            if (eachTile != null && 
                eachTile.EnemiesOnTile.Contains(enemy)) return true;
        }
        return false;
    }

    private void OnDestroy()
    {
        damagedEnemies.Clear();
    }

}
