using System.Collections.Generic;
using UnityEngine;

// 실제 인게임에서 적용되는 효과는 Effect, 시각적 효과는 VFX로 구분함
namespace Skills.Base
{
    // 범위에 영향을 가하는 스킬은 이러한 구현을 따름
    public abstract class AreaEffectSkill: ActiveSkill
    {
        [Header("AreaEffectSkill References")]
        [SerializeField] protected GameObject fieldEffectPrefab; // 실질적인 효과 프리팹
        [SerializeField] protected GameObject skillRangeVFXPrefab; // 시각 효과 프리팹
        [SerializeField] protected List<Vector2Int> skillRangeOffset;
        [SerializeField] protected string EFFECT_TAG;

        protected UnitEntity mainTarget;
        protected HashSet<Vector2Int> actualSkillRange = new HashSet<Vector2Int>();

        protected GameObject hitEffectPrefab;
        protected Dictionary<Operator, List<GameObject>> activeEffects = new Dictionary<Operator, List<GameObject>>();

        // 타겟 중복 가능성을 제거하기 위한 해쉬셋
        protected HashSet<int> enemyIdSet = new HashSet<int>();


        protected override void OnSkillStart(Operator op)
        {
            base.OnSkillStart(op);
            hitEffectPrefab = op.BaseData.HitEffectPrefab ?? null;
        }

        protected override void SetDefaults()
        {
            autoRecover = true;
            autoActivate = false;
            modifiesAttackAction = true;
        }

        // 장판 이펙트 생성 + 효과 적용
        protected override void PlaySkillEffect(Operator op)
        {
            // 스킬 범위 계산
            Vector2Int centerPos = GetCenterPos(op);
            CalculateActualSkillRange(centerPos);

            // 이펙트 시각화
            VisualizeActualSkillRange(op);

            // 실제 효과 장판 생성
            GameObject fieldEffect = CreateEffectField(op, centerPos);

            // 필드 효과 추적 및 사망 이벤트 구독
            if (fieldEffect != null)
            {
                if (!activeEffects.ContainsKey(op))
                {
                    TrackEffect(op, fieldEffect);
                    op.OnOperatorDied += HandleOperatorDeath; 
                }
            }
        }

        private void CalculateActualSkillRange(Vector2Int center)
        {
            foreach (Vector2Int offset in skillRangeOffset)
            {
                Vector2Int rotatedOffset = DirectionSystem.RotateGridOffset(offset, caster.FacingDirection);
                actualSkillRange.Add(center + rotatedOffset);
            }
        }

        private void VisualizeActualSkillRange(Operator op)
        {
            // 유효한 타일에만 VFX 생성
            foreach (Vector2Int pos in actualSkillRange)
            {
                if (MapManager.Instance.CurrentMap.IsValidGridPosition(pos.x, pos.y))
                {
                    GameObject vfxObj = ObjectPoolManager.Instance.SpawnFromPool(
                        EFFECT_TAG,
                        MapManager.Instance.ConvertToWorldPosition(pos),
                        Quaternion.identity
                    );

                    TrackEffect(op, vfxObj);

                    var rangeEffect = vfxObj.GetComponent<SkillRangeVFXController>();
                    rangeEffect.Initialize(pos, actualSkillRange, duration, EFFECT_TAG);
                }
            }
        }

        public override void PerformChangedAttackAction(Operator op)
        {
            // 빈 칸 
        }

        public override void InitializeSkillObjectPool()
        {
            ObjectPoolManager.Instance.CreatePool(EFFECT_TAG, skillRangeVFXPrefab, skillRangeOffset.Count);
        }

        public override void CleanupSkill()
        {
            // 서순 중요
            CleanupSkillObjectPool();
            actualSkillRange.Clear();
        }

        public void CleanupSkillObjectPool()
        {
            // 모든 활성 효과 정리
            foreach (var pair in activeEffects)
            {
                foreach (GameObject effect in pair.Value)
                {
                    if (effect != null)
                    {
                        var controller = effect.GetComponent<FieldEffectController>();
                        if (controller != null)
                        {
                            controller.ForceRemove();
                        }
                        else
                        {
                            Destroy(effect);
                        }
                    }
                }
            }

            ObjectPoolManager.Instance.RemovePool(EFFECT_TAG);
        }

        private void TrackEffect(Operator op, GameObject effect)
        {
            if (!activeEffects.ContainsKey(op))
            {
                activeEffects[op] = new List<GameObject>();
            }
            activeEffects[op].Add(effect);
        }

        private void HandleOperatorDeath(Operator op)
        {
            if (activeEffects.TryGetValue(op, out List<GameObject> effects))
            {
                foreach (GameObject effect in effects)
                {
                    if (effect == null) continue;

                    var controller = effect.GetComponent<FieldEffectController>();
                    if (controller != null)
                    {
                        controller.ForceRemove();
                    }
                    else
                    {
                        Destroy(effect);
                    }
                }
            }
            activeEffects.Remove(op);
        }

        protected abstract GameObject CreateEffectField(Operator op, Vector2Int centerPos); // 스킬의 실제 효과 구현
        protected abstract Vector2Int GetCenterPos(Operator op);
    }

}