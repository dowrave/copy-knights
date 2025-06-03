using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

// 사용자의 데이터(보유 오퍼레이터, 스쿼드 등등)를 관리한다.
// GameManagement의 하위 오브젝트
public class PlayerDataManager : MonoBehaviour
{
    // 플레이어가 소유한 데이터 정보
    [Serializable]
    private class PlayerData
    {
        public List<OwnedOperator> ownedOperators = new List<OwnedOperator>(); // 보유 오퍼레이터
        //public List<string> currentSquadOperatorNames = new List<string>(); // 스쿼드, 직렬화를 위해 이름만 저장
        public List<SquadOperatorInfoForSave> currentSquad = new List<SquadOperatorInfoForSave>();
        public int maxSquadSize;
        public UserInventoryData inventory = new UserInventoryData(); // 아이템 인벤토리
        public StageResultData stageResults = new StageResultData(); // 스테이지 진행 상황
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

    [SerializeField] private List<OperatorData> startingOperators = new List<OperatorData>(); //  nullable 경고문 회피: 할당이 없으면 빈 리스트
    [SerializeField] private int defaultMaxSquadSize = 7;

    private Dictionary<string, ItemData> itemDatabase = new Dictionary<string, ItemData>();

    public List<OwnedOperator> OwnedOperators => playerData.ownedOperators;

    public event Action OnSquadUpdated = delegate { }; // nullable 경고문 회피


    private void Awake()
    {
        ResetPlayerData(); // 디버깅용
        InitializeSystem();
        
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
        InstanceValidator.ValidateInstance(startingOperators);


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
                Debug.LogError($"{path}에서 OperatorData 로드 실패");
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
                maxSquadSize = defaultMaxSquadSize
            };

            // 튜토리얼 상태 초기화. 현재 TutorialData의 갯수는 3개. 
            for (int i = 0; i < 3; i++)
            {
                playerData.tutorialDataStatus.Add(TutorialStatus.NotStarted);
            }

            // 초기 오퍼레이터를 ownedOperators에 추가
            if (startingOperators != null)
            {
                foreach (var op in startingOperators)
                {
                    AddOperator(op.entityName!);
                }
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
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;  

        safePlayerData.currentSquad.Clear();
        for (int i = 0; i < safePlayerData.maxSquadSize; i++)
        {
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
                .Take(safePlayerData.maxSquadSize) // 처음부터 지정된 수만큼 가져오는 메서드
                .ToList() ?? new List<SquadOperatorInfoForSave>();
        }

        SavePlayerData();
    }

    // operatorName에 해당하는 OwnedOperator을 얻는다
    public OwnedOperator? GetOwnedOperator(string operatorName)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        return safePlayerData.ownedOperators.Find(op => op.operatorName == operatorName) ?? null;
    }


    private void TestAboutTutorial()
    {
        // 테스트: TutorialManager의 2번째 튜토리얼까지 클리어한 상태 시뮬레이션
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        // 튜토리얼 진행 상태 설정: 첫 번째와 두 번째 튜토리얼 완료, 세 번째는 NotStarted
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

        // 전체 튜토리얼 완료 여부는 아직 아님
        safePlayerData.isTutorialFinished = false;
        SavePlayerData();
    }

