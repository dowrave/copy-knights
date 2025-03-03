using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


// 사용자의 데이터(보유 오퍼레이터, 스쿼드 등등)를 관리한다.
// GameManagement의 하위 오브젝트
public class PlayerDataManager : MonoBehaviour
{
    // 플레이어가 소유한 데이터 정보
    [System.Serializable] 
    private class PlayerData
    {
        public List<OwnedOperator> ownedOperators = new List<OwnedOperator>(); // 보유 오퍼레이터
        public List<string> currentSquadOperatorNames = new List<string>(); // 스쿼드, 직렬화를 위해 이름만 저장
        public int maxSquadSize;
        public UserInventoryData inventory = new UserInventoryData(); // 아이템 인벤토리
        public StageResultData stageResults = new StageResultData(); // 스테이지 진행 상황
    }

    private PlayerData playerData;
    private Dictionary<string, OperatorData> operatorDatabase = new Dictionary<string, OperatorData>();

    [Header("초기 지급 오퍼레이터")]
    [SerializeField] private List<OperatorData> startingOperators; // 초기 지급 오퍼레이터
    [SerializeField] private int defaultMaxSquadSize = 6;

    private Dictionary<string, ItemData> itemDatabase = new Dictionary<string, ItemData>();

    public event System.Action OnSquadUpdated;


    private void Awake()
    {
        ResetPlayerData(); // 디버깅용
        InitializeSystem();
        InitializeForTest();
    }

