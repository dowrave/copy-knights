
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
namespace Skills.Base
{
    public abstract class ActiveSkill : BaseSkill
    {
        [Header("Skill Duration")]
        public float duration = 0f;

        [Header("Skill Effects")]
        [SerializeField] protected GameObject skillEffectPrefab;

        protected GameObject effectInstance;
        protected VisualEffect vfxComponent;
        protected ParticleSystem particleSystem;

        public override void Activate(Operator op)
        {
            // 스킬 사용 가능 여부 체크
            if (!op.IsDeployed || !op.CanUseSkill()) return;

            // 기본 이펙트와 추가 이펙트 재생
            PlaySkillEffect(op);
            PlayAdditionalEffects(op);

            // 지속 시간에 따른 처리
            if (duration > 0)
            {
                op.StartCoroutine(HandleSkillDuration(op));
            }
            else  // 즉발형 스킬
            {
                OnSkillStart(op);
                OnSkillEnd(op);
                op.CurrentSP = 0;
            }
        }

        // 자식 클래스가 고유한 이펙트를 추가할 수 있는 가상 메서드
        protected virtual void PlayAdditionalEffects(Operator op) { }

        protected virtual void OnSkillStart(Operator op) 
        {
            op.SetSkillOnState(true);
        }
        protected virtual void OnSkillEnd(Operator op)
        {
            SafeDestroySkillEffect(effectInstance);
            op.CurrentSP = 0;
            op.SetSkillOnState(false);
        }

        // 기본 이펙트 생성 및 재생
        protected virtual void PlaySkillEffect(Operator op)
        {
            if (skillEffectPrefab == null) return;

            // 오퍼레이터 위치에 약간 띄워서 이펙트 생성
            Vector3 effectPosition = op.transform.position + Vector3.up * 0.05f;
            effectInstance = Instantiate(
                skillEffectPrefab,
                effectPosition,
                Quaternion.identity,
                op.transform    // 오퍼레이터의 자식으로 생성
            );

            // VFX 또는 파티클 시스템 컴포넌트 검색 및 재생
            vfxComponent = effectInstance.GetComponent<VisualEffect>();
            if (vfxComponent != null)
            {
                vfxComponent.Play();
            }

            particleSystem = effectInstance.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                particleSystem.Play();
            }
        }

        // 스킬 지속시간 처리
        protected IEnumerator HandleSkillDuration(Operator op)
        {
            OnSkillStart(op);
            op.StartSkillDurationDisplay(duration);

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

        protected virtual void SafeDestroySkillEffect(GameObject effect)
        {
            if (effect == null) return;

            // 파티클 시스템의 경우 남은 파티클이 모두 사라질 때까지 대기
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(
                    effect,
                    ps.main.duration + ps.main.startLifetime.constantMax
                );
                return;
            }

            // VFX의 경우 즉시 중지 후 제거
            VisualEffect vfx = effect.GetComponent<VisualEffect>();
            if (vfx != null)
            {
                vfx.Stop();
                Destroy(effect);
                return;
            }

            // 그 외의 경우 즉시 제거
            Destroy(effect);
        }

    }
}
