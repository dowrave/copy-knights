using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 메인메뉴, 스테이지 씬 모두에서 (일단은) 사용
/// 플레이어가 갖고 있는 오퍼레이터에 대한 정보들을 불러온다
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    // 플레이어가 소유한 오퍼레이터 정보
    [System.Serializable] 
    private class PlayerData
    {
        public List<OwnedOperator> ownedOperators = new List<OwnedOperator>();
    }

    private PlayerData playerData;
    private Dictionary<string, OperatorData> operatorDatabase = new Dictionary<string, OperatorData>();

    [Header("초기 지급 오퍼레이터")]
    [SerializeField] private List<OperatorData> startingOperators; // 초기 지급 오퍼레이터


    private void Awake()
    {
        if ( Instance == null )
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSystem();
        }
        else
        {
            Destroy(gameObject);
        }
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
            playerData = new PlayerData();

            foreach (var op in startingOperators)
            {
                AddOperator(op.entityName);
            }

            SavePlayerData();
        }
        else
        {
            playerData = JsonUtility.FromJson<PlayerData>(savedData);
        }
    }

    public List<OperatorData> GetOwnedOperators()
    {
        return playerData.ownedOperators
            .Select(data => operatorDatabase[data.operatorId])
            .ToList();
    }

    public void AddOperator(string operatorId)
    {
        if (!playerData.ownedOperators.Any(op => op.operatorId == operatorId))
        {
            playerData.ownedOperators.Add(new OwnedOperator { operatorId = operatorId });
            SavePlayerData();
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

    public OperatorData GetOperatorDataFromDatabase(string operatorId)
    {
        return operatorDatabase[operatorId];
    }
}
