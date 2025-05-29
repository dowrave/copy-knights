using System.Collections.Generic;
using UnityEngine;

// 실제 인게임에서 적용되는 효과는 Effect, 시각적 효과는 VFX로 구분함
namespace Skills.Base
{
    // 범위에 영향을 가하는 스킬은 이러한 구현을 따름
    public abstract class AreaEffectSkill: ActiveSkill
    {
        [Header("AreaEffectSkill References")]
        [Tooltip("실제 효과를 담당하는 프리팹")]
        [SerializeField] protected GameObject fieldEffectPrefab = default!; // 실질적인 효과 프리팹

        [Tooltip("스킬 효과 범위의 시각적 이펙트를 담당하는 프리팹")]
        [SerializeField] protected GameObject skillRangeVFXPrefab = default!; // 시각 효과 프리팹

        [Tooltip("스킬의 타격 이펙트 프리팹")]
        [SerializeField] protected GameObject? hitEffectPrefab = default!;

        // 스킬 범위와 타격 이펙트 태그
        protected string skillRangeEffectTag;
        protected string skillHitEffectTag;


        protected UnitEntity? mainTarget;


        protected Dictionary<Operator, List<GameObject>> activeEffects = new Dictionary<Operator, List<GameObject>>();

        // 타겟 중복 가능성을 제거하기 위한 해쉬셋
        protected HashSet<int> enemyIdSet = new HashSet<int>();

        public override void Activate(Operator op)
        {
            CreateHitEffectObjectPool(op);
            base.Activate(op);
        }

        protected void CreateHitEffectObjectPool(Operator op)
        {
            if (hitEffectPrefab == null)
            {
                // 할당되지 않은 경우 오퍼레이터의 타격 이펙트를 사용
                hitEffectPrefab = op.OperatorData.HitEffectPrefab;
            }

            // 스킬 범위와 타격 이펙트 태그를 설정
            skillRangeEffectTag = $"{op.OperatorData.entityName}_SkillRangeVFX";
            skillHitEffectTag = $"{op.OperatorData.entityName}_SkillHitEffect";

            ObjectPoolManager.Instance!.CreatePool(skillHitEffectTag, hitEffectPrefab, 10);
        }


        protected override void OnSkillStart(Operator op)
        {
            base.OnSkillStart(op);
        }

        protected override void SetDefaults()
        {
            autoRecover = true;
            autoActivate = false;
            modifiesAttackAction = true;
        }

        protected override void OnSkillEnd(Operator op)
        {
            // 스킬 범위 초기화
            actualSkillRange.Clear();

            // 활성화된 스킬 효과와 VFX를 제거함
            activeEffects.Clear();

            base.OnSkillEnd(op);
        }

        // 스킬의 기능적 효과와 시각적 이펙트 실행
        protected override void PlaySkillEffect(Operator op)
        {
            // 스킬 범위 계산
            Vector2Int centerPos = GetCenterPos(op);
            CalculateActualSkillRange(centerPos);

            // 이펙트 시각화
            VisualizeActualSkillRange(op);

            // 시각 요소가 아닌, 실제 효과 장판 생성
            GameObject? fieldEffect = CreateEffectField(op, centerPos);

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

        // 스킬 범위 이펙트 시각화
        private void VisualizeActualSkillRange(Operator op)
        {
            // 유효한 타일에만 VFX 생성
            foreach (Vector2Int pos in actualSkillRange)
            {
                if (MapManager.Instance!.CurrentMap!.IsValidGridPosition(pos.x, pos.y))
                {
                    GameObject? vfxObj = ObjectPoolManager.Instance!.SpawnFromPool(
                                           skillRangeEffectTag,
                                           MapManager.Instance!.ConvertToWorldPosition(pos),
                                           Quaternion.identity
                                       );

                    if (vfxObj != null)
                    {
                        TrackEffect(op, vfxObj);

                        var rangeEffect = vfxObj.GetComponent<SkillRangeVFXController>();
                        if (rangeEffect != null)
                        {
                            rangeEffect.Initialize(pos, actualSkillRange, duration, skillRangeEffectTag);
                        }
                    }
                }
            }
        }

        public override void PerformChangedAttackAction(Operator op)
        {
            // 영역을 펼치는 스킬은 기본 공격 액션을 수행하지 않음
        }

        public override void InitializeSkillObjectPool()
        {
            ObjectPoolManager.Instance!.CreatePool(skillRangeEffectTag, skillRangeVFXPrefab, skillRangeOffset.Count);
        }

        // 스킬을 "완전히" 정리할 때 사용한다
        public override void CleanupSkill()
        {
            // 서순 중요
            CleanupSkillObjectPool(); // 오브젝트 풀을 아예 제거함
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

            // 태그에 해당하는 오브젝트 풀 제거
            ObjectPoolManager.Instance!.RemovePool(skillRangeEffectTag);
        }

        // 실제 효과, 비주얼 이펙트 모두 이것으로 동작함
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

        protected abstract GameObject? CreateEffectField(Operator op, Vector2Int centerPos); // 스킬의 실제 효과 구현
        protected abstract Vector2Int GetCenterPos(Operator op);
    }

}