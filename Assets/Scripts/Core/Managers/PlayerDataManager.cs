using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


// ������� ������(���� ���۷�����, ������ ���)�� �����Ѵ�.
// GameManagement�� ���� ������Ʈ
public class PlayerDataManager : MonoBehaviour
{
    // �÷��̾ ������ ������ ����
    [System.Serializable] 
    private class PlayerData
    {
        public List<OwnedOperator> ownedOperators = new List<OwnedOperator>(); // ���� ���۷�����
        public List<string> currentSquadOperatorNames = new List<string>(); // ������, ����ȭ�� ���� �̸��� ����
        public int maxSquadSize;
        public UserInventoryData inventory = new UserInventoryData(); // ������ �κ��丮
        public StageResultData stageResults = new StageResultData(); // �������� ���� ��Ȳ
    }

    private PlayerData playerData;
    private Dictionary<string, OperatorData> operatorDatabase = new Dictionary<string, OperatorData>();

    [Header("�ʱ� ���� ���۷�����")]
    [SerializeField] private List<OperatorData> startingOperators; // �ʱ� ���� ���۷�����
    [SerializeField] private int defaultMaxSquadSize = 6;

    private Dictionary<string, ItemData> itemDatabase = new Dictionary<string, ItemData>();

    public event System.Action OnSquadUpdated;


    private void Awake()
    {
        ResetPlayerData(); // ������
        InitializeSystem();
        InitializeForTest();
    }

    private void InitializeSystem()
    {
        LoadOperatorDatabase();
        LoadItemDatabase();
        LoadOrCreatePlayerData();
    }


// ���� ������ ���� "���" OperatorData�� �ҷ��´�
    private void LoadOperatorDatabase()
    {
#if UNITY_EDITOR
        // guid : ����Ƽ���� �� ���¿� �Ҵ��ϴ� ���� �ĺ���
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:OperatorData", 
            new[] { "Assets/ScriptableObjects/Operator" });

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            OperatorData opData = UnityEditor.AssetDatabase.LoadAssetAtPath<OperatorData>(path);
            if (opData != null && !string.IsNullOrEmpty(opData.entityName))
            {
                operatorDatabase[opData.entityName] = opData;
            }
            else
            {
                Debug.LogError($"{path}���� OperatorData �ε� ����, Ȥ�� {opData.entityName}�̶�� ��ƼƼ �̸��� ��� ����");
            }
        }

        // Validate starting operators are in database
        foreach (var op in startingOperators)
        {
            if (op != null && !operatorDatabase.ContainsKey(op.entityName))
            {
                Debug.LogError($"Starting operator {op.entityName} not found in database!");
            }
        }
