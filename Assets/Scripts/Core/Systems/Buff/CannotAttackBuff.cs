using Skills.Base;

public class CannotAttackBuff : Buff
{
    public CannotAttackBuff(float duration, OperatorSkill OperatorSkill)
    {
        this.buffName = "Cannot Attack";
        SourceSkill = OperatorSkill;
        this.duration = duration;
    }

    public override bool ModifiesAttackAction => true;

    public override void PerformChangedAttackAction(UnitEntity owner)
    {
        // 공격 쿨타임만 돌리고 실질적으로 아무런 기능을 하지 않음을 표시함
        // Buff와 똑같기 때문에 굳이 넣어도 되지 않지만, 가독성을 위해 넣음
        // 쿨타임을 돌리는 이유는 계속된 호출을 방지하기 위함
        base.PerformChangedAttackAction(owner);
    }
}