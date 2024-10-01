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
            public Vector2Int[] ChangedAttackableTiles; // �������� ������ ���� ���� ������ �״�� �̿���
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
            Debug.Log($"{Name} ��ų�� �ߵ���");

            // ���� ���� ����
            float originalCurrentHealth = @operator.CurrentHealth;
            float originalMaxHealth = @operator.MaxHealth;
            float originalAttackPower = @operator.AttackPower;
            float origianlAttackSpeed = @operator.AttackSpeed; 
            float originalDefense = @operator.currentStats.Defense;
            float originalMagicResistance = @operator.currentStats.MagicResistance;
            Vector2Int[] originalAttackableTiles = @operator.CurrentAttackbleTiles.Clone() as Vector2Int[];

            // ���� ����
            @operator.CurrentHealth *= BuffEffects.HealthModifier;
            @operator.MaxHealth *= BuffEffects.HealthModifier;
            @operator.AttackPower *= BuffEffects.AttackPowerModifier;
            @operator.currentStats.Defense *= BuffEffects.DefenseModifier;
            @operator.currentStats.MagicResistance *= BuffEffects.MagicResistanceModifier;

            // ���� ���� ��ȭ
            if (BuffEffects.ChangedAttackableTiles != null && BuffEffects.ChangedAttackableTiles.Length > 0)
            {
                ChangeAttackRange(@operator);
            }

            // ���� ����Ʈ ����
            GameObject buffEffect = null;
            if (BuffVisualEffectPrefab != null)
            {
                buffEffect = Instantiate(BuffVisualEffectPrefab, @operator.transform.position, Quaternion.identity);
                buffEffect.transform.SetParent(@operator.transform);
            }

            // SP Bar �� ���� - �� �κ��� BarUI�� �����ϰ���
            //DeployableBarUI barUI = @operator.GetComponentInChildren<DeployableBarUI>();

            // ���� ���� �ð�
            yield return new WaitForSeconds(duration);

            // ���� ����
            @operator.CurrentHealth = originalCurrentHealth;
            @operator.MaxHealth = originalMaxHealth;
            @operator.AttackPower = originalCurrentHealth;
            @operator.currentStats.Defense = originalDefense;
            @operator.currentStats.MagicResistance = originalMagicResistance;
            @operator.CurrentAttackbleTiles = originalAttackableTiles; 
            

            // ���� ����Ʈ ����
            if (buffEffect != null)
            {
                Destroy(buffEffect);
            }
        }

        /// <summary>
        /// Operator�� ������ȯ�� ����ؼ� ���� ���� ����
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