#endif
    }


    // PlayerPrefs�� �̿��� ����� �����͸� �ҷ����ų�, ������ ���� �����Ѵ�.
    private void LoadOrCreatePlayerData()
    {
        // PlayerPrefs�� ����� PlayerData�� �ҷ����ų� ������ null(�� ĭ)
        string savedData = PlayerPrefs.GetString("PlayerData", "");

        // ����� ������ ���� ��� ���� ����
        if (string.IsNullOrEmpty(savedData))
        {
            playerData = new PlayerData
            {
                maxSquadSize = defaultMaxSquadSize,
                currentSquadOperatorNames = new List<string>()
            };

            // �ʱ� ���۷����͸� ownedOperators�� �߰�
            foreach (var op in startingOperators)
            {
                AddOperator(op.entityName);
            }

            // ������ ����Ʈ�� �ʱ�ȭ��
            InitializeEmptySquad();
            SavePlayerData();
        }
        else
        {
            playerData = JsonUtility.FromJson<PlayerData>(savedData);
            ValidateSquadSize();

            // ����ȭ, ������ ���� ������� �ݿ�
            InitializeAllOperators();
        }
    }

    private void InitializeEmptySquad()
    {
        playerData.currentSquadOperatorNames.Clear();
        for (int i = 0; i < playerData.maxSquadSize; i++)
        {
            playerData.currentSquadOperatorNames.Add(null);
        }
    }

    private void ValidateSquadSize()
    {
        if (playerData.currentSquadOperatorNames.Count > playerData.maxSquadSize)
        {
            playerData.currentSquadOperatorNames = playerData.currentSquadOperatorNames
                .Take(playerData.maxSquadSize) // ó������ ������ ����ŭ �������� �޼���
                .ToList();
        }

        SavePlayerData();
    }

    public OwnedOperator GetOwnedOperator(string operatorName)
    {
        return playerData.ownedOperators.Find(op => op.operatorName == operatorName);
    }

    public List<OwnedOperator> GetOwnedOperators()
    {
        return new List<OwnedOperator>(playerData.ownedOperators);
    }

    private void InitializeForTest()
    {
        // ���۷����͵� 1����ȭ
        //foreach (var op in playerData.ownedOperators)
        //{
        //    InitializeOperator1stPromotion(op);
        //    SavePlayerData();
        //}

        // ������ ����
        //AddStartingItems();

        // �������� ���� Ŭ����
        //RecordStageResult("1-1", 3);
        //RecordStageResult("1-2", 2);
    }



    // ������ ���۷����͸� �����ϰ� ��
    public void AddOperator(string operatorName)
    {
        if (!playerData.ownedOperators.Any(op => op.operatorName == operatorName))
        {
            OperatorData opData = GetOperatorData(operatorName);
            OwnedOperator newOp = new OwnedOperator(opData);

            //InitializeOperator1stPromotion(newOp);

            playerData.ownedOperators.Add(newOp);
            SavePlayerData();
            Debug.Log($"{newOp.operatorName}�� ���������� ownedOperator�� ��ϵǾ����ϴ�");
        }
    }


    // PlayerPrefs�� Json���� PlayerData�� �����Ѵ�.
    public void SavePlayerData()
    {
        string jsonData = JsonUtility.ToJson(playerData);
        PlayerPrefs.SetString("PlayerData", jsonData);
        PlayerPrefs.Save();
    }

    public OperatorData GetOperatorData(string operatorName)
    {
        return operatorDatabase[operatorName];
    }

    public List<OperatorData> GetOwnedOperatorDatas()
    {
        return playerData.ownedOperators
            .Select(owned => GetOperatorData(owned.operatorName))
            .Where(data => data != null)
            .ToList();
    }

    private void ResetPlayerData()
    {
        PlayerPrefs.DeleteKey("PlayerData");
        PlayerPrefs.DeleteKey("SquadData");
        PlayerPrefs.Save();
    }


    // null�� �������� ���� ���� ��ġ�� ���۷����͸� ���Ե� ������ ����Ʈ ��ȯ
    public List<OwnedOperator> GetCurrentSquad()
    {
        return playerData.currentSquadOperatorNames
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(opName => GetOwnedOperator(opName))
            .Where(op => op != null)
            .ToList();
    }


    // null�� ������ ��ü ������ ����Ʈ ��ȯ. MaxSquadSize�� ����ȴ�.
    public List<OwnedOperator> GetCurrentSquadWithNull()
    {
        return playerData.currentSquadOperatorNames
            .Select(opName => string.IsNullOrEmpty(opName) ? null : GetOwnedOperator(opName))
            .ToList();
    }

    // UI�� �� ���� �ִ� OperatorData ����Ʈ ��ȯ
    public List<OperatorData> GetCurrentSquadData()
    {
        return GetCurrentSquad()
            .Select(ownedOp => ownedOp.BaseData)
            .Where(op => op != null)
            .ToList();
    }



    // �����带 ������Ʈ�Ѵ�
    public bool TryUpdateSquad(int index, string operatorName)
    {
        if (index < 0 || index >= playerData.maxSquadSize) return false;

        // ������ ũ�� Ȯ��
        while (playerData.currentSquadOperatorNames.Count <= index)
        {
            playerData.currentSquadOperatorNames.Add(null);
        }

        // ���۷����� ���� Ȯ��
        if (!string.IsNullOrEmpty(operatorName) && !playerData.ownedOperators.Any(op => op.operatorName == operatorName)) return false;

        // �ߺ� üũ
        if (!string.IsNullOrEmpty(operatorName) && playerData.currentSquadOperatorNames.Contains(operatorName)) return false;

        playerData.currentSquadOperatorNames[index] = operatorName;
        SavePlayerData();
        OnSquadUpdated?.Invoke();
        return true;
    }


    // �����带 �ʱ�ȭ�Ѵ�
    public void ClearSquad()
    {
        playerData.currentSquadOperatorNames.Clear();
        for (int i = 0; i < playerData.maxSquadSize; i++)
        {
            playerData.currentSquadOperatorNames.Add(null);
        }
        SavePlayerData();
        OnSquadUpdated?.Invoke();
    }

    public int GetMaxSquadSize() => playerData.maxSquadSize;


    // Ư�� �ε��� ������ ���۷����͸� ��ȯ�Ѵ�. ��� �ְų� Ȱ��ȭ�� �ȵ� �����̸� null�� ��ȯ�Ѵ�. �ص� �ǳ�?
    public OwnedOperator GetSquadOperatorAt(int index)
    {
        if (index < 0 || index >= playerData.currentSquadOperatorNames.Count) return null;

        string opName = playerData.currentSquadOperatorNames[index];
        return string.IsNullOrEmpty(opName) ? null : GetOwnedOperator(opName);
    }

    public List<string> GetCurrentSquadOperatorNames()
    {
        return new List<string>(playerData.currentSquadOperatorNames); 
    }

    private void LoadItemDatabase()
    {
#if UNITY_EDITOR 
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData",
            new[] { "Assets/ScriptableObjects/Items" });
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ItemData itemData = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (itemData != null)
            {
                itemDatabase[itemData.name] = itemData;
            }
        }
