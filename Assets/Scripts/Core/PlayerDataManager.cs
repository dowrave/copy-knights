using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    [System.Serializable] 
    private class PlayerData
    {
        //public List<PlayerOperatorData> ownedOperators = new List<PlayerOperatorData>();
    }

    [SerializeField] private List<OperatorData> startingOperators; // 초기 지급 오퍼레이터
    private PlayerData playerData;
    private Dictionary<string, OperatorData> operatorDatabase = new Dictionary<string, OperatorData>();


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

    private void LoadOperatorDatabase()
    {
        var allOperators = Resources.LoadAll<OperatorData>("Operators");
        foreach (var op in allOperators)
        {
            operatorDatabase[op.entityName] = op;
        }
    }

    private void LoadOrCreatePlayerData()
    {
        string savedData = PlayerPrefs.GetString("PlayerData", "");

        // 저장된 정보가 없는 경우 새로 생성
        if (string.IsNullOrEmpty(savedData))
        {
            playerData = new PlayerData();

            foreach (var op in startingOperators)
            {
                //AddOperator(op.entityName);
            }

            SavePlayerData();
        }
        else
        {
            playerData = JsonUtility.FromJson<PlayerData>(savedData);
        }
    }

    //public List<OperatorData> GetOwnedOperators()
    //{
    //    return playerData.ownedOperators
    //        .Select(data => operatorDatabase[data.operatorId])
    //        .ToList();
    //}

    //public void AddOperator(string operatorId)
    //{
    //    if (!playerData.ownedOperators.Any(op => op.operatorId == operatorId))
    //    {
    //        playerData.ownedOperators.Add(new PlayerOperatorData { operatorId = operatorId });
    //        SavePlayerData();
    //    }
    //}

    private void SavePlayerData()
    {
        string jsonData = JsonUtility.ToJson(playerData);
        PlayerPrefs.SetString("PlayerData", jsonData);
        PlayerPrefs.Save();
    }
}
