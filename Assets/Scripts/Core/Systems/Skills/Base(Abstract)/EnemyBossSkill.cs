using UnityEngine;
using Skills.Base;

public class EnemyBossSkill : UnitSkill
{
    [Header("Boss Skill Configs")]
    [SerializeField] protected float coolTime;
    [SerializeField] protected EnemyBossSkillType skillType;

    public float CoolTime => coolTime;
    public EnemyBossSkillType SkillType => skillType;


    public virtual void Activate(EnemyBoss caster, UnitEntity target) { }

    public override string GetVFXPoolTag(UnitEntity caster, GameObject vfxPrefab)
    {
        if (vfxPrefab == null)
        {
            Logger.LogError("[EnemyBossSkill.GetVFXPoolTag] vfxPrefab이 null임!!");
            return string.Empty;
        }

        if (caster is Enemy enemy)
        {
            return $"{enemy.BaseData.EntityName}_{this.name}_{vfxPrefab.name}";
        }
        else
        {
            Logger.LogError("[EnemyBossSkill.GetVFXPoolTag] caster가 Enemy가 아님!!");
            return string.Empty;
        }
    }

    public virtual void PreloadObjectPools(EnemyBossData ownerData){ }

    public override bool CanActivate(UnitEntity caster)
    {
        if (caster is EnemyBoss boss)
        {
            return boss.OperatorsInSkillRange.Count > 0;
        }
        
        return false;
    }

    public sealed override void Activate(UnitEntity caster)
    {
        if (caster is EnemyBoss boss)
        {
            Activate(boss);
        }
    }
}

public enum EnemyBossSkillType
{
    None,
    Melee,
    Ranged
}