using UnityEngine;
using System.Collections.Generic;

namespace Skills.Base
{
    public abstract class UnitSkill<Tcaster>: ScriptableObject where Tcaster: UnitEntity 
    {
        [Header("Unit Skill Properties")]
        [SerializeField] protected string skillName = string.Empty;
        public string SkillName => skillName;

        // 스킬 시작 시에 적용되는 효과
        public virtual void OnSkillActivated(Tcaster caster)
        {
            // 예: 버프 적용, 공격력 증가, 특수 공격 등
        }

        // 매 프레임 호출되어서 지속 효과를 구현
        public virtual void OnUpdate(Tcaster caster)
        {
            // 예 : 지속 힐, 지속 대미지 등
            // 내 경우는 장판을 따로 구현해서 상관은 없을 듯?
        }

        // 스킬 종료 시 정리 작업
        public virtual void OnSkillEnd(Tcaster caster)
        {
            // 예 : 버프 해제, 상태 복원 등
        }

        // 스킬 사용 가능한지를 판단
        public virtual bool CanActivate(Tcaster caster) { return true; }

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