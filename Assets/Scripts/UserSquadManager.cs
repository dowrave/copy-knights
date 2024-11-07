using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ���� �޴� ������ ���۷����͵��� ���� ����,
/// �������� ������ ���� ����Ʈ�� �����Ѵ�.
/// </summary>
public class UserSquadManager : MonoBehaviour
{
    public static UserSquadManager Instance { get; private set; }

    private List<OperatorData> currentSquad = new List<OperatorData>();
    public event System.Action OnSquadUpdated;

    // ���� ���� ����
    private int editingSlotIndex = -1;

    public int MaxSquadSize { get; } = 6;
    public bool IsEditingSquad => editingSlotIndex != -1; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ���� �ٲ� �ı����� ����
            InitializeSquad();
        }
        else
        {
            Destroy(gameObject);
        }
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
    }

    public void StartEditingSlot(int index)
    {
        if (index >= 0 && index < MaxSquadSize)
        {
            editingSlotIndex = index;
        }
    }

    /// <summary>
    /// ���Կ� ���۷����͸� ���� ��ġ
    /// </summary>
    public void ConfirmOperatorSelection(OperatorData selectedOperator)
    {
        if (IsEditingSquad)
        {
            TryReplaceOperator(editingSlotIndex, selectedOperator);
            editingSlotIndex = -1; // ���� ���� �ʱ�ȭ
        }
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

    public List<OperatorData> GetCurrentSquad()
    {
        return new List<OperatorData>(currentSquad);
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

    public bool TryReplaceOperator(int index, OperatorData newOpData)
    {
        if (index < 0 || index >= MaxSquadSize)
        {
            Debug.LogWarning("��ȿ���� ���� �ε���");
            return false; 
        }

        currentSquad[index] = newOpData;
        OnSquadUpdated?.Invoke();
        return true; 
    }

    public void ClearSquad()
    {
        currentSquad.Clear();
        OnSquadUpdated?.Invoke();
    }
}
