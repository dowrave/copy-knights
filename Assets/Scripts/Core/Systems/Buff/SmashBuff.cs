public class SmashBuff : Buff
{
    private float damageMultiplier;
    private AttackType attackType;
    private bool removeBuffAfterAttack;

    public SmashBuff(float damageMultiplier, AttackType attackType, bool removeBuffAfterAttack)
    {
        buffName = "Smash";
        this.damageMultiplier = damageMultiplier;
        this.attackType = attackType;
        this.removeBuffAfterAttack = removeBuffAfterAttack;
    }

    // 공격에 묻어나가는 로직
    public override void OnBeforeAttack(UnitEntity owner, ref float damage, ref AttackType attackType, ref bool showDamagePopup)
    {
        damage *= damageMultiplier;
        if (attackType != AttackType.None)
        {
            attackType = this.attackType;
        }
        showDamagePopup = true;
    }

    public override void OnAfterAttack(UnitEntity owner, UnitEntity target)
    {
        if (owner is Operator op)
        {
            if (removeBuffAfterAttack)
            {
                op.SetCurrentSP(0f);
                op.RemoveBuff(this);
            }
        }
    }
}