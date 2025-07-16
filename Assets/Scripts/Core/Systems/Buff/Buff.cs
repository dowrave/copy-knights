// 스킬의 부속품이 되는 버프 시스템의 기초
public abstract class Buff
{
    public string buffName;
    public float duration; // 지속 시간. 무한이면 float.PositiveInfinity
    public UnitEntity owner; // 버프 적용 대상
    public UnitEntity caster; // 버프 시전자

    public virtual void OnApply(UnitEntity owner, UnitEntity caster)
    {
        this.owner = owner;
        this.caster = caster;
    }

    public virtual void OnRemove() { } // 버프 제거 시에 호출
    public virtual void OnUpdate() { } // 매 프레임마다 업데이트가 필요하면 호출
    public virtual void OnBeforeAttack(UnitEntity owner, ref float damage, ref AttackType attackType, ref bool showDamagePopup) { } // 공격 전 호출
    public virtual void OnAfterAttack(UnitEntity target) { } // 공격 후 호출

}