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
        public class BuffModifiers
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
        public BuffModifiers Modifiers;
        public GameObject BuffEffectPrefab;
        //public Color SPBarColor = Color.yellow;

        public override void Activate(Operator op)
        {
            op.StartCoroutine(ApplyBuff(op));
        }


        private IEnumerator ApplyBuff(Operator op)
        {
            // 원래 스탯 저장
            //float originalCurrentHealth = op.CurrentHealth; 
            float originalMaxHealth = op.MaxHealth;
            float originalAttackPower = op.AttackPower;
            float origianlAttackSpeed = op.AttackSpeed; 
            float originalDefense = op.currentStats.Defense;
            float originalMagicResistance = op.currentStats.MagicResistance;
            int originalBlockableEnemies = op.currentStats.MaxBlockableEnemies;
            List<Vector2Int> originalAttackableTiles = new List<Vector2Int>(op.CurrentAttackbleTiles);

            // 버프 적용
            op.CurrentHealth *= Modifiers.HealthModifier;
            op.MaxHealth *= Modifiers.HealthModifier;
            op.AttackPower *= Modifiers.AttackPowerModifier;
            op.currentStats.Defense *= Modifiers.DefenseModifier;
            op.currentStats.MagicResistance *= Modifiers.MagicResistanceModifier;

            // 공격 범위 변화
            if (Modifiers.ChangedAttackableTiles != null && Modifiers.ChangedAttackableTiles.Length > 0)
            {
                ChangeAttackRange(op);
            }

           // 저지 수 변화
           if (Modifiers.ChangedBlockableEnemies.HasValue) // HasValue : nullable 타입이 값을 갖고 있는지 확인
            {
                op.MaxBlockableEnemies = Modifiers.ChangedBlockableEnemies.Value; // nullable 타입이 가진 실제 값을 반환. 반드시 HasValue 체크가 선행되어야 함
            }

            // 버프 이펙트 생성
            GameObject buffEffect = null;
            if (BuffEffectPrefab != null)
            {
                // 
                Vector3 buffEffectPosition = new Vector3(op.transform.position.x, 0.05f, op.transform.position.z);
                buffEffect = Instantiate(BuffEffectPrefab, buffEffectPosition, Quaternion.identity);
                buffEffect.transform.SetParent(op.transform);
            }

            // SP Bar 색 변경
            op.StartSkillDurationDisplay(duration);

            // 버프 지속 시간
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                yield return null;
                elapsedTime += Time.deltaTime;
                op.UpdateSkillDurationDisplay(1 - (elapsedTime / duration));
            }

            // 버프 해제

            // 1. 해제 직전의 현재 체력 > 최대 체력이라면 현재 체력은 원래의 최대 체력값이 됨
            if (op.CurrentHealth > originalMaxHealth) 
            {
                op.CurrentHealth = originalMaxHealth; 
            }
            // 2. 해제 직전의 현재 체력 <= 최대 체력이면 그대로 유지

            op.MaxHealth = originalMaxHealth;
            op.AttackPower = originalAttackPower;
            op.currentStats.Defense = originalDefense;
            op.currentStats.MagicResistance = originalMagicResistance;
            op.CurrentAttackbleTiles = originalAttackableTiles;
            op.MaxBlockableEnemies = originalBlockableEnemies;

            // 버프 이펙트 제거
            if (buffEffect != null)
            {
                Destroy(buffEffect);
            }

            // SP Bar 복구
            op.EndSkillDurationDisplay();
        }

        /// <summary>
        /// Operator의 방향전환을 고려해서 공격 범위 설정
        /// </summary>
        private void ChangeAttackRange(Operator op)
        {
            List<Vector2Int> newAttackbleTiles = new List<Vector2Int>(op.CurrentAttackbleTiles);

            foreach (Vector2Int additionalTile in Modifiers.ChangedAttackableTiles)
            {
                Vector2Int rotatedTile = op.RotateOffset(additionalTile, op.FacingDirection);
                if (!newAttackbleTiles.Contains(rotatedTile))
                {
                    newAttackbleTiles.Add(rotatedTile);
                }
            }

            op.CurrentAttackbleTiles = newAttackbleTiles;
        }

        private GameObject CreateBuffVisualEffect(Operator op)
        {
            // 오퍼레이터 위치에서 살짝 뒤로 오프셋
            Vector3 effectPosition = op.transform.position - op.transform.forward * 0.5f + Vector3.up * 0.5f;

            GameObject buffEffect = Instantiate(BuffEffectPrefab, effectPosition, Quaternion.identity);
            buffEffect.transform.SetParent(op.transform);

            // 카메라를 향해 회전
            buffEffect.transform.LookAt(buffEffect.transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);

            return buffEffect;
        }
    }
}