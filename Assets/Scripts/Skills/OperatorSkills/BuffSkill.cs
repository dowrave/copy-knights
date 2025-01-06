using UnityEngine;
using Skills.Base;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.VFX;


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

        // 값 임시 저장 필드
        private float originalMaxHealth;
        private float originalAttackPower;
        private float originalAttackSpeed;
        private float originalDefense;
        private float originalMagicResistance;
        int originalBlockableEnemies;
        List<Vector2Int> originalAttackableTiles;
        protected VisualEffect buffVFX;
        protected GameObject buffEffect;

        public override void Activate(Operator op)
        {
            ApplyBuff(op);
        }


        private void ApplyBuff(Operator op)
        {
            // 원래 스탯 저장
            //float originalCurrentHealth = op.CurrentHealth; 
            originalMaxHealth = op.MaxHealth;
            originalAttackPower = op.AttackPower;
            originalAttackSpeed = op.AttackSpeed; 
            originalDefense = op.currentStats.Defense;
            originalMagicResistance = op.currentStats.MagicResistance;
            originalBlockableEnemies = op.currentStats.MaxBlockableEnemies;
            originalAttackableTiles = new List<Vector2Int>(op.CurrentAttackbleTiles);

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
           if (Modifiers.ChangedBlockableEnemies.HasValue)
            {
                op.MaxBlockableEnemies = Modifiers.ChangedBlockableEnemies.Value; 
            }

            // 버프 이펙트 생성
            if (BuffEffectPrefab != null)
            {
                Vector3 buffEffectPosition = new Vector3(op.transform.position.x, 0.05f, op.transform.position.z);
                buffEffect = Instantiate(BuffEffectPrefab, buffEffectPosition, Quaternion.identity);
                buffEffect.transform.SetParent(op.transform);

                // VFX 컴포넌트 가져오기
                buffVFX = buffEffect.GetComponent<VisualEffect>();
                if (buffVFX != null)
                {
                    buffVFX.Play();
                }
            }

            op.StartCoroutine(HandleSkillDuration(op, duration));

            // 버프 해제
            OnSkillEnd(op);
        }

        /// <summary>
        /// Operator의 방향전환을 고려해서 공격 범위 설정
        /// </summary>
        private void ChangeAttackRange(Operator op)
        {
            List<Vector2Int> newAttackbleTiles = new List<Vector2Int>(op.CurrentAttackbleTiles);

            foreach (Vector2Int additionalTile in Modifiers.ChangedAttackableTiles)
            {
                Vector2Int rotatedTile = DirectionSystem.RotateGridOffset(additionalTile, op.FacingDirection);
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

        protected override void OnSkillEnd(Operator op)
        {
            base.OnSkillEnd(op);

            // 1. 해제 직전의 현재 체력 > 최대 체력이라면 현재 체력은 원래의 최대 체력값이 됨
            if (op.CurrentHealth > originalMaxHealth)
            {
                op.CurrentHealth = originalMaxHealth;
            }

            // 2. 해제 직전의 현재 체력 <= 최대 체력이면 그대로 유지
            op.MaxHealth = originalMaxHealth;
            op.AttackPower = originalAttackPower;
            op.AttackSpeed = originalAttackSpeed;
            op.currentStats.Defense = originalDefense;
            op.currentStats.MagicResistance = originalMagicResistance;
            op.CurrentAttackbleTiles = originalAttackableTiles;
            op.MaxBlockableEnemies = originalBlockableEnemies;

            // 버프 이펙트 제거
            if (buffEffect != null)
            {
                if (buffVFX != null)
                {
                    buffVFX.Stop(); // VFX 재생 중지
                }
                Destroy(buffEffect);
            }

            // SP Bar 복구
            //op.EndSkillDurationDisplay();
        }
    }
}