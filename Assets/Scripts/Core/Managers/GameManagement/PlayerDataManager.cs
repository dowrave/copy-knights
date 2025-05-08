using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

// ������� ������(���� ���۷�����, ������ ���)�� �����Ѵ�.
// GameManagement�� ���� ������Ʈ
public class PlayerDataManager : MonoBehaviour
{
    // �÷��̾ ������ ������ ����
    [Serializable] 
    private class PlayerData
    {
        public List<OwnedOperator> ownedOperators = new List<OwnedOperator>(); // ���� ���۷�����
        //public List<string> currentSquadOperatorNames = new List<string>(); // ������, ����ȭ�� ���� �̸��� ����
        public List<SquadOperatorInfoForSave> currentSquad = new List<SquadOperatorInfoForSave>();
        public int maxSquadSize;
        public UserInventoryData inventory = new UserInventoryData(); // ������ �κ��丮
        public StageResultData stageResults = new StageResultData(); // �������� ���� ��Ȳ
        public bool isTutorialFinished = false;
        public List<TutorialStatus> tutorialDataStatus = new List<TutorialStatus>();
    }



    public enum TutorialStatus
    {
        NotStarted,
        InProgress,
        Failed,
        Completed
    }

    private PlayerData? playerData;
    private Dictionary<string, OperatorData> operatorDatabase = new Dictionary<string, OperatorData>();

    [SerializeField] private List<OperatorData> startingOperators = new List<OperatorData>(); //  nullable ��� ȸ��: �Ҵ��� ������ �� ����Ʈ
    [SerializeField] private int defaultMaxSquadSize = 6;

    private Dictionary<string, ItemData> itemDatabase = new Dictionary<string, ItemData>();

    public event System.Action OnSquadUpdated = delegate { }; // nullable ��� ȸ��


