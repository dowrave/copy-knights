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
            public Vector2Int[] ChangedAttackableTiles; // 설정되지 않으면 원본 공격 범위를 그대로 이용함
        }

        public float duration = 10f;
        public BuffEffect BuffEffects;
        public GameObject BuffVisualEffectPrefab;
        //public Color SPBarColor = Color.yellow;

        public override void Activate(Operator @operator)
        {
            @operator.StartCoroutine(ApplyBuff(@operator));
        }


        private IEnumerator ApplyBuff(Operator @operator)
        {
            Debug.Log($"{Name} 스킬이 발동됨");

            // 원래 스탯 저장
            float originalCurrentHealth = @operator.CurrentHealth;
            float originalMaxHealth = @operator.MaxHealth;
            float originalAttackPower = @operator.AttackPower;
            float origianlAttackSpeed = @operator.AttackSpeed; 
            float originalDefense = @operator.currentStats.Defense;
            float originalMagicResistance = @operator.currentStats.MagicResistance;
            Vector2Int[] originalAttackableTiles = @operator.CurrentAttackbleTiles.Clone() as Vector2Int[];

            // 버프 적용
            @operator.CurrentHealth *= BuffEffects.HealthModifier;
            @operator.MaxHealth *= BuffEffects.HealthModifier;
            @operator.AttackPower *= BuffEffects.AttackPowerModifier;
            @operator.currentStats.Defense *= BuffEffects.DefenseModifier;
            @operator.currentStats.MagicResistance *= BuffEffects.MagicResistanceModifier;

            // 공격 범위 변화
            if (BuffEffects.ChangedAttackableTiles != null && BuffEffects.ChangedAttackableTiles.Length > 0)
            {
                ChangeAttackRange(@operator);
            }

            // 버프 이펙트 생성
            GameObject buffEffect = null;
            if (BuffVisualEffectPrefab != null)
            {
                buffEffect = Instantiate(BuffVisualEffectPrefab, @operator.transform.position, Quaternion.identity);
                buffEffect.transform.SetParent(@operator.transform);
            }

            // SP Bar 색 변경 - 이 부분은 BarUI에 구현하겠음
            //DeployableBarUI barUI = @operator.GetComponentInChildren<DeployableBarUI>();

            // 버프 지속 시간
            yield return new WaitForSeconds(duration);

            // 버프 해제
            @operator.CurrentHealth = originalCurrentHealth;
            @operator.MaxHealth = originalMaxHealth;
            @operator.AttackPower = originalCurrentHealth;
            @operator.currentStats.Defense = originalDefense;
            @operator.currentStats.MagicResistance = originalMagicResistance;
            @operator.CurrentAttackbleTiles = originalAttackableTiles; 
            

            // 버프 이펙트 제거
            if (buffEffect != null)
            {
                Destroy(buffEffect);
            }
        }

        /// <summary>
        /// Operator의 방향전환을 고려해서 공격 범위 설정
        /// </summary>
        private void ChangeAttackRange(Operator @operator)
        {
            List<Vector2Int> newAttackbleTiles = new List<Vector2Int>(@operator.CurrentAttackbleTiles);

            foreach (Vector2Int additionalTile in BuffEffects.ChangedAttackableTiles)
            {
                Vector2Int rotatedTile = @operator.RotateOffset(additionalTile, @operator.FacingDirection);
                if (!newAttackbleTiles.Contains(rotatedTile))
                {
                    newAttackbleTiles.Add(rotatedTile);
                }
            }

            @operator.CurrentAttackbleTiles = newAttackbleTiles.ToArray();
        }
    }
}