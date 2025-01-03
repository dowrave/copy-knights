using System.Collections.Generic;
using Skills.Base;
using UnityEngine;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Guard Skill", menuName = "Skills/GuardPromotionSkill")]
    public class GuardPromotionSkill : Skill
    {
        [Header("Skill Settings")]
        [SerializeField] private float damageMultiplier = 2f;
        [SerializeField] private GameObject skillEffectPrefab;
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

        public override bool AutoRecover => true;
        public override bool AutoActivate => false;

        public override void Activate(Operator op)
        {
            if (!op.IsDeployed || !op.CanUseSkill()) return;
            
            op.CurrentSP = 0;

            Vector2Int operatorGridPos = MapManager.Instance.ConvertToGridPosition(op.transform.position);
            Vector3 direction = op.FacingDirection;

            if (skillEffectPrefab != null)
            {
                Vector3 spawnPosition = op.transform.position;
                GameObject effectObj = Instantiate(skillEffectPrefab, spawnPosition, Quaternion.LookRotation(direction));

                // 이펙트 컨트롤러 추가 및 초기화
                GuardPromoFXController effectController = effectObj.AddComponent<GuardPromoFXController>();
                effectController.Initialize(op, direction, effectSpeed, effectLifetime, damageMultiplier, attackRange);
            }
        }
    }
}

