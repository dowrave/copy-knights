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
    // ���� �ε��� ���� ���� ����
    private int editingSlotIndex = -1;
    public int EditingSlotIndex => editingSlotIndex;
    public bool IsEditingSlot => editingSlotIndex != -1; // -1�� �ƴϸ� ���� ���̴ϱ� true

    // ��ü �ε��� ���� ���� ����
    private bool _isEditingBulk;
    public bool IsEditingBulk => _isEditingBulk;

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
        if (IsEditingSlot)
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

    public void SetIsBulkEditing(bool state)
    {
        _isEditingBulk = state; 
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
