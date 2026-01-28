using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EnemySkillController: IEnemySkillReadOnly
{
    private EnemyBoss _owner;

    private List<Operator> _operatorsInSkillRange = new List<Operator>();
    private float currentGlobalCooldown; // 공통 쿨다운 : 스킬이 2개 있는데, 두 스킬을 연속적으로 쓰는 걸 방지하기 위함
    private Dictionary<EnemyBossSkill, float> skillCooldowns = new Dictionary<EnemyBossSkill, float>(); // 스킬과 각각의 쿨다운

    private List<EnemyBossSkill> meleeSkills = new List<EnemyBossSkill>();
    private List<EnemyBossSkill> rangedSkills = new List<EnemyBossSkill>();

    private float _globalSkillCooldownDuration = 5f;
    private float _currentGlobalCooldown;

    public IReadOnlyList<Operator> OperatorsInSkillRange => _operatorsInSkillRange;
    public float CurrentGlobalCooldown => _currentGlobalCooldown; 

    public EnemySkillController(EnemyBoss owner)
    {
        _owner = owner;
    }

    public void Initialize()
    {
        // 스킬들을 리스트에 넣음
        
        foreach (EnemyBossSkill skill in _owner.BossData.MeleeSkills)
        {
            if (skill.SkillType == EnemyBossSkillType.Melee)
            {
                meleeSkills.Add(skill);
                skillCooldowns.Add(skill, 0);
            }
            else
            {
                Logger.LogError("Melee 스킬이 아닌 스킬이 들어가 있음!!!!");
            }
        }
        foreach (EnemyBossSkill skill in _owner.BossData.RangedSkills)
        {
            if (skill.SkillType == EnemyBossSkillType.Ranged)
            {
                rangedSkills.Add(skill);
                skillCooldowns.Add(skill, 0);
            }
            else
            {
                Logger.LogError("Ranged 스킬이 아닌 스킬이 들어가 있음!!!!");
            }
        }
    }

    // 행동 제약에 관계없이 업데이트되어야 하는 요소들
    public void UpdateAllCooldowns()
    {
        UpdateSkillCooldowns();
    }

    protected void UpdateSkillCooldowns()
    {
        // 공격 쿨다운 관련 로직
        // base.UpdateAllCooldowns();

        if (currentGlobalCooldown > 0)
        {
            currentGlobalCooldown -= Time.deltaTime;
        }

        // 각 스킬의 쿨다운 업데이트
        var skillKeys = new List<EnemyBossSkill>(skillCooldowns.Keys);
        foreach (var skill in skillKeys)
        {
            if (skillCooldowns[skill] > 0)
            {
                skillCooldowns[skill] -= Time.deltaTime;
            }
        }
    }

    // 스킬을 썼으면 true, 쓰지 않았으면 false 반환
    public bool OnUpdate()
    {
        return TryUseSkill();
    }

    // 스킬을 썼으면 true, 쓰지 않았으면 false
    protected bool TryUseSkill()
    {
        // 공통 쿨다운일 때는 실행되지 않음
        if (currentGlobalCooldown > 0f) return false;

        // 저지를 당할 때는 근거리 스킬 중에서 설정, 아니라면 원거리 중에서 설정
        // 이 경우는 참조 데이터를 "읽기"만 하므로 별도의 리스트를 사용할 필요는 없다.
        List<EnemyBossSkill> skillsToCheck = (_owner.BlockingOperator != null) ? meleeSkills : rangedSkills;

        // 스킬에 넣은 스킬 순서대로 돌아감
        foreach (EnemyBossSkill bossSkill in skillsToCheck)
        {
            // 원거리부터 체크 - 사거리 내에 가장 나중에 배치된 오퍼레이터
            // 근거리 -  자신을 저지 중인 오퍼레이터
            Operator mainTarget = skillsToCheck == rangedSkills ?
                _operatorsInSkillRange.OrderByDescending(op => op.DeploymentOrder).FirstOrDefault() : 
                _owner.BlockingOperator; 

            if (skillCooldowns[bossSkill] <= 0 && bossSkill.CanActivate(_owner))
            {
                bossSkill.Activate(_owner, mainTarget);

                // 쿨다운 설정
                currentGlobalCooldown = _globalSkillCooldownDuration;
                skillCooldowns[bossSkill] = bossSkill.CoolTime;

                return true;
            }
        }

        return false;
    }

    #region Skill Range Collider

    public void OnTargetEnteredSkillRange(DeployableUnitEntity target)
    {
        if (target is Operator op && op.IsDeployed)
        {
            _operatorsInSkillRange.Add(op);
        }
    }

    public void OnTargetExitedSkillRange(DeployableUnitEntity target)
    {
        if (target is Operator op && op.IsDeployed)
        {
            _operatorsInSkillRange.Remove(op);
        }
    }

    #endregion
}