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
            public Vector2Int[] ChangedAttackableTiles; // �������� ������ ���� ���� ������ �״�� �̿���
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
            // ���� ���� ����
            //float originalCurrentHealth = op.CurrentHealth; 
            float originalMaxHealth = op.MaxHealth;
            float originalAttackPower = op.AttackPower;
            float origianlAttackSpeed = op.AttackSpeed; 
            float originalDefense = op.currentStats.Defense;
            float originalMagicResistance = op.currentStats.MagicResistance;
            int originalBlockableEnemies = op.currentStats.MaxBlockableEnemies;
            List<Vector2Int> originalAttackableTiles = new List<Vector2Int>(op.CurrentAttackbleTiles);

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
           if (Modifiers.ChangedBlockableEnemies.HasValue) // HasValue : nullable Ÿ���� ���� ���� �ִ��� Ȯ��
            {
                op.MaxBlockableEnemies = Modifiers.ChangedBlockableEnemies.Value; // nullable Ÿ���� ���� ���� ���� ��ȯ. �ݵ�� HasValue üũ�� ����Ǿ�� ��
            }

            // ���� ����Ʈ ����
            GameObject buffEffect = null;
            if (BuffEffectPrefab != null)
            {
                // 
                Vector3 buffEffectPosition = new Vector3(op.transform.position.x, 0.05f, op.transform.position.z);
                buffEffect = Instantiate(BuffEffectPrefab, buffEffectPosition, Quaternion.identity);
                buffEffect.transform.SetParent(op.transform);
            }

            // SP Bar �� ����
            op.StartSkillDurationDisplay(duration);

            // ���� ���� �ð�
            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                yield return null;
                elapsedTime += Time.deltaTime;
                op.UpdateSkillDurationDisplay(1 - (elapsedTime / duration));
            }

            // ���� ����

            // 1. ���� ������ ���� ü�� > �ִ� ü���̶�� ���� ü���� ������ �ִ� ü�°��� ��
            if (op.CurrentHealth > originalMaxHealth) 
            {
                op.CurrentHealth = originalMaxHealth; 
            }
            // 2. ���� ������ ���� ü�� <= �ִ� ü���̸� �״�� ����

            op.MaxHealth = originalMaxHealth;
            op.AttackPower = originalAttackPower;
            op.currentStats.Defense = originalDefense;
            op.currentStats.MagicResistance = originalMagicResistance;
            op.CurrentAttackbleTiles = originalAttackableTiles;
            op.MaxBlockableEnemies = originalBlockableEnemies;

            // ���� ����Ʈ ����
            if (buffEffect != null)
            {
                Destroy(buffEffect);
            }

            // SP Bar ����
            op.EndSkillDurationDisplay();
        }

        /// <summary>
        /// Operator�� ������ȯ�� ����ؼ� ���� ���� ����
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
            // ���۷����� ��ġ���� ��¦ �ڷ� ������
            Vector3 effectPosition = op.transform.position - op.transform.forward * 0.5f + Vector3.up * 0.5f;

            GameObject buffEffect = Instantiate(BuffEffectPrefab, effectPosition, Quaternion.identity);
            buffEffect.transform.SetParent(op.transform);

            // ī�޶� ���� ȸ��
            buffEffect.transform.LookAt(buffEffect.transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);

            return buffEffect;
        }
    }
}