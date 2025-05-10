using System;
using System.Collections.Generic;
using System.Linq;
using Skills.Base;
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

    public int MaxSquadSize => GameManagement.Instance!.PlayerDataManager.GetMaxSquadSize();

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

    public void ConfirmOperatorSelection(OwnedOperator selectedOperator, int skillIndex)
    {
        if (IsEditingSlot)
        {
            TryReplaceOperator(editingSlotIndex, selectedOperator, skillIndex);
            editingSlotIndex = -1; // ���� ���� �ʱ�ȭ
        }
    }

    /// �����忡 ���۷����͸� ��ġ/��ü �Ϸ��� �� �� ���
    public bool TryReplaceOperator(int squadIndex, OwnedOperator? newOp = null, int skillIndex = 0)
    {
        // �ش� �ε��� �ʱ�ȭ
        if (newOp == null)
        {
            skillIndex = -1;
            return GameManagement.Instance!.PlayerDataManager.TryUpdateSquad(squadIndex, string.Empty, skillIndex);
        }
        // �ش� �ε����� ���۷����� ����
        else
        {
            if (skillIndex < 0 || skillIndex >= newOp.UnlockedSkills.Count)
                throw new InvalidOperationException("TryReplaceOperator�� ��ų ������ �̻���");
            return GameManagement.Instance!.PlayerDataManager.TryUpdateSquad(squadIndex, newOp.operatorName, skillIndex);
        }
    }

    public void SetIsBulkEditing(bool state)
    {
        _isEditingBulk = state; 
    }

    public void CancelOperatorSelection()
    {
        editingSlotIndex = -1;
    }

    // �����带 �ʱ�ȭ�մϴ�.
    public void ClearSquad()
    {
        GameManagement.Instance!.PlayerDataManager.ClearSquad();
    }

    public List<SquadOperatorInfo> GetCurrentSquad()
    {
        return GameManagement.Instance!.PlayerDataManager.GetCurrentSquad();
    }

    /// <summary>
    /// null�� ���Ե� currentSquad ����Ʈ�� ��ȯ�մϴ�.
    /// </summary>
    public List<SquadOperatorInfo?> GetCurrentSquadWithNull()
    {
        return GameManagement.Instance!.PlayerDataManager.GetCurrentSquadWithNull();
    }

    // OperatorData�� ����� ����Ʈ�� �ʿ��� ���
    public List<OperatorData> GetCurrentSquadData()
    {
        return GameManagement.Instance!.PlayerDataManager.GetCurrentSquadData();
    }

    public bool UpdateFullSquad(List<SquadOperatorInfo> ownedOpInfoSquad)
    {
        return GameManagement.Instance!.PlayerDataManager.UpdateFullSquad(ownedOpInfoSquad);
    }

    // ���� �����忡�� ���۷����Ϳ� ������ ��ų �ε����� ��ȯ�մϴ�.
    public int GetCurrentSkillIndex(OwnedOperator op)
    {
        List<SquadOperatorInfo> currentSquad = GetCurrentSquad();
        SquadOperatorInfo targetOpInfo = currentSquad.FirstOrDefault(member => member.op == op);
        if (targetOpInfo != null) return targetOpInfo.skillIndex;

        return -1;
    }
}

public class SquadOperatorInfo
{
    public OwnedOperator op;
    public int skillIndex;

    // ������
    public SquadOperatorInfo()
    {
        op = null;
        skillIndex = -1;
    }

    public SquadOperatorInfo(OwnedOperator op, int index)
    {
        this.op = op;
        skillIndex = index;
    }
}