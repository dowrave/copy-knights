using UnityEngine;
using System.Collections;

public class DoubleShotBuff : Buff
{
    public override bool ModifiesAttackAction => true;
    private float damageMultiplier;
    private float delayBetweenShots;

    public DoubleShotBuff(float delayBetweenShots, float damageMultiplier)
    {
        this.buffName = "Double Shot";
        duration = float.PositiveInfinity; // 스킬에 의해 켜고 꺼짐
        this.damageMultiplier = damageMultiplier;
        this.delayBetweenShots = delayBetweenShots;
    }

    public override void PerformChangedAction(UnitEntity owner, UnitEntity target)
    {
        owner.StartCoroutine(PerformDoubleAttack(owner));
    }

    private IEnumerator PerformDoubleAttack(UnitEntity owner)
    {
        if (owner is Operator op)
        {
            UnitEntity? target = op.CurrentTarget;
            if (target == null) yield break;

            float modifiedDamage = op.GetStat(StatType.AttackPower) * damageMultiplier;

            op.PerformActualAction(target, modifiedDamage);
            yield return new WaitForSeconds(delayBetweenShots);

            if (target != null && target.CurrentHealth >= 0)
            {
                op.PerformActualAction(target, modifiedDamage);
            }
        }
    }
}