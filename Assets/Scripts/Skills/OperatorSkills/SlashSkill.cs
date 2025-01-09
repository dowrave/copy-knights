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
        [SerializeField] private GameObject slashEffectPrefab;
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

        // 이펙트 구현 및 동작이 주요 매커니즘이므로 별도의 이펙트 생성 동작은 진행하지 않음
        public override void Activate(Operator op)
        {
            if (!op.IsDeployed || !op.CanUseSkill()) return;
            Debug.Log("SlashSkill Activate 동작");
            
            op.CurrentSP = 0;
            Vector2Int operatorGridPos = MapManager.Instance.ConvertToGridPosition(op.transform.position);
            Vector3 direction = op.FacingDirection;

            if (slashEffectPrefab != null)
            {
                Vector3 spawnPosition = op.transform.position;
                GameObject effectObj = Instantiate(slashEffectPrefab, spawnPosition, Quaternion.LookRotation(direction));

                // 이펙트 컨트롤러 추가 및 초기화
                SlashSkillEffectController effectController = effectObj.GetComponent<SlashSkillEffectController>();
                effectController.Initialize(op, direction, effectSpeed, effectLifetime, damageMultiplier, attackRange);
            }
        }
    }
}

