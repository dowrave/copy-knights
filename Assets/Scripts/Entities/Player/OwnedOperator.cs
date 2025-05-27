using System.Collections.Generic;
using UnityEngine;
using Skills.Base;
using System.Linq;


// ������ ������ ���۷������� ����
[System.Serializable]
public class OwnedOperator
{
    // ����Ǵ� �ٽ� ������ : ���� ��Ȳ�� ��Ÿ���� �ּ����� ������ ����
    public string operatorName; // OperatorData.entityName�� ����
    public int currentLevel = 1; 
    public OperatorGrowthSystem.ElitePhase currentPhase = OperatorGrowthSystem.ElitePhase.Elite0;
    public int currentExp = 0;

    // ��Ÿ�ӿ��� �����ϴ� ���� �ʵ��
    [System.NonSerialized] private OperatorStats currentStats;
    [System.NonSerialized] private List<Vector2Int> currentAttackableGridPos = new List<Vector2Int>();
    [System.NonSerialized] private List<BaseSkill> unlockedSkills = new List<BaseSkill>();
    [System.NonSerialized] private BaseSkill defaultSelectedSkill = default!;
    [System.NonSerialized] private OperatorData baseData = default!;

    // ���忡 ���� ������ ���� �ʵ�
    [SerializeField] private List<ItemWithCount> usedItems = new List<ItemWithCount>();

    // �б� ���� ������Ƽ
    public OperatorStats CurrentStats => currentStats;
    public List<BaseSkill> UnlockedSkills => unlockedSkills;
    public BaseSkill DefaultSelectedSkill => defaultSelectedSkill;
    public int DefaultSelectedSkillIndex => UnlockedSkills.IndexOf(defaultSelectedSkill);
    public List<Vector2Int> CurrentAttackableGridPos => currentAttackableGridPos;
    public OperatorData OperatorProgressData
    {
        get
        {
            // ���� ���� ��
            if (baseData == null) 
            {
                // Lazy Loading���� �����ӿ��� �ʵ带 �Ҵ��ϴ� �� �� Ȯ���� �����
                baseData = GameManagement.Instance!.PlayerDataManager.GetOperatorData(operatorName);  // PlayerDataManager���� operatorID�� �ش��ϴ� OperatorData�� ������
            }
            return baseData;
        }
    }
    public List<ItemWithCount> UsedItems => usedItems;

    private int defaultSkillIndex; // ���⼭ ���� ��

    public bool CanLevelUp => OperatorGrowthSystem.CanLevelUp(currentPhase, currentLevel);
    public bool CanPromote => OperatorGrowthSystem.CanPromote(currentPhase, currentLevel);


    // ������
    // �߿�!) ����� �����͸� �ε��� ���� �����ڰ� ȣ����� �����Ƿ� Initialize�� ������ �����ؾ� �Ѵ�.
    public OwnedOperator(OperatorData opData)
    {
        operatorName = opData.entityName;
        currentLevel = 1;
        currentPhase = OperatorGrowthSystem.ElitePhase.Elite0;
        currentExp = 0;
        baseData = opData;

        Initialize();
    }

    // ����ȭ, ������ ���� ������ �ݿ���. ���� ���� / ����ȭ �ÿ� ����ȴ�.
    public void Initialize()
    {
        currentStats = OperatorGrowthSystem.CalculateStats(this, currentLevel, currentPhase);
        InitializeAttackRange();
        InitializeSkills();
    }

    // �ε����� �޾� �⺻���� ����� ��ų�� �����Ѵ�.
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

    // ����ȭ ó��
    public void Promote()
    {
        if (!OperatorGrowthSystem.CanPromote(this)) return;

        currentPhase = OperatorGrowthSystem.ElitePhase.Elite1;
        currentLevel = 1;
        currentExp = 0;

        Initialize();
    }

    // �� �޼��� : ���� ������ �߰�
    public void AddUsedItem(ItemData item, int count)
    {
        var existingItem = usedItems.FirstOrDefault(x => x.itemData == item);
        if (existingItem.itemData != null)
        {
            // �̹� �ִٸ� �ش� �ε����� ������ �߰�
            int index = usedItems.IndexOf(existingItem);
            usedItems[index] = new ItemWithCount(item, existingItem.count + count);
        }
        else
        {
            // ���� �߰��ϴ� �Ŷ�� ���� ����
            usedItems.Add(new ItemWithCount(item, count));
        }
    }

    // ������ �̸�, ���� �����ε�
    public void AddUsedItem(Dictionary<string, int> itemsDict)
    {
        foreach (var kvp in itemsDict)
        {
            ItemData itemData = GameManagement.Instance!.PlayerDataManager.GetItemData(kvp.Key);
            AddUsedItem(itemData, kvp.Value);
        }
    }

    // ������ ������, ���� �����ε�
    public void AddUsedItem(Dictionary<ItemData, int> itemsDict)
    {
        foreach (var kvp in itemsDict)
        {
            AddUsedItem(kvp.Key, kvp.Value);
        }
    }

    // ���� ������ ���
    public List<ItemWithCount> GetUsedItemCount()
    {
        return new List<ItemWithCount>(usedItems);
    }

    // ���� ������ ����Ʈ �ʱ�ȭ
    public void ClearUsedItems()
    {
        usedItems.Clear();
    }
}