using System.Collections.Generic;
using UnityEngine;
using Skills.Base;
using System.Linq;


// 실제로 보유한 오퍼레이터의 저장
[System.Serializable]
public class OwnedOperator
{
    // 저장되는 핵심 데이터 : 진행 상황을 나타내는 최소한의 정보만 저장
    [SerializeField] private string operatorID;
    [SerializeField] private int currentLevel = 1; 
    [SerializeField] private OperatorElitePhase currentPhase = OperatorElitePhase.Elite0;
    [SerializeField] private int currentExp = 0;

    // 런타임에만 존재하는 계산된 필드들
    [System.NonSerialized] private OperatorStats currentStats;
    [System.NonSerialized] private List<Vector2Int> currentAttackableGridPos = new List<Vector2Int>();
    [System.NonSerialized] private List<OperatorSkill> unlockedSkills = new List<OperatorSkill>();
    [System.NonSerialized] private OperatorSkill defaultSelectedSkill = default!;
    [System.NonSerialized] private OperatorData operatorData = default!;

    // 성장에 사용된 아이템 저장 필드
    [SerializeField] private List<ItemWithCount> usedItems = new List<ItemWithCount>();

    // 읽기 전용 프로퍼티
    public string OperatorID => operatorID;
    public int CurrentLevel => currentLevel;
    public OperatorElitePhase CurrentPhase => currentPhase;
    public int CurrentExp => currentExp;

    public OperatorStats CurrentStats => currentStats;
    public List<OperatorSkill> UnlockedSkills => unlockedSkills;
    public OperatorSkill DefaultSelectedSkill => defaultSelectedSkill;
    public int DefaultSelectedSkillIndex => UnlockedSkills.IndexOf(defaultSelectedSkill);
    public List<Vector2Int> CurrentAttackableGridPos => currentAttackableGridPos;
    public OperatorData OperatorData
    {
        get
        {
            // 최초 접근 시
            if (operatorData == null) 
            {
                // Lazy Loading에서 게터임에도 필드를 할당하는 건 잘 확립된 방식임
                operatorData = GameManagement.Instance!.PlayerDataManager.GetOperatorData(operatorID);  // PlayerDataManager에서 operatorID에 해당하는 OperatorData를 가져옴
            }
            return operatorData;
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
        operatorID = opData.EntityID;
        currentLevel = 1;
        currentPhase = OperatorElitePhase.Elite0;
        currentExp = 0;
        operatorData = opData;

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
        currentAttackableGridPos = new List<Vector2Int>(operatorData.AttackableTiles);

        if (currentPhase > OperatorElitePhase.Elite0)
        {
            currentAttackableGridPos.AddRange(operatorData.Elite1Unlocks.additionalAttackTiles);
        }
    }

    private void InitializeSkills()
    {
        unlockedSkills = new List<OperatorSkill> { operatorData.Elite0Skill };

        if (currentPhase > OperatorElitePhase.Elite0 && 
            operatorData.Elite1Unlocks.unlockedSkill != null)
        {
            unlockedSkills.Add(operatorData.Elite1Unlocks.unlockedSkill);
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

        currentPhase = OperatorElitePhase.Elite1;
        currentLevel = 1;
        currentExp = 0;

        Initialize();
    }

    public void SetPromotionAndLevel(int targetPhase, int targetLevel)
    {
        if (targetPhase < (int)OperatorElitePhase.Elite0 || 
            targetPhase > (int)OperatorElitePhase.Elite1) return;
        
        if (targetPhase == (int)OperatorElitePhase.Elite1)
        {
            // 1정예화 필요 시 진행
            LevelUP(50, 0);
            Promote();
        }

        LevelUP(targetLevel, 0);
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

    public void SetCurrentLevel(int level)
    {
        currentLevel = level;
    }

    public void SetCurrentPhase(OperatorElitePhase phase)
    {
        currentPhase = phase;
    }

    public void SetCurrentExp(int exp)
    {
        currentExp = exp;
    }


}