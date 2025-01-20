
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
            // ��ų ��� ���� ���� üũ
            if (!op.IsDeployed || !op.CanUseSkill()) return;

            // �⺻ ����Ʈ�� �߰� ����Ʈ ���
            PlaySkillVFX(op);
            PlayAdditionalVFX(op);

            // ���� �ð��� ���� ó��
            if (duration > 0)
            {
                op.StartCoroutine(HandleSkillDuration(op));
            }
            else  // ����� ��ų
            {
                OnSkillStart(op);
                PlaySkillEffect(op);
                OnSkillEnd(op);
                op.CurrentSP = 0;
            }
        }

        // �߰� ����Ʈ
        protected virtual void PlayAdditionalVFX(Operator op) { }

        protected virtual void OnSkillStart(Operator op) 
        {
            op.SetSkillOnState(true);
        }

        // �������� ��ų ȿ�� ����.
        // ������ OnSkillStart�� OnSkillEnd ���̿� ��
        protected virtual void PlaySkillEffect(Operator op) { }

        protected virtual void OnSkillEnd(Operator op)
        {
            SafeDestroySkillVFX(VfxInstance);
            op.CurrentSP = 0;
            op.SetSkillOnState(false);
        }

        // �⺻ ����Ʈ ���� �� ���
        protected virtual void PlaySkillVFX(Operator op)
        {
            if (skillVFXPrefab == null) return;

            // ���۷����� ��ġ�� �ణ ����� ����Ʈ ����
            Vector3 effectPosition = op.transform.position + Vector3.up * 0.05f;
            VfxInstance = Instantiate(
                skillVFXPrefab,
                effectPosition,
                Quaternion.identity,
                op.transform    // ���۷������� �ڽ����� ����
            );

            // VFX �Ǵ� ��ƼŬ �ý��� ������Ʈ �˻� �� ���
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

        // ��ų ���ӽð� ó��
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

            // ��ƼŬ �ý����� ��� ���� ��ƼŬ�� ��� ����� ������ ���
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

            // VFX�� ��� ��� ���� �� ����
            VisualEffect vfx = vfxObject.GetComponent<VisualEffect>();
            if (vfx != null)
            {
                vfx.Stop();
                Destroy(vfxObject);
                return;
            }

            // �� ���� ��� ��� ����
            Destroy(vfxObject);
        }

    }
}
