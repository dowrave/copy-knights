
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Skills.Base
{
    public abstract class ActiveSkill : OperatorSkill
    {
        [Header("Skill Duration")]
        public float duration = 0f;

        [Header("Skill Duration Effects")]
        [SerializeField] protected GameObject durationVFXPrefab = default!;

        [Header("Optional) Skill Range")]
        [SerializeField] protected bool activeFromOperatorPosition = true; // UI에 사거리 표시할 때 중심이 되는 부분의 색을 변경하기 위한 기능적인 필드
        [SerializeField] protected List<Vector2Int> skillRangeOffset = new List<Vector2Int>(); // 공격 범위

        [Header("For UI")]
        [Tooltip("UI용 수평방향 오프셋. +값은 -x방향으로 이동함.")]
        [SerializeField] protected float rectOffset; // UI용 오프셋

        public IReadOnlyList<Vector2Int> SkillRangeOffset => skillRangeOffset;
        public bool ActiveFromOperatorPosition => activeFromOperatorPosition;
        public float RectOffset => rectOffset;

        public override void Activate(Operator caster)
        {
            CommonActivation(caster);

            // 지속시간에 따른 구현
            // 이외의 상황은 메서드 오버라이드 ㄱㄱ (ammoBased는 이미 그렇게 구현됨)
            if (duration > 0)
            {
                caster.StartSkillCoroutine(Co_HandleSkillDuration(caster));
            }
            else
            {
                PlayInstantSkill(caster);
            }
        }

        // 시전자 지정, VFX 설정, 공격 초기화 등등 공통된 로직을 구현한다.
        protected virtual void CommonActivation(Operator caster)
        {
            if (!caster.IsDeployed || !caster.CanUseSkill()) return;

            // 기본 이펙트와 추가 이펙트 재생
            PlayDurationVFX(caster);
            PlayAdditionalVFX(caster);

            // 스킬 사용 직후에는 공격 속도 / 모션 초기화
            caster.SetAttackDuration(0f);
            caster.SetAttackCooldown(0f);

            caster.SetSkillOnState(true);
        }

        // 추가 이펙트
        protected virtual void PlayAdditionalVFX(Operator caster) { }

        // 지속 시간이 없는 (=즉발형) 스킬 실행
        protected void PlayInstantSkill(Operator caster)
        {
            PlaySkillEffect(caster);
            OnSkillEnd(caster);
        }

        // 실질적인 스킬 효과 실행.
        // 무조건 OnSkillStart와 OnSkillEnd 사이에 들어감
        protected virtual void PlaySkillEffect(Operator caster) { }

        protected virtual void OnSkillEnd(Operator caster)
        {
            // if (VfxInstance != null)
            // {
            //     SafeDestroySkillVFX(VfxInstance);
            // }

            caster.CurrentSP = 0;
            caster.SetSkillOnState(false);

            // 스킬이 꺼질 때도 공격 쿨다운 초기화 - 바로 때릴 수 있게끔
            caster.SetAttackDuration(0f);
            caster.SetAttackCooldown(0f);

            // 버프 해제는 여기서 구현하지 않겠음 - 자식에서 각자!

            // 지속시간이 있는 스킬은 오퍼레이터의 코루틴 초기화
            if (duration > 0)
            {
                caster.EndSkillCoroutine();
            }
        }

        // 기본 이펙트 생성 및 재생
        protected virtual void PlayDurationVFX(Operator op)
        {
            if (durationVFXPrefab == null) return;

            PlayVFX(op, GetDurationVFXTag(op), op.transform.position, Quaternion.identity, duration);
        }

        // 스킬 지속시간 처리
        public virtual IEnumerator Co_HandleSkillDuration(Operator op)
        {
            PlaySkillEffect(op);

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                if (op == null) yield break; // 오퍼레이터 파괴 시에 동작을 멈춤

                elapsedTime += Time.deltaTime;

                // 서서히 감소하는 SP 동작을 여기서 구현
                // UI 업데이트는 op.CurrentSP의 세터에 의해 자동으로 이어짐.
                op.CurrentSP = op.MaxSP * (1 - (elapsedTime / duration));

                // op.UpdateSkillDurationDisplay(1 - (elapsedTime / duration));

                yield return null;
            }

            OnSkillEnd(op);
            // op.EndSkillDurationDisplay();
        }

        protected virtual void SafeDestroySkillVFX(GameObject vfxObject)
        {
            if (vfxObject == null) return;

            // 파티클 시스템의 경우 남은 파티클이 모두 사라질 때까지 대기
            ParticleSystem ps = vfxObject.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(
                    vfxObject,
                    ps.main.duration + ps.main.startLifetime.constantMax
                );
                return;
            }

            // VFX의 경우 즉시 중지 후 제거
            VisualEffect vfx = vfxObject.GetComponent<VisualEffect>();
            if (vfx != null)
            {
                vfx.Stop();
                Destroy(vfxObject);
                return;
            }

            // 그 외의 경우 즉시 제거
            Destroy(vfxObject);
        }

        public override void InitializeSkillObjectPool(UnitEntity caster)
        {
            if (durationVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(GetDurationVFXTag(caster), durationVFXPrefab, 1);
            }
        }

        public string GetDurationVFXTag(UnitEntity caster) => $"{caster.name}_{skillName}_durationVFX";
    }
}


