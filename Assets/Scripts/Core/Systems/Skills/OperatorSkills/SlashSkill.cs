using System.Collections.Generic;
using Skills.Base;
using UnityEngine;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Slash Skill", menuName = "Skills/SlashSkill")]
    public class SlashSkill : ActiveSkill
    {
        [SerializeField] private float effectDuration = 0.5f; // 마지막 대미지 판정이 발생하는 시점

        [Header("Damage Settings")]
        [SerializeField] private float firstDamageMultiplier = 2f; // 1타 때 들어가는 대미지
        [SerializeField] private float secondDamageMultiplier = 1.25f; // 여러 틱에 걸쳐 들어가는 대미지 

        [Header("VFX Settings")]
        [SerializeField] private float vfxDuration = 2f;
        [SerializeField] private GameObject slashControllerPrefab = default!;

        private string _slashControllerTag; 

        protected override void SetDefaults()
        {
            duration = 0f;
        }

        public override void PreloadObjectPools(OperatorData ownerData)
        {
            base.PreloadObjectPools(ownerData);

            if (slashControllerPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(SlashControllerTag, slashControllerPrefab, 1);

                // skillPoolTag = RegisterPool(ownerData, slashControllerPrefab, 3);
            }
        }

        public string SlashControllerTag
        {
            get
            {
                if (string.IsNullOrEmpty(_slashControllerTag))
                {
                    _slashControllerTag = $"{skillName}_SlashController";
                }
                return _slashControllerTag;
            }
        }        


        protected override void PlaySkillEffect(Operator caster)
        {
            if (slashControllerPrefab == null) return;

            // 스킬 중에는 버프 (공격 불가 버프 해제는 컨트롤러에서 공격 판정 끝나면 진행)
            caster.AddBuff(new CannotAttackBuff(effectDuration, this));

            // 풀에서 오브젝트 생성
            GameObject effectObj = ObjectPoolManager.Instance.SpawnFromPool(SlashControllerTag, caster.transform.position, caster.transform.rotation);

            // 스킬 범위 정의
            HashSet<Vector2Int> skillRange = SetSkillRange(caster);

            // 컨트롤러를 이용한 초기화
            SlashSkillController controller = effectObj.GetComponent<SlashSkillController>();
            if (controller != null)
            {
                controller.Initialize(caster, 
                    vfxDuration, 
                    skillRange, 
                    firstDamageMultiplier, 
                    secondDamageMultiplier, 
                    caster.OperatorData.HitEffectPrefab, 
                    caster.HitEffectTag, 
                    SlashControllerTag, 
                    this
                );
            }
        }

        private HashSet<Vector2Int> SetSkillRange(Operator caster)
        {
            HashSet<Vector2Int> skillRangeTiles = new HashSet<Vector2Int>();

            // 오퍼레이터의 위치 포함
            Vector2Int operatorGridPos = MapManager.Instance!.ConvertToGridPosition(caster.transform.position);
            skillRangeTiles.Add(operatorGridPos);

            // 오퍼레이터의 방향을 고려해 스킬 범위 추가
            foreach (Vector2Int baseOffset in skillRangeOffset)
            {
                Vector2Int rotatedOffset = PositionCalculationSystem.RotateGridOffset(baseOffset, caster.FacingDirection);
                Vector2Int targetPos = operatorGridPos + rotatedOffset;
                skillRangeTiles.Add(targetPos);
            }

            return skillRangeTiles;
        }
    }
}

