using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public class EnemyBoss : Enemy
{
    [NonSerialized] protected new EnemyData _enemyData; // 부모의 enemyData와 충돌 방지, 이 필드는 직렬화에서 무시됨
    [SerializeField] EnemyBossData _bossData = default!;
    [SerializeField] protected EnemyBossSkillRangeController skillRangeController;

    public override EnemyData BaseData => _bossData;
    public EnemyBossData BossData => _bossData;

    private EnemySkillController _skillController;
    public IEnemySkillReadOnly SkillController => _skillController;  

    private List<EnemyBossSkill> meleeSkills = new List<EnemyBossSkill>();
    private List<EnemyBossSkill> rangedSkills = new List<EnemyBossSkill>();

    private Dictionary<EnemyBossSkill, float> skillCooldowns = new Dictionary<EnemyBossSkill, float>();
    // 여러 스킬을 한꺼번에 사용하는 걸 방지하기 위해 모든 스킬이 체크하는 쿨타임. 
 
    private HashSet<Operator> operatorsInSkillRange = new HashSet<Operator>();
    public IReadOnlyCollection<Operator> OperatorsInSkillRange => operatorsInSkillRange;


    protected override void Awake()
    {
        base.Awake();
        _skillController = new EnemySkillController(this);
    }

    // 오버라이드하지 않고 별도로 구현
    public void Initialize(EnemyBossData bossData, PathData pathData)
    {
        if (_bossData == null)
        {
            _bossData = bossData;
        }

        if (pathData == null) Logger.LogError("pathData가 전달되지 않음");

        _pathData = pathData; 

        skillRangeController.Initialize(this);
    }

    // base.Initialize 템플릿 메서드 1
    protected override void ApplyUnitData()
    {
        if (_bossData == null)
        {
            // SerializeField에 할당되어 있지 않다면 오류 발생
            Logger.LogError($"{gameObject.name}의 EnemyData가 할당되지 않음");
            return;
        }

        // 데이터를 이용해 스탯 초기화
        _stat.Initialize(_bossData);

        _skillController.Initialize();
        skillRangeController.Initialize(this);
    }

    protected override void SetPoolTag()
    {
        PoolTag = _bossData.UnitTag;
    }

    // 스킬을 초기화함
    protected override void SetSkills()
    {
        foreach (EnemyBossSkill skill in BossData.MeleeSkills)
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
        foreach (EnemyBossSkill skill in BossData.RangedSkills)
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

    // Update에 들어가는 템플릿 메서드. 스킬 처리를 추가했다. 
    protected override void OnUpdateAction()
    {
        bool skillUsed = _skillController.OnUpdate();
        if (skillUsed) return;  

        base.OnUpdateAction();
    }

    protected override void UpdateAllCooldowns()
    {
        base.UpdateAllCooldowns();
        _skillController.UpdateAllCooldowns();
    }

    public void OnTargetEnteredSkillRange(DeployableUnitEntity target) => _skillController.OnTargetEnteredSkillRange(target);
    public void OnTargetExitedSkillRange(DeployableUnitEntity target) => _skillController.OnTargetExitedSkillRange(target);
}