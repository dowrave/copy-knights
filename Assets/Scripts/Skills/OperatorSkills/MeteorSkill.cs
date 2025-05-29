using UnityEngine;

namespace Skills.Base
{
    [CreateAssetMenu(fileName = "New Meteor Skill", menuName = "Skills/Meteor Skill")]
    public class MateorSkill : AreaEffectSkill
    {
        [Header("MateorSkill Settings")]
        [SerializeField] private float damageMultiplier = 0.5f;
        [SerializeField] private float stunDuration = 2f;
        [SerializeField] private int costRecovery = 10;
        [SerializeField] private GameObject meteorPrefab = default!; // �������� �޽� ��ü
        [SerializeField] private Vector2 meteorHeights = new Vector2(4f, 5f); // �� ������Ʈ�� ����
        [SerializeField] private float meteorDelay = 0.5f;

        protected override GameObject? CreateEffectField(Operator op, Vector2Int centerPos)
        {
            enemyIdSet.Clear();

            // �ڽ�Ʈ ȸ��
            StageManager.Instance!.RecoverDeploymentCost(costRecovery);

            // ���� ���� ���鿡�� ���׿� ����
            foreach (Vector2Int pos in actualSkillRange)
            {
                Tile? tile = MapManager.Instance!.GetTile(pos.x, pos.y); 
                if (tile != null) 
                {
                    foreach (Enemy enemy in tile.GetEnemiesOnTile())
                    {
                        int enemyId = enemy.GetInstanceID();
                        if (enemyIdSet.Add(enemyId)) // �ߺ��� enemy�� �ƴϾ �ؽ��¿� �߰��� �����ϸ�
                        {
                            CreateMeteorSequence(op, enemy);
                        }
                    }
                }
            }

            return null;
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
            float actualDamage = op.AttackPower * damageMultiplier;

            MeteorController? controller = meteorObj.GetComponent<MeteorController>();

            if (controller != null && hitEffectPrefab != null)
            {
                controller.Initialize(op, target, actualDamage, delayTime, stunDuration, hitEffectPrefab, skillHitEffectTag);
            }
        }

        protected override Vector2Int GetCenterPos(Operator op)
        {
            // mainTarget�� �߽����� �����ǹǷ�
            return MapManager.Instance!.ConvertToGridPosition(op.transform.position);
        }
    }
}

