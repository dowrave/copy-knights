using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Skills.Base
{
    // 저지당하고 있을 때 저지 중인 적을 공격하면서 저지에서 벗어나 다음 타일로 이동
    [CreateAssetMenu(fileName = "Boss SlashThrough Skill", menuName = "Skills/Boss/SlashThrough Skill")]
    public class BossSlashThroughSkill : EnemyBossSkill
    {
        [Header("Skill Configuration")]
        [SerializeField] private float damageMultiplier = 1.5f;
        [SerializeField] private float castTime = 1f;
        [SerializeField] private float moveDistance = .8f; // 오퍼레이터가 위치한 타일에서 벗어나게 하기 위한 이동거리

        [Header("VFX")]
        [SerializeField] private GameObject castVFXPrefab = default!;
        [SerializeField] private GameObject slashVFXPrefab = default!;
        [SerializeField] private GameObject hitVFXPrefab = default!; // 타격 시 적에게 나타날 이펙트 프리팹

        // VFX 자체가 실행되는 시간은 파티클 시스템에 있고
        // 오브젝트가 풀로 돌아가기까지 걸리는 시간은 기본 2초로 설정(대부분이 이를 넘기지 않을 것 같음)
        private float RETURN_POOL_WAIT_TIME = 2f;

        public override void Activate(EnemyBoss caster, UnitEntity target)
        {
            IEnumerator sequence = ActivateSequence(caster, target);
            caster.ExecuteSkillSequence(sequence);
        }

        // 스킬 실행 시의 동작.
        public IEnumerator ActivateSequence(EnemyBoss caster, UnitEntity target)
        {
            // 시전 이펙트 실행
            PlayVFX(GetCastVFXTag(caster), caster.transform.position, Quaternion.identity, RETURN_POOL_WAIT_TIME);
            caster.SetIsWaiting(true); // 시전 시간 동안 대기
            caster.SetStopAttacking(true);
            yield return new WaitForSeconds(castTime);

            // 시전 시간 후에도 타겟이 살아있다면 대미지를 주고 이동
            if (target != null && target is Operator op)
            {
                // 보스가 타겟을 통과해 지나감
                // op가 죽으면 참조되지 않으니까 이걸 앞에 구현
                SlashThrough(caster, op);


                AttackSource attackSource = new AttackSource(
                    attacker: caster,
                    position: caster.transform.position, // 크게 상관 없음
                    damage: caster.AttackPower * damageMultiplier,
                    type: AttackType.Physical,
                    isProjectile: false,
                    hitEffectPrefab: hitVFXPrefab,
                    hitEffectTag: GetHitVFXTag(caster),
                    showDamagePopup: true
                );

                op.TakeDamage(attackSource);

                // 베고 지나가는 이펙트
                // PlayVFX(GetSlashVFXTag(caster), caster.transform.position, Quaternion.identity, RETURN_POOL_WAIT_TIME);
            }

            caster.SetIsWaiting(false);
            caster.SetStopAttacking(false);
        }

        private void PlayVFX(string vfxTag, Vector3 pos, Quaternion rot, float duration = 1f)
        {
            GameObject obj = ObjectPoolManager.Instance!.SpawnFromPool(vfxTag, pos, rot);
            SelfReturnVFXController ps = obj.GetComponent<SelfReturnVFXController>();
            if (ps != null)
            {
                ps.Initialize(duration);
            }
        }

        // "뚫고 지나가는 동작"만 구현, 대미지 처리는 별도
        private void SlashThrough(EnemyBoss caster, Operator target)
        {
            // 뚫고 지나갈 방향을 계산한다.
            Vector3 moveDirection = GetSlashMoveDirection(caster, target);

            // 계산된 방향으로 위치를 이동시킨다.
            caster.transform.position = target.transform.position + moveDirection * moveDistance;

            // 지나간 후 저지 상태 수정
            caster.UpdateBlockingOperator(null);
            if (target.BlockableEnemies.Contains(caster))
            {
                target.UnblockEnemy(caster);
            }
        }

        // 보스가 오퍼레이터를 뚫고 지나갈 때의 이동 방향을 계산함
        private Vector3 GetSlashMoveDirection(EnemyBoss caster, Operator target)
        {
            // 타겟이 다음 노드에 위치한 특별한 경우
            if (target.OperatorGridPos == caster.NextNode.gridPosition)
            {
                Vector3 beforeNodeWorldPosition = caster.NextNodeWorldPosition;
                caster.UpdateNextNode();
                Vector3 afterNodeWorldPosition = caster.NextNodeWorldPosition;

                // 노드가 성공적으로 업데이트 되었다면, 이전 노드에서 새 노드로 향하는 방향을 반환
                if (afterNodeWorldPosition != beforeNodeWorldPosition)
                {
                    return (afterNodeWorldPosition - beforeNodeWorldPosition).normalized;
                }
            }

            // 일반적인 경우 (또는 마지막 노드인 경우), 현재 위치에서 다음 노드로 향하는 방향을 반환
            return (caster.NextNodeWorldPosition - caster.transform.position).normalized;
        }


        public bool CanActivate(Enemy caster)
        {
            return caster.BlockingOperator != null ? true : false;
        }


        // 단순한 연결 역할
        public override bool CanActivate(UnitEntity caster)
        {
            if (caster is Enemy enemy)
            {
                return CanActivate(enemy);
            }

            return false;
        }

                public override void InitializeSkillObjectPool(UnitEntity caster)
        {
            base.InitializeSkillObjectPool(caster);

            if (hitVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(GetHitVFXTag(caster), hitVFXPrefab, 1);
            }

            if (slashVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(GetSlashVFXTag(caster), slashVFXPrefab, 1);
            }

            if (castVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(GetCastVFXTag(caster), castVFXPrefab, 1);
            }
        }

        public float DamageMultiplier => damageMultiplier;
        public float CastTime => castTime;
        public float MoveDistance => moveDistance;

        public string GetHitVFXTag(UnitEntity caster) => $"{caster.name}_{skillName}_hit";
        public string GetSlashVFXTag(UnitEntity caster) => $"{caster.name}_{skillName}_slash";
        public string GetCastVFXTag(UnitEntity caster) => $"{caster.name}_{skillName}_cast";
    }

    
}

