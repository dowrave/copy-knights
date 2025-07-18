using UnityEngine;
using Skills.Base;
using UnityEngine.VFX;

namespace Skills.OperatorSkills
{
    [CreateAssetMenu(fileName = "New Shield Skill", menuName = "Skills/Shield Skill")]
    public class ShieldSkill : ActiveSkill
    {
        [Header("Shield Settings")]
        [SerializeField] private float shieldAmount = 500f;

        [Header("Stat Boost Settings")]
        [SerializeField] private StatModifierSkill.StatModifiers statMods;

        [Header("Shield Visual Effects")]
        [SerializeField] private GameObject shieldEffectPrefab = default!;

        private ShieldBuff? _shieldBuff;
        private StatModificationBuff _statBuff;

        protected override void PlaySkillEffect(Operator op)
        {
            _statBuff = new StatModificationBuff(this.duration, statMods);
            op.AddBuff(_statBuff);

            _shieldBuff = new ShieldBuff(shieldAmount, this.duration, shieldEffectPrefab);
            op.AddBuff(_shieldBuff);

            // ���� ���尡 �շ��� �� ���� ������ �ʱ�ȭ�Ǳ� ���Ѵٸ�
            // _shieldBuff.LinkBuff(_statBuff);
            // _shieldBuff.OnRemovedCallback += () => OnSkillEnd(op);
        }

        protected override void OnSkillEnd(Operator op)
        {
            // ���ӽð��� �� �Ǿ ������ ��� - ����� �������� ������
            if (_statBuff != null) op.RemoveBuff(_statBuff);
            if (_shieldBuff != null) op.RemoveBuff(_shieldBuff);
            _statBuff = null;
            _shieldBuff = null;

            base.OnSkillEnd(op);
        }

    }
}