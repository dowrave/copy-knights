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
        [SerializeField] private Vector2 meteorHeights = new Vector2(2f, 3f); // 두 오브젝트의 높이
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
                        // StartCoroutine은 MonoBehaviour에서만 직접 호출이 가능함
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
            // mainTarget을 중심으로 시전되므로
            return MapManager.Instance.ConvertToGridPosition(op.transform.position);
        }
    }
}

