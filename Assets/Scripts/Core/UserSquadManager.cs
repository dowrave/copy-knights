using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ���� �޴� ������ ���۷����͵��� ���� �����ϰ�,
/// �������� ������ ���� ����Ʈ�� �����Ѵ�.
/// </summary>
public class UserSquadManager : MonoBehaviour
{
    [Header("Squad Settings")]
    [SerializeField] private int maxSquadSize = 6;
    public int MaxSquadSize => maxSquadSize;

    // ���� ���� ������
    private List<OperatorData> currentSquad = new List<OperatorData>();

    // ���� ���� ����
    private int editingSlotIndex = -1;
    public int EditingSlotIndex => editingSlotIndex;
    public bool IsEditingSquad => editingSlotIndex != -1; 

    // �̺�Ʈ
    public event System.Action OnSquadUpdated;
    private void Awake()
    {
        InitializeSquad();
    }

    /// <summary>
    /// �������� �ʱ�ȭ�� ���. MaxSquadSize��ŭ�� null�� �����.
    /// </summary>
    private void InitializeSquad()
    {
        currentSquad = new List<OperatorData>(MaxSquadSize);
        for (int i=0; i < MaxSquadSize; i++)
        {
            currentSquad.Add(null);
        }

        // ����� ������ �����Ͱ� �ִٸ� �ε���
        LoadSquadData();
    }
    

    private void LoadSquadData()
    {
        // PlayerPrefs�� ����� ������ �����͸� �ε�
        string savedData = PlayerPrefs.GetString("SquadData", "");
        if (!string.IsNullOrEmpty(savedData))
        {
            try
            {
                var savedSquad = JsonUtility.FromJson<SavedSquadData>(savedData);
                for (int i = 0; i < savedSquad.operatorIds.Count && i < maxSquadSize; i++)
                {
                    string operatorId = savedSquad.operatorIds[i];
                    if (!string.IsNullOrEmpty(operatorId))
                    {
                        currentSquad[i] = PlayerDataManager.Instance.GetOperatorDataFromDatabase(operatorId);
                    } 
                }
                OnSquadUpdated?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"������ �ε� ���� : {e.Message}");
            }
        }
    }

    private void SaveSquadData()
    {
        try
        {
            // ������ ������ = currentSquad�� �ִ� ���۷����͵��� ��ƼƼ �̸��� ����Ʈ�� ����
            var squadData = new SavedSquadData
            {
                operatorIds = currentSquad
                .Where(op => op != null)
                .Select(op => op.entityName)
                .ToList()
            };

            string jsonData = JsonUtility.ToJson(squadData);
            PlayerPrefs.SetString("SquadData", jsonData);
            PlayerPrefs.Save();
        }

        catch (System.Exception e)
        {
            Debug.LogError($"������ ���� ���� : {e.Message}");
        }
    }

    // ������ ���� ���� �޼���
    public void StartEditingSlot(int index)
    {
        if (index >= 0 && index < MaxSquadSize)
        {
            editingSlotIndex = index;
        }
    }

    public void ConfirmOperatorSelection(OperatorData selectedOperator)
    {
        if (IsEditingSquad)
        {
            TryReplaceOperator(editingSlotIndex, selectedOperator);
            editingSlotIndex = -1; // ���� ���� �ʱ�ȭ
        }

        Debug.Log("���۷����� ��ġ �� ������ Ȯ��");
        for (int i = 0; i < currentSquad.Count(); i++)
        {
            Debug.Log($"{i}��° ���۷����� : {currentSquad[i]}");
        }
    }

    /// <summary>
    /// Squad�� Index�� ���۷����͸� ��ġ/��ü �Ϸ��� �� �� ���
    /// </summary>
    public bool TryReplaceOperator(int index, OperatorData newOpData = null)
    {
        if (index < 0 || index >= MaxSquadSize)
        {
            Debug.LogWarning("��ȿ���� ���� �ε���");
            return false;
        }

        currentSquad[index] = newOpData;
        OnSquadUpdated?.Invoke();
        SaveSquadData();
        return true;
    }

    public void CancelOperatorSelection()
    {
        editingSlotIndex = -1;
    }

    public bool UpdateUserSquad(List<OperatorData> newSquad)
    {
        if (newSquad.Count > MaxSquadSize)
        {
            Debug.LogWarning("���� �������� ũ�Ⱑ �ִ� ũ�⸦ ����!");
            return false; 
        }

        if (newSquad.Count != newSquad.Distinct().Count())
        {
            Debug.LogWarning("�̹� �ߺ��� ����� ����!");
            return false;
        }

        currentSquad = new List<OperatorData>(newSquad); // ���� �� ������� ������ ����
        OnSquadUpdated?.Invoke();
        return true;
    }

    /// <summary>
    /// null�� ���Ե� currentSquad ����Ʈ�� ��ȯ�մϴ�.
    /// </summary>
    public List<OperatorData> GetCurrentSquad()
    {
        return new List<OperatorData>(currentSquad);
    }

    /// <summary>
    /// currentSquad���� null�� �͵��� �����ϰ� ��ȯ�մϴ�.
    /// </summary>
    public List<OperatorData> GetActiveOperators()
    {
        return currentSquad.Where(op => op != null).ToList();
    }



    /// <summary>
    /// ������ ���۷����� ����
    /// </summary>
    public bool TryRemoveOperator(OperatorData opData)
    {
        bool removed = currentSquad.Remove(opData);
        if (removed)
        {
            OnSquadUpdated?.Invoke();
        }
        return removed;
    }


    public void ClearSquad()
    {
        currentSquad.Clear();
        OnSquadUpdated?.Invoke();
    }

    // ����� ������ Ŭ����
    [System.Serializable]
    private class SavedSquadData
    {
        public List<string> operatorIds = new List<string>();
    }

}
