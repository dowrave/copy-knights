using System.Collections.Generic;
using UnityEngine;
using Skills.Base;
using System.Linq;


// 실제로 보유한 오퍼레이터의 저장
[System.Serializable]
public class OwnedOperator
{
    // 저장되는 핵심 데이터 : 진행 상황을 나타내는 최소한의 정보만 저장
    public string operatorName; // OperatorData.entityName과 동일
    public int currentLevel = 1; 
    public OperatorGrowthSystem.ElitePhase currentPhase = OperatorGrowthSystem.ElitePhase.Elite0;
    public int currentExp = 0;

    // 런타임에만 존재하는 계산된 필드들
    [System.NonSerialized] private OperatorStats currentStats;
    [System.NonSerialized] private List<Vector2Int> currentAttackableGridPos = new List<Vector2Int>();
    [System.NonSerialized] private List<BaseSkill> unlockedSkills = new List<BaseSkill>();
    [System.NonSerialized] private BaseSkill defaultSelectedSkill = default!;
    [System.NonSerialized] private OperatorData baseData = default!;

    // 성장에 사용된 아이템 저장 필드
    [SerializeField] private List<ItemWithCount> usedItems = new List<ItemWithCount>();

    // 읽기 전용 프로퍼티
    public OperatorStats CurrentStats => currentStats;
    public List<BaseSkill> UnlockedSkills => unlockedSkills;
    public BaseSkill DefaultSelectedSkill => defaultSelectedSkill;
    public int DefaultSelectedSkillIndex => UnlockedSkills.IndexOf(defaultSelectedSkill);
    public List<Vector2Int> CurrentAttackableGridPos => currentAttackableGridPos;
    public OperatorData OperatorProgressData
    {
        get
        {
            // 최초 접근 시
            if (baseData == null) 
            {
                // Lazy Loading에서 게터임에도 필드를 할당하는 건 잘 확립된 방식임
                baseData = GameManagement.Instance!.PlayerDataManager.GetOperatorData(operatorName);  // PlayerDataManager에서 operatorID에 해당하는 OperatorData를 가져옴
            }
            return baseData;
        }
    }
    public List<ItemWithCount> UsedItems => usedItems;

    private int defaultSkillIndex; // 여기서 갖는 값

    public bool CanLevelUp => OperatorGrowthSystem.CanLevelUp(currentPhase, currentLevel);
    public bool CanPromote => OperatorGrowthSystem.CanPromote(currentPhase, currentLevel);


    // 생성자
    // 중요!) 저장된 데이터를 로드할 때는 생성자가 호출되지 않으므로 Initialize는 별도로 실행해야 한다.
    public OwnedOperator(OperatorData opData)
    {
        operatorName = opData.entityName;
        currentLevel = 1;
        currentPhase = OperatorGrowthSystem.ElitePhase.Elite0;
        currentExp = 0;
        baseData = opData;

        Initialize();
    }

    // 정예화, 레벨에 따른 사항을 반영함. 게임 실행 / 정예화 시에 실행된다.
    public void Initialize()
    {
        currentStats = OperatorGrowthSystem.CalculateStats(this, currentLevel, currentPhase);
        InitializeAttackRange();
        InitializeSkills();
    }

    // 인덱스를 받아 기본으로 사용할 스킬을 설정한다.
    public void SetDefaultSelectedSkill(int skillIndex)
    {
        if (unlockedSkills.Count > skillIndex)
        {
            defaultSkillIndex = skillIndex;
            defaultSelectedSkill = unlockedSkills[defaultSkillIndex];
        }
    }

    private void InitializeAttackRange()
    {
        currentAttackableGridPos = new List<Vector2Int>(OperatorProgressData.attackableTiles);

        if (currentPhase > OperatorGrowthSystem.ElitePhase.Elite0)
        {
            currentAttackableGridPos.AddRange(baseData.elite1Unlocks.additionalAttackTiles);
        }
    }

    private void InitializeSkills()
    {
        unlockedSkills = new List<BaseSkill> { baseData.elite0Skill };

        if (currentPhase > OperatorGrowthSystem.ElitePhase.Elite0 && 
            baseData.elite1Unlocks.unlockedSkill != null)
        {
            unlockedSkills.Add(baseData.elite1Unlocks.unlockedSkill);
        }

        defaultSelectedSkill = unlockedSkills[0];
    }

    public void LevelUP(int targetLevel, int remainingExp)
    {
        if (targetLevel <= currentLevel) return;
        if (targetLevel > OperatorGrowthSystem.GetMaxLevel(currentPhase)) return;

        currentLevel = targetLevel;
        currentExp = remainingExp;

        Initialize();
    }

    // 정예화 처리
    public void Promote()
    {
        if (!OperatorGrowthSystem.CanPromote(this)) return;

        currentPhase = OperatorGrowthSystem.ElitePhase.Elite1;
        currentLevel = 1;
        currentExp = 0;

        Initialize();
    }

    // 본 메서드 : 사용된 아이템 추가
    public void AddUsedItem(ItemData item, int count)
    {
        var existingItem = usedItems.FirstOrDefault(x => x.itemData == item);
        if (existingItem.itemData != null)
        {
            // 이미 있다면 해당 인덱스의 갯수만 추가
            int index = usedItems.IndexOf(existingItem);
            usedItems[index] = new ItemWithCount(item, existingItem.count + count);
        }
        else
        {
            // 새로 추가하는 거라면 만들어서 넣음
            usedItems.Add(new ItemWithCount(item, count));
        }
    }

    // 아이템 이름, 숫자 오버로드
    public void AddUsedItem(Dictionary<string, int> itemsDict)
    {
        foreach (var kvp in itemsDict)
        {
            ItemData itemData = GameManagement.Instance!.PlayerDataManager.GetItemData(kvp.Key);
            AddUsedItem(itemData, kvp.Value);
        }
    }

    // 아이템 데이터, 숫자 오버로드
    public void AddUsedItem(Dictionary<ItemData, int> itemsDict)
    {
        foreach (var kvp in itemsDict)
        {
            AddUsedItem(kvp.Key, kvp.Value);
        }
    }

    // 사용된 아이템 얻기
    public List<ItemWithCount> GetUsedItemCount()
    {
        return new List<ItemWithCount>(usedItems);
    }

    // 사용된 아이템 리스트 초기화
    public void ClearUsedItems()
    {
        usedItems.Clear();
    }
}