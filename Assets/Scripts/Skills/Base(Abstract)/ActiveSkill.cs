
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;
namespace Skills.Base
{
    public abstract class ActiveSkill : BaseSkill
    {
        [Header("Skill Duration")]
        public float duration = 0f;

        [Header("Skill Duration Effects")]
        [SerializeField] protected GameObject skillVFXPrefab;

        protected GameObject VfxInstance;
        protected VisualEffect VfxComponent;
        protected ParticleSystem VfxPs;

        public override void Activate(Operator op)
        {
            // 스킬 사용 가능 여부 체크
            if (!op.IsDeployed || !op.CanUseSkill()) return;

            // 기본 이펙트와 추가 이펙트 재생
            PlaySkillVFX(op);
            PlayAdditionalVFX(op);

            // 지속 시간에 따른 처리
            if (duration > 0)
            {
                op.StartCoroutine(HandleSkillDuration(op));
            }
            else  // 즉발형 스킬
            {
                OnSkillStart(op);
                PlaySkillEffect(op);
                OnSkillEnd(op);
                op.CurrentSP = 0;
            }
        }

        // 추가 이펙트
        protected virtual void PlayAdditionalVFX(Operator op) { }

        protected virtual void OnSkillStart(Operator op) 
        {
            op.SetSkillOnState(true);
        }

        // 실질적인 스킬 효과 실행.
        // 무조건 OnSkillStart와 OnSkillEnd 사이에 들어감
        protected virtual void PlaySkillEffect(Operator op) { }

        protected virtual void OnSkillEnd(Operator op)
        {
            SafeDestroySkillVFX(VfxInstance);
            op.CurrentSP = 0;
            op.SetSkillOnState(false);
        }

        // 기본 이펙트 생성 및 재생
        protected virtual void PlaySkillVFX(Operator op)
        {
            if (skillVFXPrefab == null) return;

            // 오퍼레이터 위치에 약간 띄워서 이펙트 생성
            Vector3 effectPosition = op.transform.position + Vector3.up * 0.05f;
            VfxInstance = Instantiate(
                skillVFXPrefab,
                effectPosition,
                Quaternion.identity,
                op.transform    // 오퍼레이터의 자식으로 생성
            );

            // VFX 또는 파티클 시스템 컴포넌트 검색 및 재생
            VfxComponent = VfxInstance.GetComponent<VisualEffect>();
            if (VfxComponent != null)
            {
                VfxComponent.Play();
            }

            VfxPs = VfxInstance.GetComponent<ParticleSystem>();
            if (VfxPs != null)
            {
                VfxPs.Play();
            }
        }

        // 스킬 지속시간 처리
        protected IEnumerator HandleSkillDuration(Operator op)
        {
            OnSkillStart(op);
            op.StartSkillDurationDisplay(duration);
            PlaySkillEffect(op);

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                yield return null;
                elapsedTime += Time.deltaTime;
                op.UpdateSkillDurationDisplay(1 - (elapsedTime / duration));
            }

            OnSkillEnd(op);
            op.EndSkillDurationDisplay();
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

    }
}