    // 유저가 오퍼레이터를 보유하게 함
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
        }
    }


    // PlayerPrefs에 Json으로 PlayerData를 저장한다.
    public void SavePlayerData()
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        string jsonData = JsonUtility.ToJson(safePlayerData);

        // Dictionary 직렬화를 위한 Newtonsoft.Json 사용
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

    // 새로 만들 메서드 : <OwnedOperator, skillIndex>를 갖는 리스트를 반환함. 런타임에서만 사용.
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
            .Where(runtimeInfo => runtimeInfo != null) // null인 항목들 제외
            .Select(runtimeInfo => runtimeInfo!) // nullability 경고 제거 목적
            .ToList() ?? new List<SquadOperatorInfo>();
    }

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
                    return null;
                }
                else
                {
                    OwnedOperator ownedOp = GetOwnedOperator(savedInfo.operatorName);

                    if (ownedOp != null) return new SquadOperatorInfo(ownedOp, savedInfo.skillIndex);
                    else
                    {
                        return null;
                    }
                }
            })
            .ToList();
    }

    // UI에 쓸 수도 있는 OperatorData 리스트 반환
    public List<OperatorData> GetCurrentSquadData()
    {
        return GetCurrentSquad()
            .Select(opInfo => opInfo.op.OperatorProgressData)
            .Where(op => op != null)
            .ToList();
    }

    // 스쿼드를 업데이트한다
    public bool TryUpdateSquad(int squadIndex, string operatorName, int skillIndex)
    {
        // 1. 저장된 스쿼드 정보를 불러옴
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;
        List<SquadOperatorInfoForSave> squadForSave = safePlayerData.currentSquad;
        if (squadIndex < 0 || squadIndex >= safePlayerData.maxSquadSize) return false;


        // 2. 스쿼드 크기 확보
        while (squadForSave.Count <= squadIndex)
        {
            squadForSave.Add(new SquadOperatorInfoForSave());
        }

        // 3. 오퍼레이터를 넣는 경우, 지금 보유했는지 + 스쿼드에 중복된 오퍼레이터가 없는지 점검함
        // 빈 슬롯으로 대체하려는 경우는 이 조건들을 무시
        if (operatorName != string.Empty)
        {
            // 오퍼레이터 소유 확인
            if (!string.IsNullOrEmpty(operatorName) && !safePlayerData.ownedOperators.Any(op => op.operatorName == operatorName)) return false;

            // 같은 이름의 오퍼레이터 중복 방지 : 단, 스킬 인덱스가 다른 경우는 허용함
            if (squadForSave.Any(opInfo => opInfo != null && opInfo.operatorName == operatorName && opInfo.skillIndex == skillIndex)) return false;
        }

        // 4. 등록
        squadForSave[squadIndex] = new SquadOperatorInfoForSave(operatorName, skillIndex);

        // 디버깅 - 스쿼드 수정마다 반복문 실행해서 점검
        // for (int i=0; i < squadForSave.Count; i++)
        // {
        //     Debug.Log($"스쿼드 구성 : {i}번째 인덱스 - ({squadForSave[i].operatorName}, 스킬 인덱스 : {squadForSave[i].skillIndex})");
        // }

        SavePlayerData();
        OnSquadUpdated?.Invoke();
        return true;
    }

    // 일괄 업데이트용
    public bool UpdateFullSquad(List<SquadOperatorInfo> tempOwnedOpSquad)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;
        var newSquadForSave = new List<SquadOperatorInfoForSave>(safePlayerData.maxSquadSize);

        // 1. newSquadForSave 내용을 채움
        for (int i = 0; i < safePlayerData.maxSquadSize; i++)
        {
            if (i < tempOwnedOpSquad.Count)
            {
                SquadOperatorInfo runtimeInfo = tempOwnedOpSquad[i];
                if (runtimeInfo == null || runtimeInfo.op == null)
                {
                    // 빈 슬롯 처리
                    newSquadForSave.Add(new SquadOperatorInfoForSave()); // 기본값
                }
                else
                {
                    string opName = runtimeInfo.op.operatorName;
                    int skillIdx = runtimeInfo.skillIndex;

                    if (!string.IsNullOrEmpty(opName) &&
                        (safePlayerData.ownedOperators == null || !safePlayerData.ownedOperators.Any(ownedOp => ownedOp.operatorName == opName)))
                    {
                        // 소유하지 않은 오퍼레이터가 있으면 업데이트 실패
                        Debug.LogError($"{opName}은 소유하고 있는 오퍼레이터가 아님");
                        return false;
                    }

                    // 빈 슬롯
                    if (string.IsNullOrEmpty(opName))
                    {
                        skillIdx = -1;
                    }

                    newSquadForSave.Add(new SquadOperatorInfoForSave(opName, skillIdx));
                }
            }
            else
            {
                // 비어있는 나머지 슬롯은 빈 슬롯으로 채움
                newSquadForSave.Add(new SquadOperatorInfoForSave());
            }
        }
        
        // 2. 실제 스쿼드 데이터 교체
        safePlayerData.currentSquad = newSquadForSave;
        SavePlayerData();
        OnSquadUpdated?.Invoke();
        return true;
    }


    // 스쿼드를 초기화한다
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
        // InitializeForTest();
        GameManagement.Instance!.TestManager.InitializeForTest();
    }

    public int GetMaxSquadSize() => playerData!.maxSquadSize;


    // 특정 인덱스 슬롯의 오퍼레이터를 반환한다. 비어 있거나 활성화가 안된 슬롯이면 null을 반환한다. 해도 되나?
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

        // 모든 아이템 수량 검증
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

    // 어떤 아이템이 몇 개 있는지 확인하는 메서드. 있는지 여부도 체크 가능.
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

    public bool IsStageCleared(string stageId)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        return safePlayerData.stageResults.clearedStages.Any(info => info.stageId == stageId);
    }

    // 특정 스테이지의 클리어 정보를 가져오며, 없으면 null을 반환한다.
    public StageResultData.StageResultInfo? GetStageResultInfo(string stageId)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        return safePlayerData.stageResults.clearedStages.FirstOrDefault(info => info.stageId == stageId);
    }

    // 스테이지 클리어 기록하기
    public void RecordStageResult(string stageId, int stars)
    {
        InstanceValidator.ValidateInstance(playerData);
        var safePlayerData = playerData!;

        var existingClear = GetStageResultInfo(stageId);
        if (existingClear != null)
        {
            if (existingClear.stars >= stars) return;

            // 더 좋은 기록을 냈을 경우, 기존 기록 제거
            safePlayerData.stageResults.clearedStages.Remove(existingClear);
        }

        var newClearInfo = new StageResultData.StageResultInfo(stageId, stars);
        safePlayerData.stageResults.clearedStages.Add(newClearInfo);

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
        GrantItems(firstClearRewards);
        GrantItems(basicClearRewards);

        SavePlayerData();
    }

    // 사용자에게 아이템을 지급
    private void GrantItems(List<ItemWithCount> rewardItems)
    {
        foreach (ItemWithCount itemWithCount in rewardItems)
        {
            if (itemWithCount.itemData != null)
            {
                // 비율이 반영된 아이템 지급 갯수
                AddItems(itemWithCount.itemData.itemName!, itemWithCount.count);
            }
            else
            {
                throw new InvalidOperationException("아이템이 정상적으로 지급되지 않는 현상 발생");
            }
        }
    }

    // 아이템을 실질적으로 저장하는 메서드
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
                // 이미 있는 아이템이라면 갯수를 늘림
                existingItemStack.count += itemCount;
            }
            else
            {
                // 인벤토리에 아이템이 없으면 새로 생성
                safePlayerData.inventory.items.Add(new UserInventoryData.ItemStack(itemData.itemName, itemCount));
            }
        }
        else
        {
            Debug.LogError($"{itemName}은 아이템 데이터베이스에 존재하지 않는 이름임");
        }
    }

    // 오버로드 메서드
    public void AddItems(Dictionary<ItemData, int> itemsDict)
    {
        foreach (var kvp in itemsDict)
        {
            AddItems(kvp.Key.itemName, kvp.Value);
        }
    }

    // 튜토리얼 완료 상태 확인
    public bool IsAllTutorialFinished()
    {
        return playerData!.isTutorialFinished;
    }

    // 가장 마지막으로 클리어된 튜토리얼 인덱스
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

    // 튜토리얼 상태를 설정하는 메서드
    public void SetTutorialStatus(int tutorialIndex, TutorialStatus status)
    {
        playerData.tutorialDataStatus[tutorialIndex] = status;
        SavePlayerData();
    }

    // 튜토리얼 상태를 확인하는 메서드
    public bool IsTutorialStatus(int tutorialIndex, TutorialStatus status)
    {
        return playerData.tutorialDataStatus[tutorialIndex] == status;
    }

    // 모든 튜토리얼 진행
    public void FinishAllTutorials()
    {
        playerData.isTutorialFinished = true;

        // 모든 튜토리얼을 완료 상태로 설정
        for (int i = 0; i < 3; i++)
        {
            playerData.tutorialDataStatus[i] = TutorialStatus.Completed;
        }

        SavePlayerData();
    }

    // 정예화에 필요한 아이템들이 모두 있는지 검사하는 메서드
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

    public ItemData GetItemData(string itemName)
    {
        return itemDatabase[itemName];
    }
}

[Serializable]
public class SquadOperatorInfoForSave
{
    public string operatorName;
    public int skillIndex;

    // 생성자
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
