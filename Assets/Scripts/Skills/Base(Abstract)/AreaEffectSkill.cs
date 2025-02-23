using System.Collections.Generic;
using UnityEngine;

// ���� �ΰ��ӿ��� ����Ǵ� ȿ���� Effect, �ð��� ȿ���� VFX�� ������
namespace Skills.Base
{
    // ������ ������ ���ϴ� ��ų�� �̷��� ������ ����
    public abstract class AreaEffectSkill: ActiveSkill
    {
        [Header("AreaEffectSkill References")]
        [SerializeField] protected GameObject fieldEffectPrefab; // �������� ȿ�� ������
        [SerializeField] protected GameObject skillRangeVFXPrefab; // �ð� ȿ�� ������
        [SerializeField] protected List<Vector2Int> skillRangeOffset;
        [SerializeField] protected string EFFECT_TAG;

        protected UnitEntity mainTarget;
        protected HashSet<Vector2Int> actualSkillRange = new HashSet<Vector2Int>();

        protected GameObject hitEffectPrefab;
        protected Dictionary<Operator, List<GameObject>> activeEffects = new Dictionary<Operator, List<GameObject>>();

        // Ÿ�� �ߺ� ���ɼ��� �����ϱ� ���� �ؽ���
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

        // ���� ����Ʈ ���� + ȿ�� ����
        protected override void PlaySkillEffect(Operator op)
        {
            // ��ų ���� ���
            Vector2Int centerPos = GetCenterPos(op);
            CalculateActualSkillRange(centerPos);

            // ����Ʈ �ð�ȭ
            VisualizeActualSkillRange(op);

            // ���� ȿ�� ���� ����
            GameObject fieldEffect = CreateEffectField(op, centerPos);

            // �ʵ� ȿ�� ���� �� ��� �̺�Ʈ ����
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
            // ��ȿ�� Ÿ�Ͽ��� VFX ����
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
            // �� ĭ 
        }

        public override void InitializeSkillObjectPool()
        {
            ObjectPoolManager.Instance.CreatePool(EFFECT_TAG, skillRangeVFXPrefab, skillRangeOffset.Count);
        }

        public override void CleanupSkill()
        {
            // ���� �߿�
            CleanupSkillObjectPool();
            actualSkillRange.Clear();
        }

        public void CleanupSkillObjectPool()
        {
            // ��� Ȱ�� ȿ�� ����
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

        protected abstract GameObject CreateEffectField(Operator op, Vector2Int centerPos); // ��ų�� ���� ȿ�� ����
        protected abstract Vector2Int GetCenterPos(Operator op);
    }

}