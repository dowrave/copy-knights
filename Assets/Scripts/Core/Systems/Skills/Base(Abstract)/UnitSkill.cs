using UnityEngine;

namespace Skills.Base
{
    public abstract class UnitSkill : ScriptableObject
    {
        [Header("Unit Skill Properties")]
        public string skillName = string.Empty;

        protected UnitEntity caster = default!;

        // 스킬 활성화 시에 호출
        public virtual void Activate(UnitEntity caster) { }

        // 스킬 사용 가능한지를 판단
        public virtual bool CanActivate(UnitEntity caster)
        {
            return false;
        }

        // 매 프레임 호출, 스킬 상태를 업데이트함
        public virtual void OnUpdate(UnitEntity caster) { }

        // 시전자가 공격하기 전에 호출
        public virtual void OnBeforeAttack(UnitEntity caster, UnitEntity target, ref float damage, ref AttackType attackType) { }

        // 시전자가 공격한 후에 호출
        public virtual void OnAfterAttack(UnitEntity caster, UnitEntity target) { }

        // RegisterPool할 요소들을 이것저것 넣는 메서드
        public virtual void InitializeSkillObjectPool(UnitEntity caster) { }

        protected string RegisterPool(UnitEntity caster, GameObject prefab, int initialSize = 5)
        {
            if (prefab == null) return string.Empty;

            string poolTag = GetVFXPoolTag(caster, prefab);
            if (!ObjectPoolManager.Instance.IsPoolExist(poolTag))
            {
                ObjectPoolManager.Instance.CreatePool(poolTag, prefab, initialSize);
            }
            return poolTag;
        }

        public virtual string GetVFXPoolTag(UnitEntity caster, GameObject vfxPrefab)
        {
            return string.Empty;
        }
    }
}