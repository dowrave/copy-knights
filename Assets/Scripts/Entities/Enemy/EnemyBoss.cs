using UnityEngine;
using System.Collections.Generic;
using System;

public class EnemyBoss : Enemy
{
    [NonSerialized] protected new EnemyData enemyData; // 부모의 enemyData와 충돌 방지, 이 필드는 직렬화에서 무시됨
    [SerializeField] EnemyBossData bossData = default!;

    public override EnemyData BaseData => bossData;
    public EnemyBossData BossData => bossData;

    private List<EnemyBossSkill> meleeSkills;
    private List<EnemyBossSkill> rangedSkills;

    private Dictionary<EnemyBossSkill, float> skillCooldowns = new Dictionary<EnemyBossSkill, float>();
    // 여러 스킬을 한꺼번에 사용하는 걸 방지하기 위해 모든 스킬이 체크하는 쿨타임. 
    private float globalSkillCooldownDuration = 5f;
    private float currentGlobalCooldown = 0f; 

    public override void SetPrefab()
    {
        prefab = bossData.prefab;
    }

    public void Initialize(EnemyBossData bossData, PathData pathData)
    {
        base.Initialize(bossData, pathData);

        // 스킬 초기화
        SetSkills();
    }

    // 스킬을 초기화함
    protected override void SetSkills()
    {
        foreach (EnemyBossSkill skill in BossData.MeleeSkills)
        {
            if (skill.SkillType == EnemyBossSkillType.Melee)
            {
                meleeSkills.Add(skill);
                skillCooldowns.Add(skill, skill.CoolTime);
            }
            else
            {
                Debug.LogError("Melee 스킬이 아닌 스킬이 들어가 있음!!!!");
            }
        }
        foreach (EnemyBossSkill skill in BossData.RangedSkills)
        {
            if (skill.SkillType == EnemyBossSkillType.Ranged)
            {
                rangedSkills.Add(skill);
                skillCooldowns.Add(skill, skill.CoolTime);
            }
            else
            {
                Debug.LogError("Ranged 스킬이 아닌 스킬이 들어가 있음!!!!");
            }
        }
    }

    protected override void CreateObjectPool()
    {
        base.CreateObjectPool();

        // 스킬 오브젝트 풀 생성 - 전체 스킬이 skillCooldowns에 들어간 상태이므로 반복문을 2번 쓰지 않기 위해 이런 식으로 구현
        foreach (var skill in skillCooldowns.Keys)
        {
            skill.InitializeSkillObjectPool(this);
        }
    }

    // 쿨다운이 돌고 있는 상황이라면 쿨다운을 업데이트한다.
    protected override void UpdateAllCooldowns()
    {
        base.UpdateAllCooldowns();

        if (currentGlobalCooldown > 0)
        {
            currentGlobalCooldown -= Time.deltaTime;
        }

        // Dict의 Value만 바꿔도 되기에 이렇게 안 해도 되지만 습관 들여놓기
        var skillKeys = new List<EnemyBossSkill>(skillCooldowns.Keys);
        foreach (var skill in skillKeys)
        {
            if (skillCooldowns[skill] > 0)
            {
                skillCooldowns[skill] -= Time.deltaTime;
            }
        }
    }


    protected override void DecideAndPerformAction()
    {
        if (TryUseSkill())
        {
            return;
        }

        base.DecideAndPerformAction();
    }

    private bool TryUseSkill()
    {
        // 공통 쿨다운일 때는 실행되지 않음
        if (currentGlobalCooldown > 0f) return false;

        // 저지를 당할 때는 근거리 스킬 중에서 설정, 아니라면 원거리 중에서 설정
        // 이 경우는 참조 데이터를 "읽기"만 하기 때문에 별도의 리스트를 사용할 필요는 없다.
        List<EnemyBossSkill> skillsToCheck = (BlockingOperator != null) ? meleeSkills : rangedSkills;

        // 근접 스킬에 넣은 스킬 순서대로 돌아감
        foreach (EnemyBossSkill bossSkill in skillsToCheck)
        {
            if (skillCooldowns[bossSkill] <= 0 && bossSkill.CanActivate(this))
            {
                bossSkill.Activate(this);

                // 쿨다운 설정
                currentGlobalCooldown = globalSkillCooldownDuration;
                skillCooldowns[bossSkill] = bossSkill.CoolTime;

                return true;
            }
        }
        
        return false;
    }

    
    

    // 연결 구현
    public override void Initialize(EnemyData enemyData, PathData pathData)
    {
        if (enemyData is EnemyBossData bossData)
        {
            Initialize(bossData, pathData);
        }
    }
}