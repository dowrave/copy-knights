using UnityEngine;
using Skills.Base;
using System.Collections;
using System.Collections.Generic;


namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Buff Skill", menuName = "Skills/Buff Skill")]
    public class BuffSkill: Skill
    {
        [System.Serializable] 
        public class BuffEffect
        {
            public float HealthModifier = 1f;
            public float AttackPowerModifier = 1f;
            public float AttackSpeedModifier = 1f;
            public float DefenseModifier = 1f;
            public float MagicResistanceModifier = 1f;
            public int? ChangedBlockableEnemies = null;
            public Vector2Int[] ChangedAttackableTiles; // 설정되지 않으면 원본 공격 범위를 그대로 이용함
        }

        public float duration = 10f;
        public BuffEffect BuffEffects;
        public GameObject BuffVisualEffectPrefab;
        //public Color SPBarColor = Color.yellow;

        public override void Activate(Operator op)
        {
            op.StartCoroutine(ApplyBuff(op));
        }


        private IEnumerator ApplyBuff(Operator op)
        {
            Debug.Log($"{Name} 스킬이 발동됨");

            // 원래 스탯 저장
            float originalCurrentHealth = op.CurrentHealth;
            float originalMaxHealth = op.MaxHealth;
            float originalAttackPower = op.AttackPower;
            float origianlAttackSpeed = op.AttackSpeed; 
            float originalDefense = op.currentStats.Defense;
            float originalMagicResistance = op.currentStats.MagicResistance;
            int originalBlockableEnemies = op.currentStats.MaxBlockableEnemies;
            Vector2Int[] originalAttackableTiles = op.CurrentAttackbleTiles.Clone() as Vector2Int[];

            // 버프 적용
            op.CurrentHealth *= BuffEffects.HealthModifier;
            op.MaxHealth *= BuffEffects.HealthModifier;
            op.AttackPower *= BuffEffects.AttackPowerModifier;
            op.currentStats.Defense *= BuffEffects.DefenseModifier;
            op.currentStats.MagicResistance *= BuffEffects.MagicResistanceModifier;

            // 공격 범위 변화
            if (BuffEffects.ChangedAttackableTiles != null && BuffEffects.ChangedAttackableTiles.Length > 0)
            {
                ChangeAttackRange(op);
            }

           // 저지 수 변화
           if (BuffEffects.ChangedBlockableEnemies.HasValue) // HasValue : nullable 타입이 값을 갖고 있는지 확인
            {
                op.MaxBlockableEnemies = BuffEffects.ChangedBlockableEnemies.Value; // nullable 타입이 가진 실제 값을 반환. 반드시 HasValue 체크가 선행되어야 함
            }

            // 버프 이펙트 생성
            GameObject buffEffect = null;
            if (BuffVisualEffectPrefab != null)
            {
                buffEffect = Instantiate(BuffVisualEffectPrefab, op.transform.position, Quaternion.identity);
                buffEffect.transform.SetParent(op.transform);
            }

            // SP Bar 색 변경 - 이 부분은 BarUI에 구현하겠음
            //DeployableBarUI barUI = op.GetComponentInChildren<DeployableBarUI>();

            // 버프 지속 시간
            yield return new WaitForSeconds(duration);

            // 버프 해제
            op.CurrentHealth = originalCurrentHealth;
            op.MaxHealth = originalMaxHealth;
            op.AttackPower = originalCurrentHealth;
            op.currentStats.Defense = originalDefense;
            op.currentStats.MagicResistance = originalMagicResistance;
            op.CurrentAttackbleTiles = originalAttackableTiles;
            op.MaxBlockableEnemies = originalBlockableEnemies;
            

            // 버프 이펙트 제거
            if (buffEffect != null)
            {
                Destroy(buffEffect);
            }
        }

        /// <summary>
        /// Operator의 방향전환을 고려해서 공격 범위 설정
        /// </summary>
        private void ChangeAttackRange(Operator op)
        {
            List<Vector2Int> newAttackbleTiles = new List<Vector2Int>(op.CurrentAttackbleTiles);

            foreach (Vector2Int additionalTile in BuffEffects.ChangedAttackableTiles)
            {
                Vector2Int rotatedTile = op.RotateOffset(additionalTile, op.FacingDirection);
                if (!newAttackbleTiles.Contains(rotatedTile))
                {
                    newAttackbleTiles.Add(rotatedTile);
                }
            }

            op.CurrentAttackbleTiles = newAttackbleTiles.ToArray();
        }
    }
}