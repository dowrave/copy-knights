using System;
using System.Collections;
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
        [SerializeField] private GameObject meteorPrefab; // �������� �޽� ��ü
        [SerializeField] private Vector2 meteorHeights = new Vector2(2f, 3f); // �� ������Ʈ�� ����
        [SerializeField] private float meteorDelay = 0.5f;

        protected override GameObject CreateEffectField(Operator op, Vector2Int centerPos)
        {
            // �ڽ�Ʈ ȸ��
            StageManager.Instance.RecoverDeploymentCost(costRecovery);

            // ���� ���� ���鿡�� ���׿� ����
            foreach (Vector2Int pos in actualSkillRange)
            {
                Tile tile = MapManager.Instance.GetTile(pos.x, pos.y); 
                if (tile != null)
                {
                    foreach (Enemy enemy in tile.GetEnemiesOnTile())
                    {
                        // StartCoroutine�� MonoBehaviour������ ���� ȣ���� ������
                        op.StartCoroutine(CreateMeteorSequence(op, enemy));
                    }
                }
            }

            return null;
        }

        private IEnumerator CreateMeteorSequence(Operator op, Enemy target)
        {
            MeteorController firstMeteor = CreateMeteor(op, target, meteorHeights.x);
            MeteorController secondMeteor = CreateMeteor(op, target, meteorHeights.y);

            yield return new WaitForSeconds(meteorDelay);

            if (firstMeteor != null)
            {
                float damage = op.AttackPower * damageMultiplier;
                firstMeteor.Initialize(op, target, damage, stunDuration);
            }

            yield return new WaitForSeconds(meteorDelay);

            if (secondMeteor != null)
            {
                float damage = op.AttackPower * damageMultiplier;
                secondMeteor.Initialize(op, target, damage, stunDuration);
            }
        }

        private MeteorController CreateMeteor(Operator op, Enemy target, float height)
        {
            Vector3 spawnPos = target.transform.position + Vector3.up * height;

            GameObject meteorObj = Instantiate(meteorPrefab, spawnPos, Quaternion.Euler(90, 0, 0), target.transform);

            MeteorController controller = meteorObj.GetComponent<MeteorController>();
            return controller;
        }

        protected override Vector2Int GetCenterPos(Operator op)
        {
            // mainTarget�� �߽����� �����ǹǷ�
            return MapManager.Instance.ConvertToGridPosition(op.transform.position);
        }
    }
}

