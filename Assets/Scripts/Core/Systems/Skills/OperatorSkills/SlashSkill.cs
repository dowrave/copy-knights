using System.Collections.Generic;
using Skills.Base;
using UnityEngine;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Slash Skill", menuName = "Skills/SlashSkill")]
    public class SlashSkill : ActiveSkill
    {
        [Header("Damage Settings")]
        [SerializeField] private float damageMultiplier = 2f;

        [Header("Skill Settings")]
        [SerializeField] private GameObject slashEffectPrefab = default!;
        [SerializeField] private float effectSpeed = 8f;
        [SerializeField] private float effectLifetime = 0.5f;

        protected override void SetDefaults()
        {
            duration = 0f;
        }

        protected override void PlaySkillEffect(Operator op)
        {
            if (slashEffectPrefab == null) return;

            Vector3 spawnPosition = op.transform.position + op.transform.forward * 0.5f;
            Quaternion spawnRotation = Quaternion.LookRotation(op.FacingDirection);
            GameObject effectObj = Instantiate(slashEffectPrefab, spawnPosition, spawnRotation);

            SlashSkillController controller = effectObj.GetComponent<SlashSkillController>();
            if (controller != null)
            {
                controller.Initialize(op, op.FacingDirection, effectSpeed, effectLifetime, damageMultiplier, skillRangeOffset, op.OperatorData.HitEffectPrefab, op.HitEffectTag);
            }
        }
    }
}

