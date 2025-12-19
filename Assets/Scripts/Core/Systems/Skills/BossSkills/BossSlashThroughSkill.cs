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
        [SerializeField] private float moveDistance = .8f; // 오퍼레이터가 위치한 타일에서 벗어나게 하기 위한 이동

        [Header("VFX")]
        [SerializeField] private GameObject castVFXPrefab = default!;
        [SerializeField] private GameObject slashVFXPrefab = default!;
        [SerializeField] private GameObject hitVFXPrefab = default!; // 타격 시 적에게 나타날 이펙트 프리팹
        [SerializeField] private GameObject crossVFXPrefab = default!; // 뚫기 시작 -> 도착 후에 시전자에게 잠깐 나타나는 이펙트

        private string _crossVFXTag;
        private string _hitVFXTag;
        private string _castVFXTag;
        private string _slashVFXTag;

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

            float crossVFXStartTime = 0.8f;

            // 시전 이펙트 실행
            PlayVFX(caster, CastVFXTag, caster.transform.position, Quaternion.identity, RETURN_POOL_WAIT_TIME);
            caster.SetIsWaiting(true); // 시전 시간 동안 대기
            caster.SetStopAttacking(true);
            yield return new WaitForSeconds(crossVFXStartTime);

            // 시전 
            GameObject crossVFXObj = PlayVFX(caster, CrossVFXTag, caster.transform.position, Quaternion.identity, RETURN_POOL_WAIT_TIME);
            crossVFXObj.transform.SetParent(caster.transform);
            yield return new WaitForSeconds(castTime - crossVFXStartTime);

            // 시전 시간 후에도 타겟이 살아있다면 대미지를 주고 이동
            if (target != null && target is Operator op)
            {
                // 보스가 타겟을 통과해 지나감
                // op가 죽으면 참조되지 않으니까 이걸 앞에 구현

                // 뚫고 지나갈 방향을 계산한다.
                Vector3 moveDirection = GetSlashMoveDirection(caster, op);

                // 캐스팅 중에 때리는 위치를 저장함
                Vector3 beforePosition = caster.transform.position;

                // 뚫고 지나가는 효과 실행
                SlashThrough(caster, op, moveDirection);

                AttackSource attackSource = new AttackSource(
                    attacker: caster,
                    position: beforePosition, // Hit 이펙트의 파티클 튀는 방향 때문에 방향 설정이 중요함(지나가면서 때린다는 설정이므로 이전 위치로 설정)
                    damage: caster.AttackPower * damageMultiplier,
                    type: AttackType.Physical,
                    isProjectile: false,
                    hitEffectTag: HitVFXTag,
                    showDamagePopup: true
                );

                op.TakeDamage(attackSource);

                // 베고 지나가는 이펙트
                Quaternion rot = Quaternion.LookRotation(moveDirection);
                PlayVFX(caster, SlashVFXTag, target.transform.position, rot, RETURN_POOL_WAIT_TIME);
            }

            caster.SetIsWaiting(false);
            caster.SetStopAttacking(false);
        }

        // "뚫고 지나가는 동작"만 구현, 대미지 처리는 별도
        private void SlashThrough(EnemyBoss caster, Operator target, Vector3 moveDirection)
        {
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

        public override void PreloadObjectPools(EnemyBossData ownerData)
        {
            base.PreloadObjectPools(ownerData);

            if (hitVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(HitVFXTag, hitVFXPrefab, 1);
            }

            if (slashVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(SlashVFXTag, slashVFXPrefab, 1);
            }

            if (castVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(CastVFXTag, castVFXPrefab, 1);
            }

            if (crossVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(CrossVFXTag, crossVFXPrefab, 1);
            }
        }


        public float DamageMultiplier => damageMultiplier;
        public float CastTime => castTime;
        public float MoveDistance => moveDistance;

        // public string HitVFXTag => _hitVFXTag ??= $"{skillName}_hit";
        // public string SlashVFXTag => _slashVFXTag ??= $"{skillName}_slash";
        // public string CastVFXTag => _castVFXTag ??= $"{skillName}_cast";
        // public string CrossVFXTag => _crossVFXTag ??= $"{skillName}_cross";

        public string HitVFXTag
        {
            get
            {
                if (string.IsNullOrEmpty(_hitVFXTag))
                {
                    _hitVFXTag = $"{skillName}_HitVFX";
                }
                return _hitVFXTag;
            }
        }
        public string SlashVFXTag
        {
            get
            {
                if (string.IsNullOrEmpty(_slashVFXTag))
                {
                    _slashVFXTag = $"{skillName}_SlashVFX";
                }
                return _slashVFXTag;
            }
        }

        public string CastVFXTag
        {
            get
            {
                if (string.IsNullOrEmpty(_castVFXTag))
                {
                    _castVFXTag = $"{skillName}_CastVFX";
                }
                return _castVFXTag;
            }
        }

        public string CrossVFXTag
        {
            get
            {
                if (string.IsNullOrEmpty(_crossVFXTag))
                {
                    _crossVFXTag = $"{skillName}_CrossVFX";
                }
                return _crossVFXTag;
            }
        }



    }

    
}