#endif
    }

    public bool AddItem(string itemName, int count = 1)
    {
        UserInventoryData.ItemStack existingItem = playerData.inventory.items.Find(i => i.itemName == itemName);

        // dict�� �̿�, �������� ������ ���� ���ϰ� ������ ���� ����
        if (existingItem != null)
        {
            existingItem.count += count;
        }
        else
        {
            playerData.inventory.items.Add(new UserInventoryData.ItemStack(itemName, count));
        }

        SavePlayerData();
        return true;
    }

    public bool UseItems(Dictionary<string, int> itemsToUse)
    {
       // ��� ������ ���� ����
       foreach (var (itemName, count) in itemsToUse)
        {
            var itemStack = playerData.inventory.items.Find(i => i.itemName == itemName);
            if (itemStack == null || itemStack.count < count)
            {
                return false;
            }
        }

       foreach (var (itemName, count) in itemsToUse)
        {
            var itemStack = playerData.inventory.items.Find(i => i.itemName == itemName);
            itemStack.count -= count;
            if (itemStack.count <= 0)
            {
                playerData.inventory.items.Remove(itemStack);
            }
        }

        SavePlayerData();
        return true;
    }

    public List<(ItemData itemData, int count)> GetAllItems()
    {
        List<(ItemData, int)> result = new List<(ItemData data, int count)>();
        foreach (var itemStack in playerData.inventory.items)
        {
            if (itemDatabase.TryGetValue(itemStack.itemName, out ItemData itemData))
            {
                result.Add((itemData, itemStack.count));
            }
        }

        return result;
    }

    public int GetItemCount(string itemName)
    {
        UserInventoryData.ItemStack itemStack = playerData.inventory.items.Find(i => i.itemName == itemName);
        return itemStack?.count ?? 0;
    }

    private void InitializeAllOperators()
    {
        foreach (var ownedOp in playerData.ownedOperators)
        {
            ownedOp.Initialize();
        }
    }

    private void InitializeOperator1stPromotion(OwnedOperator op)
    {
        op.LevelUP(50, 0);
        op.Promote();
    }

    public bool IsStageCleared(string stageId)
    {
        return playerData.stageResults.clearedStages.Any(info => info.stageId == stageId);
    }

    // Ư�� ���������� Ŭ���� ���� ��������
    public StageResultData.StageResultInfo GetStageResultInfo(string stageId)
    {
        return playerData.stageResults.clearedStages.FirstOrDefault(info => info.stageId == stageId);
    }

    // �������� Ŭ���� ����ϱ�
    public void RecordStageResult(string stageId, int stars)
    {
        var existingClear = GetStageResultInfo(stageId);
        if (existingClear != null)
        {
            if (existingClear.stars >= stars) return;

            // �� ���� ����� ���� ���, ���� ��� ����
            playerData.stageResults.clearedStages.Remove(existingClear);
        }

        var newClearInfo = new StageResultData.StageResultInfo(stageId, stars);
        playerData.stageResults.clearedStages.Add(newClearInfo);

        SavePlayerData();
    }

    // ��� ���� Ȯ��
    public bool IsStageUnlocked(string stageId)
    {
        if (stageId == "1-0") return true;

        string[] parts = stageId.Split('-');
        if (parts.Length != 2) return false;

        int chapter = int.Parse(parts[0]);
        int stage = int.Parse(parts[1]);

        // ���� ���������� Ŭ������� ������ ���
        string previousStageId = $"{chapter}-{stage - 1}";
        return IsStageCleared(previousStageId);
    }

    // �������� �ش� ����
    public OwnedOperator GetOperatorInSlot(int index)
    {
        if (playerData.currentSquadOperatorNames[index] != null)
        {
            return GetOwnedOperator(playerData.currentSquadOperatorNames[index]);
        }
        else
        {
            return null;
        }
    }

    public void GrantStageRewards(List<ItemWithCount> rewardItems)
    {
        if (rewardItems == null) 
        {
            Debug.LogWarning("���޹��� ������ ����� ����ֽ��ϴ�.");
            return;
        }

        bool errorOccurred = false;

        foreach (ItemWithCount itemWithCount in rewardItems)
        {
            if (itemWithCount.itemData != null)
            {
                AddItems(itemWithCount.itemData.itemName, itemWithCount.count);
                Debug.Log($"{itemWithCount.itemData.itemName} x {itemWithCount.count} ���� �Ϸ�");
            }
            else
            {
                Debug.LogError("ItemData�� null�̰ų�, �ε���� �ʾҽ��ϴ�.");
                errorOccurred = true;
            }
        }

        // ���������� �������� �߰��Ǿ��ٸ� ����
        SavePlayerData();

        if (errorOccurred)
        {
            Debug.LogWarning("�Ϻ� �������� ȹ������ ���߽��ϴ�");
        }
    }

    // �������� �����ϰ� �����ϴ� �޼���
    public void AddItems(string itemName, int itemCount)
    {
        if (itemDatabase.TryGetValue(itemName, out ItemData itemData))
        {
            var existingItemStack = playerData.inventory.items
                .FirstOrDefault(itemStack => itemStack.itemName == itemName);

            if (existingItemStack != null)
            {
                // �̹� �ִ� �������̶�� ������ �ø�
                existingItemStack.count += itemCount;
            }
            else
            {
                // �κ��丮�� �������� ������ ���� ����
                playerData.inventory.items.Add(new UserInventoryData.ItemStack(itemData.itemName, itemCount));
            }
        }
        else
        {
            Debug.LogError($"{itemName}�� ������ �����ͺ��̽��� �������� �ʴ� �̸���");
        }
    }

    // ������ �����ͺ��̽��� �̿��� �ʱ� ������ ����
    // �̸��� itemData.name �ʵ��� �װ�(LoadItemDatabase ����)
    private void AddStartingItems()
    {
        // ���� ���� Ű���� ItemData.name ���� / ������
        if (itemDatabase.TryGetValue("ExpSmall", out ItemData expSmall))
        {
            playerData.inventory.items.Add(new UserInventoryData.ItemStack(expSmall.itemName, 5));
        }

        if (itemDatabase.TryGetValue("ExpMiddle", out ItemData expMiddle))
        {
            playerData.inventory.items.Add(new UserInventoryData.ItemStack("ExpMiddle", 1));
        }

        if (itemDatabase.TryGetValue("ItemPromotion", out ItemData promotion))
        {
            playerData.inventory.items.Add(new UserInventoryData.ItemStack("ItemPromotion", 1));
        }
    }
}
