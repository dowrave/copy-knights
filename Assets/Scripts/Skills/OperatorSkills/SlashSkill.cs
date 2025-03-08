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

        [Header("Attack Range")]
        [SerializeField]
        // 범위에 설정하는 벡터 기준은 왼쪽 방향임
        private List<Vector2Int> attackRange = new List<Vector2Int>
        {
            new Vector2Int(-1, 0),
            new Vector2Int(-2, 0),
            new Vector2Int(-3, 0)
        };

        protected override void SetDefaults()
        {
            autoRecover = true;
        }

        protected override void PlaySkillEffect(Operator op)
        {
            Vector2Int operatorGridPos = MapManager.Instance!.ConvertToGridPosition(op.transform.position);
            Vector3 direction = op.FacingDirection;

            if (slashEffectPrefab != null)
            {
                Vector3 spawnPosition = op.transform.position;
                GameObject effectObj = Instantiate(slashEffectPrefab, spawnPosition, Quaternion.LookRotation(direction));

                // 이펙트 컨트롤러 추가 및 초기화
                SlashSkillController? effectController = effectObj.GetComponent<SlashSkillController>();
                GameObject? hitEffectPrefab = op.OperatorData.HitEffectPrefab;
                if (effectController != null && hitEffectPrefab != null)
                {
                    effectController.Initialize(op, direction, effectSpeed, effectLifetime, damageMultiplier, attackRange, hitEffectPrefab);

                }
            }
        }
    }
}

