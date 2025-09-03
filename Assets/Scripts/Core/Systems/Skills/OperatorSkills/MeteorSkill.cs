using UnityEngine;
using System.Collections;
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
        [SerializeField] private Vector2 meteorHeights = new Vector2(); // �� ������Ʈ�� ����
        [SerializeField] private GameObject hitEffectPrefab = default!;
        [SerializeField] private GameObject skillRangeVFXPrefab = default!;
        [SerializeField] private float fallSpeed = 10f;

        private float meteorDelay = 0.5f;

        string METEOR_TAG; // ���׿� ȿ�� �±�
        string SKILL_RANGE_VFX_TAG; // �ʵ� ���� VFX �±�
        string HIT_EFFECT_TAG; // Ÿ�� ����Ʈ �±�

        protected override void SetDefaults()
        {
            duration = 0f; // ��߼� ��ų ���
        }

        protected override void PlaySkillEffect(Operator caster)
        {
            if (hitEffectPrefab == null)
            {
                hitEffectPrefab = caster.OperatorData.HitEffectPrefab;
            }

            // �ڽ�Ʈ ȸ��
            StageManager.Instance!.RecoverDeploymentCost(costRecovery);

            // ���� ���
            Vector2Int centerPos = GetCenterGridPos(caster);

            // ���� ��ų�� �߽� ��ġ�� �޶��� ��쿡�� ���� ������ �����
            if (centerPos != caster.LastSkillCenter)
            {
                HashSet<Vector2Int> skillRange = PositionCalculationSystem.CalculateRange(skillRangeOffset, centerPos, caster.FacingDirection);
                caster.SetCurrentSkillRange(skillRange);
                caster.SetLastSkillCenter(centerPos);
            }

            VisualizeSkillRange(caster, caster.GetCurrentSkillRange());

            // �ߺ� Ÿ�� ������ ���� ID ��Ʈ
            var enemyIdSet = new HashSet<int>();

            // ���� ���� ��� ������ ���׿� ��ȯ
            foreach (Vector2Int pos in caster.GetCurrentSkillRange())
            {
                Tile? tile = MapManager.Instance!.GetTile(pos.x, pos.y);
                if (tile != null)
                {
                    foreach (Enemy enemy in tile.EnemiesOnTile.ToList())
                    {
                        if (enemyIdSet.Add(enemy.GetInstanceID()))
                        {
                            // �ڷ�ƾ�� Monobehaviour�� �޴� ��ü������ ���� ����
                            // �� ��ũ��Ʈ�� ScriptableObject�� �����. ���� ������ ��ü���� ��û�Ѵ�.
                            caster.StartCoroutine(CreateMeteorSequence(caster, enemy));
                        }
                    }
                }
            }
            
            
        }

        private IEnumerator CreateMeteorSequence(Operator caster, Enemy target)
        {
            CreateMeteor(caster, target, meteorHeights.x);

            yield return new WaitForSeconds(meteorDelay);

            CreateMeteor(caster, target, meteorHeights.y);
        }

        private void CreateMeteor(Operator caster, Enemy target, float height)
        {
            if (target != null)
            {
                Vector3 spawnPos = target.transform.position + Vector3.up * height;
                GameObject meteorObj = Instantiate(meteorPrefab, spawnPos, Quaternion.identity, target.transform);

                MeteorController? controller = meteorObj.GetComponent<MeteorController>();

                if (controller != null)
                {
                    float actualDamage = caster.AttackPower * damageMultiplier;
                    controller.Initialize(caster, target, actualDamage, fallSpeed, stunDuration, hitEffectPrefab, HIT_EFFECT_TAG);
                }
            }
        }

        public override void InitializeSkillObjectPool(UnitEntity caster)
        {
            if (caster is Operator op)
            {
                base.InitializeSkillObjectPool(caster);
                InitializeSkillObjectPool(op);
            }
        }

        public void InitializeSkillObjectPool(Operator caster)
        {
            if (caster is Operator op)
            {
                if (meteorPrefab != null)
                {
                    METEOR_TAG = RegisterPool(op, meteorPrefab, 10);
                }
                if (skillRangeVFXPrefab != null)
                {
                    SKILL_RANGE_VFX_TAG = RegisterPool(op, skillRangeVFXPrefab, skillRangeOffset.Count);
                }
                if (hitEffectPrefab != null)
                {
                    HIT_EFFECT_TAG = RegisterPool(op, hitEffectPrefab, 10);
                }
            }
        }

        protected void VisualizeSkillRange(Operator op, IReadOnlyCollection<Vector2Int> range)
        {
            foreach (Vector2Int pos in range)
            {
                if (MapManager.Instance!.CurrentMap!.IsValidGridPosition(pos.x, pos.y))
                {
                    Vector3 worldPos = MapManager.Instance!.ConvertToWorldPosition(pos);
                    GameObject? vfxObj = ObjectPoolManager.Instance?.SpawnFromPool(SKILL_RANGE_VFX_TAG, worldPos, Quaternion.identity);

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

        protected void VisualizeSkillRange(UnitEntity caster, IReadOnlyCollection<Vector2Int> range)
        {
            if (caster is Operator op)
            {
                VisualizeSkillRange(op, range);
            }
        }
    }
}

