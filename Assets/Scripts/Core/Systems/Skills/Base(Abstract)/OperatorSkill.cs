using System.Collections;
using UnityEngine;

namespace Skills.Base
{
    public abstract class OperatorSkill : UnitSkill<Operator>
    {
        [Header("Operator Base Skill Properties")]
        [TextArea(3, 10)]
        public string description = string.Empty;
        public float spCost = 0f;
        public Sprite skillIcon = default!;

        public bool autoRecover = false; // 활성화시 자동 회복, 비활성화 시 공격 시 회복
        public bool autoActivate = false; // 자동발동 여부

        [Header("테스트 여부 설정 : 테스트 시에 SP = 1")]
        public bool isOnTest = false; // 현재 테스트 여부, 테스트 시 SP = 1로 설정

        public float SPCost => isOnTest ? 1f : spCost; // 테스트 모드일 때 SP 비용을 1로 설정
        
        // 이펙트를 변경해야 하는 경우
        [Header("Attack VFX Overrides(Optional)")]
        public GameObject meleeAttackVFXOverride;

        // 인스펙터 bool 필드값들 초기 설정.
        protected virtual void SetDefaults() { }
        protected void Reset() => SetDefaults();

        // 액티브 스킬을 켰을 때의 동작
        // public virtual void Activate(Operator op) { }

        // modifiesAttackAction가 true일 때, 공격을 변경하는 액션
        public virtual void PerformChangedAttackAction(Operator op) { }

        // 스킬에서 사용되는 오브젝트 풀 생성
        public virtual void PreloadObjectPools(OperatorData ownerData)
        {
            // 근접 공격 VFX 변경
            if (meleeAttackVFXOverride != null)
            {
                ObjectPoolManager.Instance.CreatePool(MeleeAttackVFXTag, meleeAttackVFXOverride, 1);
            }
        }

        protected string _meleeAttackVFXTag;
        public string MeleeAttackVFXTag
        {
            get
            {
                if (string.IsNullOrEmpty(_meleeAttackVFXTag))
                {
                    _meleeAttackVFXTag = $"{skillName}_MeleeAttackVFX";
                }
                return _meleeAttackVFXTag;
            }
        }



        // UnitSkill로 타입이 오더라도 안전하게 실행시키기 위한 메서드들
        // sealed는 하위 클래스의 오버라이드를 방지함 
        // public sealed override void Activate(UnitEntity caster)
        // {
        //     if (caster is Operator op)
        //     {
        //         Activate(op);
        //     }
        // }
    }

    }