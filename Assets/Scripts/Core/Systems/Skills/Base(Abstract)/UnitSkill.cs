using UnityEngine;
using System.Collections.Generic;

namespace Skills.Base
{
    public abstract class UnitSkill : ScriptableObject
    {
        [Header("Unit Skill Properties")]
        [SerializeField] protected string skillName = string.Empty;
        public string SkillName => skillName;

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
        public virtual void PreloadObjectPools() { }

        protected virtual GameObject PlayVFX(UnitEntity caster, string vfxTag, Vector3 pos, Quaternion rot, float duration = 2f)
        {
            GameObject obj = ObjectPoolManager.Instance!.SpawnFromPool(vfxTag, pos, rot);
            if (obj != null)
            {
                SelfReturnVFXController ps = obj.GetComponent<SelfReturnVFXController>();
                if (ps != null)
                {
                    ps.Initialize(duration, caster);
                }

                return obj;
            }
            else
            {
                return null;
            }
        }

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

        /// <summary>
        /// 범위가 있는 스킬의 중심 위치. 기본적으로 시전자의 위치로 설정되어 있습니다.
        /// </summary>
        protected virtual Vector2Int GetCenterGridPos(UnitEntity caster)
        {
            return MapManager.Instance.ConvertToGridPosition(caster.transform.position);
        }
    }
}