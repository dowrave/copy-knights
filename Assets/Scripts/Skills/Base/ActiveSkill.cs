
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
            // ��ų ��� ���� ���� üũ
            if (!op.IsDeployed || !op.CanUseSkill()) return;

            // �⺻ ����Ʈ�� �߰� ����Ʈ ���
            PlaySkillEffect(op);
            PlayAdditionalEffects(op);

            // ���� �ð��� ���� ó��
            if (duration > 0)
            {
                op.StartCoroutine(HandleSkillDuration(op));
            }
            else  // ����� ��ų
            {
                OnSkillStart(op);
                OnSkillEnd(op);
                op.CurrentSP = 0;
            }
        }

        // �ڽ� Ŭ������ ������ ����Ʈ�� �߰��� �� �ִ� ���� �޼���
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

        // �⺻ ����Ʈ ���� �� ���
        protected virtual void PlaySkillEffect(Operator op)
        {
            if (skillEffectPrefab == null) return;

            // ���۷����� ��ġ�� �ణ ����� ����Ʈ ����
            Vector3 effectPosition = op.transform.position + Vector3.up * 0.05f;
            effectInstance = Instantiate(
                skillEffectPrefab,
                effectPosition,
                Quaternion.identity,
                op.transform    // ���۷������� �ڽ����� ����
            );

            // VFX �Ǵ� ��ƼŬ �ý��� ������Ʈ �˻� �� ���
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

        // ��ų ���ӽð� ó��
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

            // ��ƼŬ �ý����� ��� ���� ��ƼŬ�� ��� ����� ������ ���
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

            // VFX�� ��� ��� ���� �� ����
            VisualEffect vfx = effect.GetComponent<VisualEffect>();
            if (vfx != null)
            {
                vfx.Stop();
                Destroy(effect);
                return;
            }

            // �� ���� ��� ��� ����
            Destroy(effect);
        }

    }
}
