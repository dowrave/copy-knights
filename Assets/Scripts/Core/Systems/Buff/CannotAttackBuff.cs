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

    // PerformChangedAction이 부모 클래스에서 비어 있기 때문에 별도로 구현하지 않음
    // 공격이 나가지 않으면 ㅇㅋ
}