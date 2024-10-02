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
            public Vector2Int[] ChangedAttackableTiles; // �������� ������ ���� ���� ������ �״�� �̿���
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
            Debug.Log($"{Name} ��ų�� �ߵ���");

            // ���� ���� ����
            float originalCurrentHealth = op.CurrentHealth;
            float originalMaxHealth = op.MaxHealth;
            float originalAttackPower = op.AttackPower;
            float origianlAttackSpeed = op.AttackSpeed; 
            float originalDefense = op.currentStats.Defense;
            float originalMagicResistance = op.currentStats.MagicResistance;
            int originalBlockableEnemies = op.currentStats.MaxBlockableEnemies;
            Vector2Int[] originalAttackableTiles = op.CurrentAttackbleTiles.Clone() as Vector2Int[];

            // ���� ����
            op.CurrentHealth *= BuffEffects.HealthModifier;
            op.MaxHealth *= BuffEffects.HealthModifier;
            op.AttackPower *= BuffEffects.AttackPowerModifier;
            op.currentStats.Defense *= BuffEffects.DefenseModifier;
            op.currentStats.MagicResistance *= BuffEffects.MagicResistanceModifier;

            // ���� ���� ��ȭ
            if (BuffEffects.ChangedAttackableTiles != null && BuffEffects.ChangedAttackableTiles.Length > 0)
            {
                ChangeAttackRange(op);
            }

           // ���� �� ��ȭ
           if (BuffEffects.ChangedBlockableEnemies.HasValue) // HasValue : nullable Ÿ���� ���� ���� �ִ��� Ȯ��
            {
                op.MaxBlockableEnemies = BuffEffects.ChangedBlockableEnemies.Value; // nullable Ÿ���� ���� ���� ���� ��ȯ. �ݵ�� HasValue üũ�� ����Ǿ�� ��
            }

            // ���� ����Ʈ ����
            GameObject buffEffect = null;
            if (BuffVisualEffectPrefab != null)
            {
                buffEffect = Instantiate(BuffVisualEffectPrefab, op.transform.position, Quaternion.identity);
                buffEffect.transform.SetParent(op.transform);
            }

            // SP Bar �� ���� - �� �κ��� BarUI�� �����ϰ���
            //DeployableBarUI barUI = op.GetComponentInChildren<DeployableBarUI>();

            // ���� ���� �ð�
            yield return new WaitForSeconds(duration);

            // ���� ����
            op.CurrentHealth = originalCurrentHealth;
            op.MaxHealth = originalMaxHealth;
            op.AttackPower = originalCurrentHealth;
            op.currentStats.Defense = originalDefense;
            op.currentStats.MagicResistance = originalMagicResistance;
            op.CurrentAttackbleTiles = originalAttackableTiles;
            op.MaxBlockableEnemies = originalBlockableEnemies;
            

            // ���� ����Ʈ ����
            if (buffEffect != null)
            {
                Destroy(buffEffect);
            }
        }

        /// <summary>
        /// Operator�� ������ȯ�� ����ؼ� ���� ���� ����
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