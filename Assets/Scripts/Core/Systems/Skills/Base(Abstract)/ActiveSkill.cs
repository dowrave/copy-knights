
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
        [SerializeField] protected GameObject skillVFXPrefab = default!;

        [Header("Optional) Skill Range")]
        [SerializeField] protected bool activeFromOperatorPosition = true; // UI�� ��Ÿ� ǥ���� �� �߽��� �Ǵ� �κ��� ���� �����ϱ� ���� ������� �ʵ�
        [SerializeField] protected List<Vector2Int> skillRangeOffset = new List<Vector2Int>(); // ���� ����

        [Header("For UI")]
        [Tooltip("UI�� ������� ������. +���� -x�������� �̵���.")]
        [SerializeField] protected float rectOffset; // UI�� ������

        public IReadOnlyList<Vector2Int> SkillRangeOffset => skillRangeOffset;
        public bool ActiveFromOperatorPosition => activeFromOperatorPosition;
        public float RectOffset => rectOffset;

        protected GameObject? VfxInstance;
        protected VisualEffect? VfxComponent;
        protected ParticleSystem? VfxPs;

        protected HashSet<Vector2Int> actualSkillRange = new HashSet<Vector2Int>();

        public override void Activate(Operator op)
        {
            CommonActivation(op);

            // ���ӽð��� ���� ����
            // �̿��� ��Ȳ�� �޼��� �������̵� ���� (ammoBased�� �̹� �׷��� ������)
            if (duration > 0)
            {
                op.StartSkillCoroutine(Co_HandleSkillDuration(op));
            }
            else
            {
                PlayInstantSkill(op);
            }
        }

        // ������ ����, VFX ����, ���� �ʱ�ȭ ��� ����� ������ �����Ѵ�.
        protected virtual void CommonActivation(Operator op)
        {
            caster = op;
            if (!op.IsDeployed || !op.CanUseSkill()) return;

            // �⺻ ����Ʈ�� �߰� ����Ʈ ���
            PlaySkillVFX(op);
            PlayAdditionalVFX(op);

            // ��ų ��� ���Ŀ��� ���� �ӵ� / ��� �ʱ�ȭ
            op.SetAttackDuration(0f);
            op.SetAttackCooldown(0f);

            op.SetSkillOnState(true);
        }

        // �߰� ����Ʈ
        protected virtual void PlayAdditionalVFX(Operator op) { }

        // ���� �ð��� ���� (=�����) ��ų ����
        protected void PlayInstantSkill(Operator op)
        {
            PlaySkillEffect(op);
            OnSkillEnd(op);
        }

        // �������� ��ų ȿ�� ����.
        // ������ OnSkillStart�� OnSkillEnd ���̿� ��
        protected virtual void PlaySkillEffect(Operator op) { }

        protected virtual void OnSkillEnd(Operator op)
        {
            if (VfxInstance != null)
            {
                SafeDestroySkillVFX(VfxInstance);
            }

            op.CurrentSP = 0;
            op.SetSkillOnState(false);

            // ��ų�� ���� ���� ���� ��ٿ� �ʱ�ȭ - �ٷ� ���� �� �ְԲ�
            op.SetAttackDuration(0f);
            op.SetAttackCooldown(0f);

            // ���� ������ ���⼭ �������� �ʰ��� - �ڽĿ��� ����!
            
            // ���ӽð��� �ִ� ��ų�� ���۷������� �ڷ�ƾ �ʱ�ȭ
            if (duration > 0)
            {
                op.EndSkillCoroutine();
            }
        }

        // �⺻ ����Ʈ ���� �� ���
        protected virtual void PlaySkillVFX(Operator op)
        {
            if (skillVFXPrefab == null) return;

            Vector3 effectPosition = op.transform.position;
            VfxInstance = Instantiate(
                skillVFXPrefab,
                effectPosition,
                Quaternion.identity,
                op.transform    // ���۷������� �ڽ����� ����
            );

            if (VfxInstance != null)
            {
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
        }

        // ��ų ���ӽð� ó��
        public virtual IEnumerator Co_HandleSkillDuration(Operator op)
        {
            PlaySkillEffect(op);

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                if (op == null) yield break; // ���۷����� �ı� �ÿ� ������ ����
                
                elapsedTime += Time.deltaTime;

                // ������ �����ϴ� SP ������ ���⼭ ����
                // UI ������Ʈ�� op.CurrentSP�� ���Ϳ� ���� �ڵ����� �̾���.
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

        protected void CalculateActualSkillRange(Vector2Int center)
        {
            foreach (Vector2Int offset in skillRangeOffset)
            {
                Vector2Int rotatedOffset = DirectionSystem.RotateGridOffset(offset, Caster.FacingDirection);
                actualSkillRange.Add(center + rotatedOffset);
            }
        }
    }
}
