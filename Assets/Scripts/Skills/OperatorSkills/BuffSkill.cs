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
            public Vector2Int[] ChangedAttackableTiles; // �������� ������ ���� ���� ������ �״�� �̿���
        }

        public float duration = 10f;
        public BuffModifiers Modifiers;
        public GameObject BuffEffectPrefab;

        // �� �ӽ� ���� �ʵ�
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
            // ���� ���� ����
            //float originalCurrentHealth = op.CurrentHealth; 
            originalMaxHealth = op.MaxHealth;
            originalAttackPower = op.AttackPower;
            originalAttackSpeed = op.AttackSpeed; 
            originalDefense = op.currentStats.Defense;
            originalMagicResistance = op.currentStats.MagicResistance;
            originalBlockableEnemies = op.currentStats.MaxBlockableEnemies;
            originalAttackableTiles = new List<Vector2Int>(op.CurrentAttackbleTiles);

            // ���� ����
            op.CurrentHealth *= Modifiers.HealthModifier;
            op.MaxHealth *= Modifiers.HealthModifier;
            op.AttackPower *= Modifiers.AttackPowerModifier;
            op.currentStats.Defense *= Modifiers.DefenseModifier;
            op.currentStats.MagicResistance *= Modifiers.MagicResistanceModifier;

            // ���� ���� ��ȭ
            if (Modifiers.ChangedAttackableTiles != null && Modifiers.ChangedAttackableTiles.Length > 0)
            {
                ChangeAttackRange(op);
            }

           // ���� �� ��ȭ
           if (Modifiers.ChangedBlockableEnemies.HasValue)
            {
                op.MaxBlockableEnemies = Modifiers.ChangedBlockableEnemies.Value; 
            }

            // ���� ����Ʈ ����
            if (BuffEffectPrefab != null)
            {
                Vector3 buffEffectPosition = new Vector3(op.transform.position.x, 0.05f, op.transform.position.z);
                buffEffect = Instantiate(BuffEffectPrefab, buffEffectPosition, Quaternion.identity);
                buffEffect.transform.SetParent(op.transform);

                // VFX ������Ʈ ��������
                buffVFX = buffEffect.GetComponent<VisualEffect>();
                if (buffVFX != null)
                {
                    buffVFX.Play();
                }
            }

            op.StartCoroutine(HandleSkillDuration(op, duration));

            // ���� ����
            OnSkillEnd(op);
        }

        /// <summary>
        /// Operator�� ������ȯ�� ����ؼ� ���� ���� ����
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
            // ���۷����� ��ġ���� ��¦ �ڷ� ������
            Vector3 effectPosition = op.transform.position - op.transform.forward * 0.5f + Vector3.up * 0.5f;

            GameObject buffEffect = Instantiate(BuffEffectPrefab, effectPosition, Quaternion.identity);
            buffEffect.transform.SetParent(op.transform);

            // ī�޶� ���� ȸ��
            buffEffect.transform.LookAt(buffEffect.transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);

            return buffEffect;
        }

        protected override void OnSkillEnd(Operator op)
        {
            base.OnSkillEnd(op);

            // 1. ���� ������ ���� ü�� > �ִ� ü���̶�� ���� ü���� ������ �ִ� ü�°��� ��
            if (op.CurrentHealth > originalMaxHealth)
            {
                op.CurrentHealth = originalMaxHealth;
            }

            // 2. ���� ������ ���� ü�� <= �ִ� ü���̸� �״�� ����
            op.MaxHealth = originalMaxHealth;
            op.AttackPower = originalAttackPower;
            op.AttackSpeed = originalAttackSpeed;
            op.currentStats.Defense = originalDefense;
            op.currentStats.MagicResistance = originalMagicResistance;
            op.CurrentAttackbleTiles = originalAttackableTiles;
            op.MaxBlockableEnemies = originalBlockableEnemies;

            // ���� ����Ʈ ����
            if (buffEffect != null)
            {
                if (buffVFX != null)
                {
                    buffVFX.Stop(); // VFX ��� ����
                }
                Destroy(buffEffect);
            }

            // SP Bar ����
            //op.EndSkillDurationDisplay();
        }
    }
}