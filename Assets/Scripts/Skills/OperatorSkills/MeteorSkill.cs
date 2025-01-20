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
        [SerializeField] private GameObject meteorPrefab; // 떨어지는 메쉬 자체
        [SerializeField] private Vector2 meteorHeights = new Vector2(4f, 5f); // 두 오브젝트의 높이
        [SerializeField] private float meteorDelay = 0.5f;

        protected override GameObject CreateEffectField(Operator op, Vector2Int centerPos)
        {
            // 코스트 회복
            StageManager.Instance.RecoverDeploymentCost(costRecovery);

            // 범위 내의 적들에게 메테오 생성
            foreach (Vector2Int pos in actualSkillRange)
            {
                Tile tile = MapManager.Instance.GetTile(pos.x, pos.y); 
                if (tile != null) 
                {
                    foreach (Enemy enemy in tile.GetEnemiesOnTile())
                    {
                        CreateMeteorSequence(op, enemy);
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

            MeteorController controller = meteorObj.GetComponent<MeteorController>();
            controller.Initialize(op, target, actualDamage, delayTime, stunDuration);
        }

        protected override Vector2Int GetCenterPos(Operator op)
        {
            // mainTarget을 중심으로 시전되므로
            return MapManager.Instance.ConvertToGridPosition(op.transform.position);
        }
    }
}