    private void InitializeSystem()
    {
        LoadOperatorDatabase();
        LoadItemDatabase();
        LoadOrCreatePlayerData();
    }


// 현재 게임이 가진 "모든" OperatorData를 불러온다
    private void LoadOperatorDatabase()
    {
#if UNITY_EDITOR
        // guid : 유니티에서 각 에셋에 할당하는 고유 식별자
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
                Debug.LogError($"{path}에서 OperatorData 로드 실패, 혹은 {opData.entityName}이라는 엔티티 이름이 비어 있음");
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


    // PlayerPrefs를 이용해 저장된 데이터를 불러오거나, 없으면 새로 생성한다.
    private void LoadOrCreatePlayerData()
    {
        // PlayerPrefs에 저장된 PlayerData를 불러오거나 없으면 null(빈 칸)
        string savedData = PlayerPrefs.GetString("PlayerData", "");

        // 저장된 정보가 없는 경우 새로 생성
        if (string.IsNullOrEmpty(savedData))
        {
            playerData = new PlayerData
            {
                maxSquadSize = defaultMaxSquadSize,
                currentSquadOperatorNames = new List<string>()
            };

            // 초기 오퍼레이터를 ownedOperators에 추가
            foreach (var op in startingOperators)
            {
                AddOperator(op.entityName);
            }

            // 스쿼드 리스트를 초기화함
            InitializeEmptySquad();
            SavePlayerData();
        }
        else
        {
            playerData = JsonUtility.FromJson<PlayerData>(savedData);
            ValidateSquadSize();

            // 정예화, 레벨에 따른 변경사항 반영
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
                .Take(playerData.maxSquadSize) // 처음부터 지정된 수만큼 가져오는 메서드
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
        // 오퍼레이터들 1정예화
        //foreach (var op in playerData.ownedOperators)
        //{
        //    InitializeOperator1stPromotion(op);
        //    SavePlayerData();
        //}

        // 아이템 지급
        //AddStartingItems();

        // 스테이지 임의 클리어
        //RecordStageResult("1-1", 3);
        //RecordStageResult("1-2", 2);
    }



    // 유저가 오퍼레이터를 보유하게 함
    public void AddOperator(string operatorName)
    {
        if (!playerData.ownedOperators.Any(op => op.operatorName == operatorName))
        {
            OperatorData opData = GetOperatorData(operatorName);
            OwnedOperator newOp = new OwnedOperator(opData);

            //InitializeOperator1stPromotion(newOp);

            playerData.ownedOperators.Add(newOp);
            SavePlayerData();
            Debug.Log($"{newOp.operatorName}가 정상적으로 ownedOperator에 등록되었습니다");
        }
    }


    // PlayerPrefs에 Json으로 PlayerData를 저장한다.
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


    // null을 포함하지 않은 실제 배치된 오퍼레이터만 포함된 스쿼드 리스트 반환
    public List<OwnedOperator> GetCurrentSquad()
    {
        return playerData.currentSquadOperatorNames
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(opName => GetOwnedOperator(opName))
            .Where(op => op != null)
            .ToList();
    }


    // null을 포함한 전체 스쿼드 리스트 반환. MaxSquadSize가 보장된다.
    public List<OwnedOperator> GetCurrentSquadWithNull()
    {
        return playerData.currentSquadOperatorNames
            .Select(opName => string.IsNullOrEmpty(opName) ? null : GetOwnedOperator(opName))
            .ToList();
    }

    // UI에 쓸 수도 있는 OperatorData 리스트 반환
    public List<OperatorData> GetCurrentSquadData()
    {
        return GetCurrentSquad()
            .Select(ownedOp => ownedOp.BaseData)
            .Where(op => op != null)
            .ToList();
    }



    // 스쿼드를 업데이트한다
    public bool TryUpdateSquad(int index, string operatorName)
    {
        if (index < 0 || index >= playerData.maxSquadSize) return false;

        // 스쿼드 크기 확보
        while (playerData.currentSquadOperatorNames.Count <= index)
        {
            playerData.currentSquadOperatorNames.Add(null);
        }

        // 오퍼레이터 소유 확인
        if (!string.IsNullOrEmpty(operatorName) && !playerData.ownedOperators.Any(op => op.operatorName == operatorName)) return false;

        // 중복 체크
        if (!string.IsNullOrEmpty(operatorName) && playerData.currentSquadOperatorNames.Contains(operatorName)) return false;

        playerData.currentSquadOperatorNames[index] = operatorName;
        SavePlayerData();
        OnSquadUpdated?.Invoke();
        return true;
    }


    // 스쿼드를 초기화한다
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


    // 특정 인덱스 슬롯의 오퍼레이터를 반환한다. 비어 있거나 활성화가 안된 슬롯이면 null을 반환한다. 해도 되나?
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

        // dict를 이용, 아이템이 있으면 값만 더하고 없으면 새로 만듦
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
       // 모든 아이템 수량 검증
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

    // 특정 스테이지의 클리어 정보 가져오기
    public StageResultData.StageResultInfo GetStageResultInfo(string stageId)
    {
        return playerData.stageResults.clearedStages.FirstOrDefault(info => info.stageId == stageId);
    }

    // 스테이지 클리어 기록하기
    public void RecordStageResult(string stageId, int stars)
    {
        var existingClear = GetStageResultInfo(stageId);
        if (existingClear != null)
        {
            if (existingClear.stars >= stars) return;

            // 더 좋은 기록을 냈을 경우, 기존 기록 제거
            playerData.stageResults.clearedStages.Remove(existingClear);
        }

        var newClearInfo = new StageResultData.StageResultInfo(stageId, stars);
        playerData.stageResults.clearedStages.Add(newClearInfo);

        SavePlayerData();
    }

    // 언락 상태 확인
    public bool IsStageUnlocked(string stageId)
    {
        if (stageId == "1-0") return true;

        string[] parts = stageId.Split('-');
        if (parts.Length != 2) return false;

        int chapter = int.Parse(parts[0]);
        int stage = int.Parse(parts[1]);

        // 이전 스테이지가 클리어됐을 때에만 언락
        string previousStageId = $"{chapter}-{stage - 1}";
        return IsStageCleared(previousStageId);
    }

    // 스쿼드의 해당 슬롯
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
            Debug.LogWarning("지급받을 아이템 목록이 비어있습니다.");
            return;
        }

        bool errorOccurred = false;

        foreach (ItemWithCount itemWithCount in rewardItems)
        {
            if (itemWithCount.itemData != null)
            {
                AddItems(itemWithCount.itemData.itemName, itemWithCount.count);
                Debug.Log($"{itemWithCount.itemData.itemName} x {itemWithCount.count} 지급 완료");
            }
            else
            {
                Debug.LogError("ItemData가 null이거나, 로드되지 않았습니다.");
                errorOccurred = true;
            }
        }

        // 정상적으로 아이템이 추가되었다면 저장
        SavePlayerData();

        if (errorOccurred)
        {
            Debug.LogWarning("일부 아이템을 획득하지 못했습니다");
        }
    }

    // 아이템을 지급하고 저장하는 메서드
    public void AddItems(string itemName, int itemCount)
    {
        if (itemDatabase.TryGetValue(itemName, out ItemData itemData))
        {
            var existingItemStack = playerData.inventory.items
                .FirstOrDefault(itemStack => itemStack.itemName == itemName);

            if (existingItemStack != null)
            {
                // 이미 있는 아이템이라면 갯수를 늘림
                existingItemStack.count += itemCount;
            }
            else
            {
                // 인벤토리에 아이템이 없으면 새로 생성
                playerData.inventory.items.Add(new UserInventoryData.ItemStack(itemData.itemName, itemCount));
            }
        }
        else
        {
            Debug.LogError($"{itemName}은 아이템 데이터베이스에 존재하지 않는 이름임");
        }
    }

    // 아이템 데이터베이스를 이용해 초기 아이템 지급
    // 이름은 itemData.name 필드의 그것(LoadItemDatabase 참조)
    private void AddStartingItems()
    {
        // 여기 들어가는 키값이 ItemData.name 값임 / 생성자
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
