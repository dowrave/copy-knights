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
        [SerializeField] private GameObject shieldVFXPrefab = default!;

        private ShieldBuff? _shieldBuff;
        private StatModificationBuff _statBuff;

        protected override void PlaySkillEffect(Operator op)
        {
            _statBuff = new StatModificationBuff(this.duration, statMods);
            op.AddBuff(_statBuff);

            _shieldBuff = new ShieldBuff(shieldAmount, this.duration);
            op.AddBuff(_shieldBuff);

            // 스탯 버프 VFX는 ActiveSkill의 로직에 의해 동작함

            // Shield VFX 실행
            GameObject shieldVFXObject = ObjectPoolManager.Instance.SpawnFromPool(GetShieldVFXTag(op.OperatorData), op.transform.position, Quaternion.identity);
            shieldVFXObject.transform.SetParent(op.gameObject.transform);
            ShieldVFXController shieldVFX = shieldVFXObject.GetComponent<ShieldVFXController>();
            if (shieldVFX != null)
            {
                Debug.Log("[ShieldSkill]스킬 시전 이펙트 시작");
                shieldVFX.Initialize(duration, op);
            }

            // 만약 쉴드가 뚫렸을 때 스탯 버프도 초기화되길 원한다면
            // _shieldBuff.LinkBuff(_statBuff);
            // _shieldBuff.OnRemovedCallback += () => OnSkillEnd(op);
        }

        protected override void OnSkillEnd(Operator op)
        {
            // 지속시간이 다 되어서 끝나는 경우 - 적용된 버프들을 제거함
            if (_statBuff != null) op.RemoveBuff(_statBuff);
            if (_shieldBuff != null) op.RemoveBuff(_shieldBuff);
            _statBuff = null;
            _shieldBuff = null;

            base.OnSkillEnd(op);
        }

        public override void PreloadObjectPools(OperatorData ownerData)
        {
            base.PreloadObjectPools(ownerData);

            if (shieldVFXPrefab != null)
            {
                ObjectPoolManager.Instance.CreatePool(GetShieldVFXTag(ownerData), shieldVFXPrefab, 1);
            }
        }

        public string GetShieldVFXTag(OperatorData ownerData) => $"{ownerData.entityName}_{skillName}_Shield";
    }
}