using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


public class SquadEditPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform operatorSlotsContainer;
    [SerializeField] private List<OperatorSlotButton> operatorSlots = new List<OperatorSlotButton>();
    [SerializeField] private Button enterStageButton;
    //[SerializeField] private OperatorSelectionPanel operatorSelectionPanel;

    // ���߿� ������������ ������ �޾ƿ͵� �ǰڴ�.
    private const int ACTIVE_OPERATOR_SLOTS = 6;

    private OperatorSlotButton selectedSlot;
    private List<OperatorData> currentSquad = new List<OperatorData>();

    private void Start()
    {
        InitializePanel();
        UpdateEnterButtonState();
    }

    private void InitializePanel()
    {
       // ���Ժ� Ÿ�� ����
       for (int i = 0; i < operatorSlots.Count; i++)
        {
            OperatorSlotButton slot = operatorSlots[i];
            
            // 1. Ȱ��ȭ
            if (i < ACTIVE_OPERATOR_SLOTS)
            {
                slot.Initialize(true);
            }
            else
            {
                slot.Initialize(false);
            }
        }
    }

    private void HandleStartButtonClicked()
    {

    }

    /// <summary>
    /// Ȱ��ȭ�� ���� �߿��� ���۷����Ͱ� ��ġ�� ������ ��
    /// </summary>
    /// <returns></returns>
    private int GetDeployedOperatorCount()
    {
        int count = 0;
        for (int i = 0; i < ACTIVE_OPERATOR_SLOTS; i++)
        {
            if (!operatorSlots[i].IsEmpty())
            {
                count++;
            }
        }
        return count;
    }

    private void UpdateEnterButtonState()
    {
        enterStageButton.interactable = GetDeployedOperatorCount() > 0;
    }
}
