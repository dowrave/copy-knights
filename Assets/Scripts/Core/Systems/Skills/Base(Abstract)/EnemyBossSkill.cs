using UnityEngine;
using Skills.Base;

public class EnemyBossSkill : UnitSkill
{
    [Header("Boss Skill Configs")]
    [SerializeField] private float coolTime;
    [SerializeField] private EnemyBossSkillType skillType;

    public EnemyBoss Caster => caster as EnemyBoss;
    public float CoolTime => coolTime;
    public EnemyBossSkillType SkillType => skillType;


    public virtual void Activate(EnemyBoss caster) { }

    public override string GetVFXPoolTag(UnitEntity caster, GameObject vfxPrefab)
    {
        if (vfxPrefab == null)
        {
            Debug.LogError("[EnemyBossSkill.GetVFXPoolTag] vfxPrefab이 null임!!");
            return string.Empty;
        }

        if (caster is Enemy enemy)
        {
            return $"{enemy.BaseData.entityName}_{this.name}_{vfxPrefab.name}";
        }
        else
        {
            Debug.LogError("[EnemyBossSkill.GetVFXPoolTag] caster가 Enemy가 아님!!");
            return string.Empty;
        }
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