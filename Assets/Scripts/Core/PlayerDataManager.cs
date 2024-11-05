using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ���θ޴�, �������� �� ��ο��� (�ϴ���) ���
/// �÷��̾ ���� �ִ� ���۷����Ϳ� ���� �������� �ҷ��´�
/// </summary>
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    // �÷��̾ ������ ���۷����� ����
    [System.Serializable] 
    private class PlayerData
    {
        public List<OwnedOperator> ownedOperators = new List<OwnedOperator>();
    }

    private PlayerData playerData;
    private Dictionary<string, OperatorData> operatorDatabase = new Dictionary<string, OperatorData>();

    [Header("�ʱ� ���� ���۷�����")]
    [SerializeField] private List<OperatorData> startingOperators; // �ʱ� ���� ���۷�����


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
    /// ���� ������ ���� "���" OperatorData�� �ҷ��´�
    /// </summary>
    private void LoadOperatorDatabase()
    {
#if UNITY_EDITOR
        // guid = globally identified identifier, ����Ƽ���� �� ���¿� �Ҵ��ϴ� ���� �ĺ���
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

    /// <summary>
    /// PlayerPrefs�� �̿��� ����� �����͸� �ҷ����ų�, ������ ���� �����Ѵ�.
    /// </summary>
    private void LoadOrCreatePlayerData()
    {
        // PlayerPrefs�� ����� PlayerData�� �ҷ����ų� ������ null(�� ĭ)
        //PlayerPrefs.DeleteKey("PlayerData");
        string savedData = PlayerPrefs.GetString("PlayerData", "");

        // ����� ������ ���� ��� ���� ����
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
    /// PlayerPrefs�� Json���� PlayerData�� �����Ѵ�. �� �� ���� ��ġ�� �÷���(������/MAC/�ȵ���̵�/iOS ��) ���� �ٸ���
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
