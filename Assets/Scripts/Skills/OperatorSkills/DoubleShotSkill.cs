using System.Collections;
using Skills.Base;
using UnityEngine;
using UnityEngine.VFX;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Double Shot Skill", menuName = "Skills/Double Shot Skill")]
    public class DoubleShotSkill: Skill
    {
        [Header("Skill Settings")]
        [SerializeField] private float damageMultiplier = 1.2f;
        [SerializeField] private float delayBetweenShots = 0.1f;
        [SerializeField] private GameObject SkillEffectPrefab; // Ȱ��ȭ �� �����ϴ� ����Ʈ
        [SerializeField] private float skillDuration = 15f;

        private bool isSkillActive = false;
        // ����Ʈ 
        GameObject buffEffectObject;
        VisualEffect skillEffectVFX;
        ParticleSystem skillEffectPS;

        public override bool AutoRecover => true;
        public override bool AutoActivate => false;
        public override bool ModifiesAttackAction => true;


        public override void Activate(Operator op)
        {
            if (!op.IsDeployed || !op.CanUseSkill()) return;

            isSkillActive = true;
            op.SetAttackCooldown(0f);
            PlaySkillEffect(op);

            op.StartCoroutine(HandleSkillDuration(op, skillDuration));

            // ���⿡ �ڵ带 �ۼ��ϸ� �ڷ�ƾ ����� ���� ���� �ٷ� �����
            //SafeDestroySkillEffect(buffEffectObject);
            //isSkillActive = false;
        }

        public void PlaySkillEffect(Operator op)
        {
            if (SkillEffectPrefab != null)
            {
                Vector3 buffEffectPosition = new Vector3(op.transform.position.x, 0.05f, op.transform.position.z);
                buffEffectObject = Instantiate(SkillEffectPrefab, buffEffectPosition, Quaternion.identity);
                buffEffectObject.transform.SetParent(op.transform);

                // VFX�� ��ƼŬ �ý��� ��������
                if (buffEffectObject.GetComponent<VisualEffect>() != null)
                {
                    skillEffectVFX = buffEffectObject.GetComponent<VisualEffect>();
                    skillEffectVFX.Play();
                }
                else if (buffEffectObject.GetComponent<ParticleSystem>() != null)
                {
                    skillEffectPS = buffEffectObject.GetComponent<ParticleSystem>();
                    skillEffectPS.Play();
                }
            }
        }

        public void SafeDestroySkillEffect(GameObject effectObject)
        {
            if (effectObject == null) return;

            var ps = effectObject.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(effectObject, ps.main.duration + ps.main.startLifetime.constantMax);
                return;
            }

            var vfx = effectObject.GetComponent<VisualEffect>();
            if (vfx != null)
            {
                Destroy(effectObject);
                return;
            }
        }

        public override void PerformSkillAction(Operator op)
        {
            if (isSkillActive)
            {
                op.StartCoroutine(PerformDoubleShot(op));
            }
        }

        private IEnumerator PerformDoubleShot(Operator op)
        {
            UnitEntity target = op.CurrentTarget;
            float modifiedDamage = op.AttackPower * damageMultiplier;

            op.Attack(target, modifiedDamage);
            yield return new WaitForSeconds(delayBetweenShots);

            if (target != null && !target.Equals(null))
            {
                op.Attack(target, modifiedDamage);
                yield return new WaitForSeconds(1f / op.AttackSpeed);
            }
        }
    }
}