    private void Awake()
    {
        ResetPlayerData(); // ������
        InitializeSystem();
        
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
        InstanceValidator.ValidateInstance(startingOperators);


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
                Debug.LogError($"{path}���� OperatorData �ε� ����");
            }
        }

        // Validate starting operators are in database
        foreach (var op in startingOperators!)
        {
            InstanceValidator.ValidateInstance(op.entityName);

            if (op != null && !operatorDatabase.ContainsKey(op.entityName!))
            {
                Debug.LogError($"Starting operator {op.entityName!} not found in database!");
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
                maxSquadSize = defaultMaxSquadSize
            };

            // Ʃ�丮�� ���� �ʱ�ȭ. ���� TutorialData�� ������ 3��. 
            for (int i = 0; i < 3; i++)
            {
                playerData.tutorialDataStatus.Add(TutorialStatus.NotStarted);
            }


            // �ʱ� ���۷����͸� ownedOperators�� �߰�
            if (startingOperators != null)
            {
                foreach (var op in startingOperators)
                {
                    AddOperator(op.entityName!);
                }
            }

            // ������ ����Ʈ�� �ʱ�ȭ��
            InitializeEmptySquad();
            SavePlayerData();
        }
        else
        {
            playerData = JsonUtility.FromJson<PlayerData>(savedData);
            //playerData = JsonConvert.DeserializeObject<PlayerData>(savedData);
            ValidateSquadSize();

            // ����ȭ, ������ ���� ������� �ݿ�
            InitializeAllOperators();
        }
    }

    private void InitializeEmptySquad()
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;  

        safePlayerData.currentSquad.Clear();
        for (int i = 0; i < safePlayerData.maxSquadSize; i++)
        {
            //safePlayerData.currentSquadOperatorNames.Add(string.Empty); // null ��� �� ���ڿ� �߰�
            safePlayerData.currentSquad.Add(new SquadOperatorInfoForSave());
        }
    }

    private void ValidateSquadSize()
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        if (safePlayerData.currentSquad?.Count > safePlayerData.maxSquadSize)
        {
            safePlayerData.currentSquad = safePlayerData.currentSquad?
                .Take(safePlayerData.maxSquadSize) // ó������ ������ ����ŭ �������� �޼���
                .ToList() ?? new List<SquadOperatorInfoForSave>();
        }

        SavePlayerData();
    }

    // operatorName�� �ش��ϴ� OwnedOperator�� ��´�
    public OwnedOperator? GetOwnedOperator(string operatorName)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        return safePlayerData.ownedOperators.Find(op => op.operatorName == operatorName) ?? null;
    }

    // ������ �ִ� ���۷����͵� �ҷ�����
    public List<OwnedOperator> GetOwnedOperators()
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        return new List<OwnedOperator>(safePlayerData.ownedOperators);
    }

    private void TestAboutTutorial()
    {
        // �׽�Ʈ: TutorialManager�� 2��° Ʃ�丮����� Ŭ������ ���� �ùķ��̼�
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        // Ʃ�丮�� ���� ���� ����: ù ��°�� �� ��° Ʃ�丮�� �Ϸ�, �� ��°�� NotStarted
        if (safePlayerData.tutorialDataStatus.Count >= 3)
        {
            safePlayerData.tutorialDataStatus[0] = TutorialStatus.Completed;
            safePlayerData.tutorialDataStatus[1] = TutorialStatus.Completed;
            safePlayerData.tutorialDataStatus[2] = TutorialStatus.NotStarted;
        }
        else
        {
            safePlayerData.tutorialDataStatus = new List<TutorialStatus>
            {
                TutorialStatus.Completed,
                TutorialStatus.Completed,
                TutorialStatus.NotStarted
            };
        }

        // ��ü Ʃ�丮�� �Ϸ� ���δ� ���� �ƴ�
        safePlayerData.isTutorialFinished = false;
        SavePlayerData();
    }

    // �׽�Ʈ�� �ʱ�ȭ
    private void InitializeForTest()
    {
        //TestAboutTutorial();

        int targetLevel = 50;

        ////���۷����͵� ���� �ݿ�
        foreach (var op in playerData.ownedOperators)
        {
            InitializeOperatorLevelUp(op, targetLevel);
            InitializeOperator1stPromotion(op);
        }

        // ���۷����͵� ���Կ� ��ġ
        InitializeSquad();

        // ������ ����
        //AddStartingItems();

        // �������� ���� Ŭ����
        StageClearAndGetRewards("1-0", 3);
        StageClearAndGetRewards("1-1", 3);


        SavePlayerData();
    }

    private void StageClearAndGetRewards(string stageId, int stars)
    {
        // ���� �߿�) ���� ���� �� ���
        StageData stageData = GameManagement.Instance!.StageDatabase.GetDataById(stageId);
        GameManagement.Instance!.RewardManager.SetAndGiveStageRewards(stageData, stars);
        RecordStageResult(stageId, stars);
    }


    // ������ ���۷����͸� �����ϰ� ��
    public void AddOperator(string operatorName)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        if (!safePlayerData.ownedOperators.Any(op => op.operatorName == operatorName))
        {
            OperatorData opData = GetOperatorData(operatorName);
            OwnedOperator newOp = new OwnedOperator(opData);

            //InitializeOperator1stPromotion(newOp);

            safePlayerData.ownedOperators.Add(newOp);
            SavePlayerData();
            Debug.Log($"{newOp.operatorName}�� ���������� ownedOperator�� ��ϵǾ����ϴ�");
        }
    }


    // PlayerPrefs�� Json���� PlayerData�� �����Ѵ�.
    public void SavePlayerData()
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        string jsonData = JsonUtility.ToJson(safePlayerData);

        // Dictionary ����ȭ�� ���� Newtonsoft.Json ���
        //string jsonData = JsonConvert.SerializeObject(safePlayerData, settings);
        PlayerPrefs.SetString("PlayerData", jsonData);
        PlayerPrefs.Save();
    }

    public OperatorData GetOperatorData(string operatorName)
    {
        return operatorDatabase[operatorName];
    }

    public List<OperatorData> GetOwnedOperatorDatas()
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        return safePlayerData.ownedOperators
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


    // opName ����Ʈ�� OwnedOperator ����Ʈ�� �� ��ȯ��
    //public List<OwnedOperator> GetCurrentSquad()
    //{
    //    InstanceValidator.ValidateInstance(playerData);
    //    var safePlayerData = playerData!;

    //    return safePlayerData.currentSquad
    //        .Where(squadOpInfo => squadOpInfo != null && !string.IsNullOrEmpty(squadOpInfo.operatorName))
    //        .Select(squadOpInfo => GetOwnedOperator(squadOpInfo.operatorName!)) // 'opName'�� null�� �ƴ��� ��������� ǥ��  
    //        .Where(op => op != null)
    //        .Cast<OwnedOperator>() // null�� �ƴ� OwnedOperator�� ĳ����  
    //        .ToList() ?? new List<OwnedOperator>();
    //}

    // ���� ���� �޼��� : <OwnedOperator, skillIndex>�� ���� ����Ʈ�� ��ȯ��. ��Ÿ�ӿ����� ����ϴ� Ÿ���� �ϳ� �����ص���.
    public List<SquadOperatorInfo> GetCurrentSquad()
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        return safePlayerData.currentSquad
            .Where(savedInfo => savedInfo != null && !string.IsNullOrEmpty(savedInfo.operatorName))
            .Select(savedInfo =>
            {
                OwnedOperator ownedOp = GetOwnedOperator(savedInfo.operatorName!);

                if (ownedOp != null)
                {
                    return new SquadOperatorInfo(ownedOp, savedInfo.skillIndex);
                }
                return null;
            })
            .Where(runtimeInfo => runtimeInfo != null) // null�� �׸�� ����
            .Select(runtimeInfo => runtimeInfo!) // nullability ��� ���� ����
            .ToList() ?? new List<SquadOperatorInfo>();
    }


    // null�� ������ ��ü ������ ����Ʈ ��ȯ. MaxSquadSize�� ����ȴ�.
    //public List<OwnedOperator?> GetCurrentSquadWithNull()
    //{
    //    InstanceValidator.ValidateInstance(playerData);
    //    var safePlayerData = playerData!;

    //    return safePlayerData.currentSquad
    //        .Select(squadOpInfo => string.IsNullOrEmpty(squadOpInfo.operatorName) ? null : GetOwnedOperator(squadOpInfo.operatorName))
    //        .ToList();
    //}

    public List<SquadOperatorInfo?> GetCurrentSquadWithNull()
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        if (safePlayerData.currentSquad == null) return new List<SquadOperatorInfo?>();

        return safePlayerData.currentSquad
            .Select(savedInfo =>
            {
                if (savedInfo == null || string.IsNullOrEmpty(savedInfo.operatorName))
                {
                    return (SquadOperatorInfo?)null;
                }
                else
                {
                    OwnedOperator ownedOp = GetOwnedOperator(savedInfo.operatorName);

                    if (ownedOp != null) return new SquadOperatorInfo(ownedOp, savedInfo.skillIndex);
                    else
                    {
                        return (SquadOperatorInfo?)null;
                    }
                }
            })
            .ToList();
    }

    // UI�� �� ���� �ִ� OperatorData ����Ʈ ��ȯ
    public List<OperatorData> GetCurrentSquadData()
    {
        return GetCurrentSquad()
            .Select(opInfo => opInfo.op.OperatorProgressData)
            .Where(op => op != null)
            .ToList();
    }



    // �����带 ������Ʈ�Ѵ�
    public bool TryUpdateSquad(int squadIndex, string operatorName, int skillIndex)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;
        List<SquadOperatorInfoForSave> squadForSave = safePlayerData.currentSquad;

        if (squadIndex < 0 || squadIndex >= safePlayerData.maxSquadSize) return false;


        // ������ ũ�� Ȯ��
        while (squadForSave.Count <= squadIndex)
        {
            squadForSave.Add(new SquadOperatorInfoForSave());
        }

        // ���۷����� ���� Ȯ��
        if (!string.IsNullOrEmpty(operatorName) && !safePlayerData.ownedOperators.Any(op => op.operatorName == operatorName)) return false;

        // ���� �̸��� ���۷����� �ߺ� ���� : ��, ��ų �ε����� �ٸ� ���� �����
        if (squadForSave.Any(opInfo => opInfo != null && opInfo.operatorName == operatorName && opInfo.skillIndex == skillIndex)) return false;

        // ��� ����
        // ��ų �ε����� �����ϴ� ������ �ʿ��غ���. �̰� ���������� �����ؾ� �� �����̶� �ϴ� �ۼ��� �ص�
        //safePlayerData.currentSquadOperatorNames[squadIndex] = operatorName;
        squadForSave[squadIndex] = new SquadOperatorInfoForSave(operatorName, skillIndex);

        // ������ �������� �ݺ��� �����ؼ� ����
        for (int i=0; i < squadForSave.Count; i++)
        {
            Debug.Log($"������ ���� : {i}��° �ε��� - ({squadForSave[i].operatorName}, ��ų �ε��� : {squadForSave[i].skillIndex})");
        }

        SavePlayerData();
        OnSquadUpdated?.Invoke();
        return true;
    }

    // �ϰ� ������Ʈ��
    public bool UpdateFullSquad(List<SquadOperatorInfo> tempOwnedOpSquad)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;
        var newSquadForSave = new List<SquadOperatorInfoForSave>(safePlayerData.maxSquadSize);

        // 1. newSquadForSave ������ ä��
        for (int i = 0; i < safePlayerData.maxSquadSize; i++)
        {
            if (i < tempOwnedOpSquad.Count)
            {
                SquadOperatorInfo runtimeInfo = tempOwnedOpSquad[i];
                if (runtimeInfo == null || runtimeInfo.op == null)
                {
                    // �� ���� ó��
                    newSquadForSave.Add(new SquadOperatorInfoForSave()); // �⺻��
                }
                else
                {
                    string opName = runtimeInfo.op.operatorName;
                    int skillIdx = runtimeInfo.skillIndex;

                    if (!string.IsNullOrEmpty(opName) &&
                        (safePlayerData.ownedOperators == null || !safePlayerData.ownedOperators.Any(ownedOp => ownedOp.operatorName == opName)))
                    {
                        // �������� ���� ���۷����Ͱ� ������ ������Ʈ ����
                        Debug.LogError($"{opName}�� �����ϰ� �ִ� ���۷����Ͱ� �ƴ�");
                        return false;
                    }

                    // �� ����
                    if (string.IsNullOrEmpty(opName))
                    {
                        skillIdx = -1;
                    }

                    newSquadForSave.Add(new SquadOperatorInfoForSave(opName, skillIdx));
                }
            }
            else
            {
                // ����ִ� ������ ������ �� �������� ä��
                newSquadForSave.Add(new SquadOperatorInfoForSave());
            }
        }
        
        // 2. ���� ������ ������ ��ü
        safePlayerData.currentSquad = newSquadForSave;
        SavePlayerData();
        OnSquadUpdated?.Invoke();
        return true;
    }


    // �����带 �ʱ�ȭ�Ѵ�
    public void ClearSquad()
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        safePlayerData.currentSquad.Clear();
        for (int i = 0; i < safePlayerData.maxSquadSize; i++)
        {
            safePlayerData.currentSquad.Add(new SquadOperatorInfoForSave());
        }
        SavePlayerData();
        OnSquadUpdated?.Invoke();
    }

    private void Start()
    {
        InitializeForTest();
    }

    public int GetMaxSquadSize() => playerData!.maxSquadSize;


    // Ư�� �ε��� ������ ���۷����͸� ��ȯ�Ѵ�. ��� �ְų� Ȱ��ȭ�� �ȵ� �����̸� null�� ��ȯ�Ѵ�. �ص� �ǳ�?
    public OwnedOperator? GetSquadOperatorAt(int index)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        if (index < 0 || index >= safePlayerData.currentSquad.Count) return null;

        string? opName = safePlayerData.currentSquad[index].operatorName;
        return string.IsNullOrEmpty(opName) ? null : GetOwnedOperator(opName);
    }

    public List<string?> GetCurrentSquadOperatorNames()
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        //return new List<string?>(safePlayerData.currentSquadOperatorNames);
        return new List<string?>(safePlayerData.currentSquad.Select(opInfo => opInfo?.operatorName));
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

    public bool UseItems(Dictionary<string, int> itemsToUse)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        // ��� ������ ���� ����
        foreach (var (itemName, count) in itemsToUse)
        {
            var itemStack = safePlayerData.inventory.items.Find(i => i.itemName == itemName);
            if (itemStack == null || itemStack.count < count)
            {
                return false;
            }
        }

       foreach (var (itemName, count) in itemsToUse)
        {
            var itemStack = safePlayerData.inventory.items.Find(i => i.itemName == itemName);
            itemStack.count -= count;
            if (itemStack.count <= 0)
            {
                safePlayerData.inventory.items.Remove(itemStack);
            }
        }

        SavePlayerData();
        return true;
    }

    public List<(ItemData itemData, int count)> GetAllItems()
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        List<(ItemData, int)> result = new List<(ItemData data, int count)>();
        foreach (var itemStack in safePlayerData.inventory.items)
        {
            if (itemDatabase.TryGetValue(itemStack.itemName, out ItemData itemData))
            {
                result.Add((itemData, itemStack.count));
            }
        }

        return result;
    }

    // � �������� �� �� �ִ��� Ȯ���ϴ� �޼���. �ִ��� ���ε� üũ ����.
    public int GetItemCount(string itemName)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        UserInventoryData.ItemStack itemStack = safePlayerData.inventory.items.Find(i => i.itemName == itemName);
        return itemStack?.count ?? 0;
    }

    private void InitializeAllOperators()
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        foreach (var ownedOp in safePlayerData.ownedOperators)
        {
            ownedOp.Initialize();
        }
    }

    private void InitializeSquad()
    {
        // ������ �ִ� ���۷����͵� �ҷ�����
        List<OwnedOperator> ownedOps = GetOwnedOperators();

        // ���۷����͵��� �����忡 ��ġ�ϱ�
        for (int i = 0; i < 6; i++)
        {
            // 2�� ������ ���
            if (i == 1) continue;

            // ���� �߰� ���� �ʿ�
            GameManagement.Instance!.UserSquadManager.TryReplaceOperator(i, ownedOps[i], 0);
        }
    }

    private void InitializeOperatorLevelUp(OwnedOperator op, int level)
    {
        op.LevelUP(level, 0);
    }

    private void InitializeOperator1stPromotion(OwnedOperator op)
    {
        if (op.currentLevel == OperatorGrowthSystem.Elite0MaxLevel)
        {
            op.Promote();
        }
    }

    public bool IsStageCleared(string stageId)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        return safePlayerData.stageResults.clearedStages.Any(info => info.stageId == stageId);
    }

    // Ư�� ���������� Ŭ���� ������ ��������, ������ null�� ��ȯ�Ѵ�.
    public StageResultData.StageResultInfo? GetStageResultInfo(string stageId)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        return safePlayerData.stageResults.clearedStages.FirstOrDefault(info => info.stageId == stageId);
    }

    // �������� Ŭ���� ����ϱ�
    public void RecordStageResult(string stageId, int stars)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        var existingClear = GetStageResultInfo(stageId);
        if (existingClear != null)
        {
            if (existingClear.stars >= stars) return;

            // �� ���� ����� ���� ���, ���� ��� ����
            safePlayerData.stageResults.clearedStages.Remove(existingClear);
        }

        var newClearInfo = new StageResultData.StageResultInfo(stageId, stars);
        safePlayerData.stageResults.clearedStages.Add(newClearInfo);

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
    public OwnedOperator? GetOperatorInSlot(int index)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        if (safePlayerData.currentSquad[index] != null)
        {
            return GetOwnedOperator(safePlayerData.currentSquad[index].operatorName);
        }
        else
        {
            return null;
        }
    }



    public void GrantStageRewards(List<ItemWithCount> firstClearRewards, List<ItemWithCount> basicClearRewards)
    {
        // IReadOnlyList ���� ����� ����ȭ���� ���� �� �־, List�� �ٲ㼭 �����ϴ� �� �����ϴ�.
        //List<ItemWithCount> firstClearRewards = new List<ItemWithCount>(StageManager.Instance!.ActualFirstClearRewards);
        //List<ItemWithCount> basicClearRewards = new List<ItemWithCount>(StageManager.Instance!.ActualBasicClearRewards);

        GrantItems(firstClearRewards);
        GrantItems(basicClearRewards);

        SavePlayerData();
    }

    // ����ڿ��� �������� ����
    private void GrantItems(List<ItemWithCount> rewardItems)
    {
        foreach (ItemWithCount itemWithCount in rewardItems)
        {
            if (itemWithCount.itemData != null)
            {
                // ������ �ݿ��� ������ ���� ����
                AddItems(itemWithCount.itemData.itemName!, itemWithCount.count);
            }
            else
            {
                throw new InvalidOperationException("�������� ���������� ���޵��� �ʴ� ���� �߻�");
            }
        }
    }

    // �������� ���������� �����ϴ� �޼���
    public void AddItems(string itemName, int itemCount)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        if (itemDatabase.TryGetValue(itemName, out ItemData itemData))
        {
            var existingItemStack = safePlayerData.inventory.items
                .FirstOrDefault(itemStack => itemStack.itemName == itemName);

            if (existingItemStack != null)
            {
                // �̹� �ִ� �������̶�� ������ �ø�
                existingItemStack.count += itemCount;
            }
            else
            {
                // �κ��丮�� �������� ������ ���� ����
                safePlayerData.inventory.items.Add(new UserInventoryData.ItemStack(itemData.itemName, itemCount));
            }
        }
        else
        {
            Debug.LogError($"{itemName}�� ������ �����ͺ��̽��� �������� �ʴ� �̸���");
        }
    }

    // Ʃ�丮�� �Ϸ� ���� Ȯ��
    public bool IsAllTutorialFinished()
    {
        return playerData!.isTutorialFinished;
    }

    // ���� ���������� Ŭ����� Ʃ�丮�� �ε���
    public int GetLastCompletedTutorialIndex()
    {
        int lastCompletedIndex = -1;
        for (int i = 0; i < 3; i++)
        {
            if (IsTutorialStatus(i, TutorialStatus.Completed))
            {
                lastCompletedIndex = i;
            }
        }
        return lastCompletedIndex;
    }

    // Ʃ�丮�� ���¸� �����ϴ� �޼���
    public void SetTutorialStatus(int tutorialIndex, TutorialStatus status)
    {
        playerData.tutorialDataStatus[tutorialIndex] = status;
        SavePlayerData();
    }

    // Ʃ�丮�� ���¸� Ȯ���ϴ� �޼���
    public bool IsTutorialStatus(int tutorialIndex, TutorialStatus status)
    {
        return playerData.tutorialDataStatus[tutorialIndex] == status;
    }

    // ��� Ʃ�丮�� ����
    public void FinishAllTutorials()
    {
        playerData.isTutorialFinished = true;

        // ��� Ʃ�丮���� �Ϸ� ���·� ����
        for (int i = 0; i < 3; i++)
        {
            playerData.tutorialDataStatus[i] = TutorialStatus.Completed;
        }

        SavePlayerData();
    }

    // ����ȭ�� �ʿ��� �����۵��� ��� �ִ��� �˻��ϴ� �޼���
    public bool HasPromotionItems(OperatorData opData)
    {
        foreach (OperatorData.PromotionItems promotionItem in opData.promotionItems)
        {
            string itemName = promotionItem.itemData.itemName;
            int requiredCount = promotionItem.count;
            int playerHasCount = GetItemCount(itemName);
            if (playerHasCount < requiredCount) return false;
        }

        return true;
    }

    // ������ �����ͺ��̽��� �̿��� �ʱ� ������ ����
    // �̸��� itemData.name �ʵ��� �װ�(LoadItemDatabase ����)
    private void AddStartingItems()
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        // ���� ���� Ű���� ItemData.name ���� / ������
        if (itemDatabase.TryGetValue("ExpSmall", out ItemData expSmall))
        {
            safePlayerData.inventory.items.Add(new UserInventoryData.ItemStack(expSmall.itemName, 99));
        }

        if (itemDatabase.TryGetValue("ExpMiddle", out ItemData expMiddle))
        {
            safePlayerData.inventory.items.Add(new UserInventoryData.ItemStack(expMiddle.itemName, 99));
        }

        if (itemDatabase.TryGetValue("ItemPromotion", out ItemData promotion))
        {
            safePlayerData.inventory.items.Add(new UserInventoryData.ItemStack(promotion.itemName, 1));
        }
    }
}

[Serializable]
public class SquadOperatorInfoForSave
{
    public string operatorName;
    public int skillIndex;

    // ������
    public SquadOperatorInfoForSave()
    {
        operatorName = string.Empty;
        skillIndex = -1;
    }

    public SquadOperatorInfoForSave(string name, int index)
    {
        operatorName = name;
        skillIndex = index;
    }
}
