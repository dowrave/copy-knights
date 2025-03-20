using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ���� ������ ������ PlayerDataManager���� �̷�����.
/// ���⼭�� ������ UI ���� ������ ������
/// </summary>
public class UserSquadManager : MonoBehaviour
{
    // ���� ���� ����
    private int editingSlotIndex = -1;
    public int EditingSlotIndex => editingSlotIndex;
    public bool IsEditingSquad => editingSlotIndex != -1; // -1�� �ƴϸ� ���� ���̴ϱ� true

    public int? MaxSquadSize => GameManagement.Instance!.PlayerDataManager.GetMaxSquadSize();

    // �̺�Ʈ
    public event System.Action? OnSquadUpdated;


    private void OnEnable()
    {
        GameManagement.Instance!.PlayerDataManager.OnSquadUpdated += HandleSquadUpdated;
    }

    private void OnDisable()
    {
        GameManagement.Instance!.PlayerDataManager.OnSquadUpdated -= HandleSquadUpdated;
    }

    private void HandleSquadUpdated()
    {
        OnSquadUpdated?.Invoke();
    }

    // ������ ���� ���� �޼���
    public void StartEditingSlot(int index)
    {
        if (index >= 0 && index < MaxSquadSize)
        {
            editingSlotIndex = index;
        }
    }

    public void ConfirmOperatorSelection(OwnedOperator selectedOperator)
    {
        if (IsEditingSquad)
        {
            TryReplaceOperator(editingSlotIndex, selectedOperator);
            editingSlotIndex = -1; // ���� ���� �ʱ�ȭ
        }
    }

    /// Squad�� Index�� ���۷����͸� ��ġ/��ü �Ϸ��� �� �� ���
    public bool TryReplaceOperator(int index, OwnedOperator? newOp = null)
    {
        return GameManagement.Instance!.PlayerDataManager.TryUpdateSquad(index, newOp?.operatorName ?? string.Empty); // operatorName�� null�� ����� ó�� �߰�
    }

    public void CancelOperatorSelection()
    {
        editingSlotIndex = -1;
    }


    public List<OwnedOperator> GetCurrentSquad()
    {
        return GameManagement.Instance!.PlayerDataManager.GetCurrentSquad();
    }

    /// <summary>
    /// null�� ���Ե� currentSquad ����Ʈ�� ��ȯ�մϴ�.
    /// </summary>
    public List<OwnedOperator?> GetCurrentSquadWithNull()
    {
        return GameManagement.Instance!.PlayerDataManager.GetCurrentSquadWithNull();
    }

    // OperatorData�� ����� ����Ʈ�� �ʿ��� ���
    public List<OperatorData> GetCurrentSquadData()
    {
        return GameManagement.Instance!.PlayerDataManager.GetCurrentSquadData();
    }
}
