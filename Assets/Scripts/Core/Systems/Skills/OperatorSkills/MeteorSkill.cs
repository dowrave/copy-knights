using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Skills.Base
{
    [CreateAssetMenu(fileName = "New Meteor Skill", menuName = "Skills/Meteor Skill")]
    public class MateorSkill : ActiveSkill
    {
        [Header("MateorSkill Settings")]
        [SerializeField] private float damageMultiplier = 0.5f;
        [SerializeField] private float stunDuration = 2f;
        [SerializeField] private int costRecovery = 10;
        [SerializeField] private GameObject meteorPrefab = default!; // �������� �޽� ��ü
        [SerializeField] private Vector2 meteorHeights = new Vector2(4f, 5f); // �� ������Ʈ�� ����
        [SerializeField] private float meteorDelay = 0.5f;
        [SerializeField] private GameObject hitEffectPrefab = default!;
        [SerializeField] private GameObject skillRangeVFXPrefab = default!;
        private string hitEffectTag = "MeteorHit";

        protected override void SetDefaults()
        {
            duration = 0f; // ��߼� ��ų ���
        }

        protected override void PlaySkillEffect(Operator op)
        {
            if (hitEffectPrefab == null)
            {
                hitEffectPrefab = op.OperatorData.HitEffectPrefab;
            }

            // �ڽ�Ʈ ȸ��
            StageManager.Instance!.RecoverDeploymentCost(costRecovery);

            // ���� ���
            caster = op;
            Vector2Int centerPos = MapManager.Instance!.ConvertToGridPosition(op.transform.position);
            actualSkillRange.Clear();
            CalculateActualSkillRange(centerPos);
            VisualizeSkillRange(op, actualSkillRange);

            // �ߺ� Ÿ�� ������ ���� ID ��Ʈ
            var enemyIdSet = new HashSet<int>();

            // ���� ���� ��� ������ ���׿� ��ȯ
            foreach (Vector2Int pos in actualSkillRange)
            {
                Tile? tile = MapManager.Instance!.GetTile(pos.x, pos.y);
                if (tile != null)
                {
                    foreach (Enemy enemy in tile.EnemiesOnTile.ToList())
                    {
                        if (enemyIdSet.Add(enemy.GetInstanceID()))
                        {
                            CreateMeteorSequence(op, enemy);
                        }
                    }
                }
            }
        }

        private void CreateMeteorSequence(Operator op, Enemy target)
        {
            CreateMeteor(op, target, meteorHeights.x, 0f);
            CreateMeteor(op, target, meteorHeights.y, meteorDelay);
        }

        private void CreateMeteor(Operator op, Enemy target, float height, float delayTime)
        {
            Vector3 spawnPos = target.transform.position + Vector3.up * height;
            GameObject meteorObj = Instantiate(meteorPrefab, spawnPos, Quaternion.Euler(90, 0, 0), target.transform);

            MeteorController? controller = meteorObj.GetComponent<MeteorController>();

            if (controller != null)
            {
                float actualDamage = op.AttackPower * damageMultiplier;
                controller.Initialize(op, target, actualDamage, delayTime, stunDuration, hitEffectPrefab, hitEffectTag);
            }
        }

        public override void InitializeSkillObjectPool(UnitEntity caster)
        {
            base.InitializeSkillObjectPool(caster);
            if (caster is Operator op)
            {
                if (meteorPrefab != null) ObjectPoolManager.Instance!.CreatePool($"{op.OperatorData.entityName}_{this.name}_Meteor", meteorPrefab, 10);
                if (hitEffectPrefab != null) ObjectPoolManager.Instance!.CreatePool($"{op.OperatorData.entityName}_{this.name}_HitEffect", hitEffectPrefab, 10);
                if (skillRangeVFXPrefab != null) ObjectPoolManager.Instance!.CreatePool($"{op.OperatorData.entityName}_{this.name}_RangeVFX", skillRangeVFXPrefab, skillRangeOffset.Count);
            }
        }

        private void VisualizeSkillRange(Operator op, HashSet<Vector2Int> range)
        {
            string vfxPoolTag = $"{this.name}_RangeVFX";

            foreach (Vector2Int pos in range)
            {
                if (MapManager.Instance!.CurrentMap!.IsValidGridPosition(pos.x, pos.y))
                {
                    Vector3 worldPos = MapManager.Instance!.ConvertToWorldPosition(pos);
                    GameObject? vfxObj = ObjectPoolManager.Instance?.SpawnFromPool(vfxPoolTag, worldPos, Quaternion.identity);

                    if (vfxObj != null)
                    {
                        // ��߼� ��ų�� �ð� ȿ���� ª�� �����ǹǷ�,
                        // �θ� �������� �ʾƵ� ������ ������� �ϴ� ���� �� ���� �� �ֽ��ϴ�.
                        // �Ǵ� op�� �׾��� �� �Բ� ������� �Ϸ��� �θ� ������ �����ϴ�. (������ ����)
                        vfxObj.transform.SetParent(op.transform);

                        var controller = vfxObj.GetComponent<SkillRangeVFXController>();
                        if (controller != null)
                        {
                            // duration�� 0�̹Ƿ�, ��Ʈ�ѷ��� ���������� ª�� �ð�(��: 1��) ���ȸ� ǥ���մϴ�.
                            controller.Initialize(pos, range, this.duration);
                        }
                    }
                }
            }
        }
    }
}

