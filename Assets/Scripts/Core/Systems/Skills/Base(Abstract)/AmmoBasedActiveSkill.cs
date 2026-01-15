using UnityEngine;
using System.Collections;
using Skills.OperatorSkills;

namespace Skills.Base
{
    // 탄환 수가 제한되어 있고 지속시간이 무한인 스킬의 추상 클래스
    public abstract class AmmoBasedActiveSkill : ActiveSkill
    {
        [Header("Ammo Settings")]
        [SerializeField] protected int maxAmmo;

        [Header("Attack Modifications")]
        [SerializeField] StatModifierSkill.StatModifiers statModifier;

        public override void OnActivated(Operator op)
        {
            // 스탯 변경 버프 생성
            StatModificationBuff statBuff = new StatModificationBuff(float.PositiveInfinity, statModifier);

            // 공격 횟수 세는 버프 생성
            AttackCounterBuff attackCounterBuff = new AttackCounterBuff(maxAmmo);

            // 공격 횟수 세는 버프에 다른 버프 연결
            attackCounterBuff.LinkBuff(statBuff);

            // 공격 횟수 세는 버프 종료 시 실행될 함수 지정
            // OnEnd는 Operator를 받고 콜백은 인자가 없으므로 람다식으로 감싸서 호출한다.
            attackCounterBuff.OnRemovedCallback += () => OnEnd(op);

            // 실제 버프 추가
            op.AddBuff(statBuff);
            op.AddBuff(attackCounterBuff);

            // 지속시간이 무한인 스킬이기 때문에 이런 식으로 구현했음. 만약 있다면 부모 클래스를 따르는 게 낫다.
        } 
    }
}
