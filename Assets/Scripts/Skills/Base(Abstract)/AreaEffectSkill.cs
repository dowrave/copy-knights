using System.Collections.Generic;
using UnityEngine;

// ���� �ΰ��ӿ��� ����Ǵ� ȿ���� Effect, �ð��� ȿ���� VFX�� ������
namespace Skills.Base
{
    // ������ ������ ���ϴ� ��ų�� �̷��� ������ ����
    public abstract class AreaEffectSkill: ActiveSkill
    {
        [Header("AreaEffectSkill References")]
        [Tooltip("���� ȿ���� ����ϴ� ������")]
        [SerializeField] protected GameObject fieldEffectPrefab = default!; // �������� ȿ�� ������

        [Tooltip("��ų ȿ�� ������ �ð��� ����Ʈ�� ����ϴ� ������")]
        [SerializeField] protected GameObject skillRangeVFXPrefab = default!; // �ð� ȿ�� ������

        [Tooltip("��ų�� Ÿ�� ����Ʈ ������")]
        [SerializeField] protected GameObject? hitEffectPrefab = default!;

        // ��ų ������ Ÿ�� ����Ʈ �±�
        protected string skillRangeEffectTag;
        protected string skillHitEffectTag;


        protected UnitEntity? mainTarget;


        protected Dictionary<Operator, List<GameObject>> activeEffects = new Dictionary<Operator, List<GameObject>>();

        // Ÿ�� �ߺ� ���ɼ��� �����ϱ� ���� �ؽ���
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
                // �Ҵ���� ���� ��� ���۷������� Ÿ�� ����Ʈ�� ���
                hitEffectPrefab = op.OperatorData.HitEffectPrefab;
            }

            // ��ų ������ Ÿ�� ����Ʈ �±׸� ����
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
            // ��ų ���� �ʱ�ȭ
            actualSkillRange.Clear();

            // Ȱ��ȭ�� ��ų ȿ���� VFX�� ������
            activeEffects.Clear();

            base.OnSkillEnd(op);
        }

        // ��ų�� ����� ȿ���� �ð��� ����Ʈ ����
        protected override void PlaySkillEffect(Operator op)
        {
            // ��ų ���� ���
            Vector2Int centerPos = GetCenterPos(op);
            CalculateActualSkillRange(centerPos);

            // ����Ʈ �ð�ȭ
            VisualizeActualSkillRange(op);

            // �ð� ��Ұ� �ƴ�, ���� ȿ�� ���� ����
            GameObject? fieldEffect = CreateEffectField(op, centerPos);

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

        // ��ų ���� ����Ʈ �ð�ȭ
        private void VisualizeActualSkillRange(Operator op)
        {
            // ��ȿ�� Ÿ�Ͽ��� VFX ����
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
            // ������ ��ġ�� ��ų�� �⺻ ���� �׼��� �������� ����
        }

        public override void InitializeSkillObjectPool()
        {
            ObjectPoolManager.Instance!.CreatePool(skillRangeEffectTag, skillRangeVFXPrefab, skillRangeOffset.Count);
        }

        // ��ų�� "������" ������ �� ����Ѵ�
        public override void CleanupSkill()
        {
            // ���� �߿�
            CleanupSkillObjectPool(); // ������Ʈ Ǯ�� �ƿ� ������
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

            // �±׿� �ش��ϴ� ������Ʈ Ǯ ����
            ObjectPoolManager.Instance!.RemovePool(skillRangeEffectTag);
        }

        // ���� ȿ��, ���־� ����Ʈ ��� �̰����� ������
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

        protected abstract GameObject? CreateEffectField(Operator op, Vector2Int centerPos); // ��ų�� ���� ȿ�� ����
        protected abstract Vector2Int GetCenterPos(Operator op);
    }

}