using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 사용자의 데이터(보유 오퍼레이터, 스쿼드 등등)를 관리한다.
/// GameManagement의 하위 오브젝트임에 유의
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    // 플레이어가 소유한 오퍼레이터 정보
    [System.Serializable] 
    private class PlayerData
    {
        public List<OwnedOperator> ownedOperators = new List<OwnedOperator>();
        public List<string> currentSquadOperatorNames = new List<string>(); // 직렬화의 용이성, 저장 공간 저장 등의 이유로 string만을 사용
        public int maxSquadSize;
    }

    private PlayerData playerData;
    private Dictionary<string, OperatorData> operatorDatabase = new Dictionary<string, OperatorData>();

    [Header("초기 지급 오퍼레이터")]
    [SerializeField] private List<OperatorData> startingOperators; // 초기 지급 오퍼레이터

    [SerializeField] private int defaultMaxSquadSize = 6;

    public event System.Action OnSquadUpdated;


    private void Awake()
    {
        ResetPlayerData();
        InitializeSystem();
    }

    private void InitializeSystem()
    {
        LoadOperatorDatabase(); 
        LoadOrCreatePlayerData();
    }

    /// <summary>
    /// 현재 게임이 가진 "모든" OperatorData를 불러온다
    /// </summary>
    private void LoadOperatorDatabase()
    {
#if UNITY_EDITOR
        // guid = globally identified identifier, 유니티에서 각 에셋에 할당하는 고유 식별자
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

    /// <summary>
    /// PlayerPrefs를 이용해 저장된 데이터를 불러오거나, 없으면 새로 생성한다.
    /// </summary>
    private void LoadOrCreatePlayerData()
    {
        // PlayerPrefs에 저장된 PlayerData를 불러오거나 없으면 null(빈 칸)
        //PlayerPrefs.DeleteKey("PlayerData");
        string savedData = PlayerPrefs.GetString("PlayerData", "");

        // 저장된 정보가 없는 경우 새로 생성
        if (string.IsNullOrEmpty(savedData))
        {
            playerData = new PlayerData
            {
                maxSquadSize = defaultMaxSquadSize
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
        if (playerData.currentSquadOperatorNames == null)
        {
            playerData.currentSquadOperatorNames = new List<string>();
        }

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
    
    /// <summary>
    /// 유저가 오퍼레이터를 보유하게 함
    /// </summary>
    public void AddOperator(string operatorName)
    {
        if (!playerData.ownedOperators.Any(op => op.operatorName == operatorName))
        {
            OwnedOperator newOp = new OwnedOperator
            {
                operatorName = operatorName,
                currentLevel = 1,
                currentPhase = OperatorGrowthSystem.ElitePhase.Elite0,
                currentExp = 0,
                selectedSkillIndex = 0
            };
            playerData.ownedOperators.Add(newOp);
            SavePlayerData();
            Debug.Log($"{newOp.operatorName}가 정상적으로 ownedOperator에 등록되었습니다");
        }
    }

    /// <summary>
    /// PlayerPrefs에 Json으로 PlayerData를 저장한다. 이 때 저장 위치는 플랫폼(윈도우/MAC/안드로이드/iOS 등) 별로 다르다
    /// </summary>
    private void SavePlayerData()
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

    /// <summary>
    /// null을 포함하지 않은 실제 배치된 오퍼레이터만 포함된 스쿼드 리스트 반환
    /// </summary>
    public List<OwnedOperator> GetCurrentSquad()
    {
        return playerData.currentSquadOperatorNames
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(opName => GetOwnedOperator(opName))
            .Where(op => op != null)
            .ToList();
    }

    /// <summary>
    /// null을 포함한 전체 스쿼드 리스트 반환. MaxSquadSize가 보장된다.
    /// </summary>
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


    /// <summary>
    /// 스쿼드를 업데이트한다
    /// </summary>
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

    /// <summary>
    /// 스쿼드를 초기화한다
    /// </summary>
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

    /// <summary>
    /// 특정 인덱스 슬롯의 오퍼레이터를 반환한다. 비어 있거나 활성화가 안된 슬롯이면 null을 반환한다. 해도 되나?
    /// </summary>
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

}
