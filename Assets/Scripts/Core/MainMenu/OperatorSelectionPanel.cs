using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class OperatorSelectionPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentContainer;
    [SerializeField] private OperatorSlotButton slotButtonPrefab;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private ScrollRect scrollRect;

    private List<OperatorSlotButton> operatorSlots = new List<OperatorSlotButton>();
    private OperatorSlotButton selectedSlot;

    private void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
        confirmButton.interactable = false;
    }

    private void OnEnable()
    {
        PopulateOperators();
        ResetSelection();
    }


    /// <summary>
    /// ������ ���۷����� ����Ʈ�� ����� ���۷����� ���Ե��� �ʱ�ȭ�մϴ�.
    /// </summary>
    private void PopulateOperators()
    {
        // ���� ����
        ClearSlots();

        // ���� ���� ���۷����� �ε�
        var ownedOperators = PlayerDataManager.Instance.GetOwnedOperators();

        // ���۷����� ���� ���� ����
        foreach (var operatorData in ownedOperators)
        {
            OperatorSlotButton slot = Instantiate(slotButtonPrefab, contentContainer);
            slot.Initialize(true);
            slot.AssignOperator(operatorData);
            slot.OnSlotClicked.AddListener(HandleSlotClicked);
            operatorSlots.Add(slot);
        }
    }

    private void HandleSlotClicked(OperatorSlotButton clickedSlot)
    {
        if (selectedSlot != null) { selectedSlot.SetSelected(false); }

        selectedSlot = clickedSlot;
        selectedSlot.SetSelected(true);
        confirmButton.interactable = true;
    }
    
    /// <summary>
    /// Ȯ�� ��ư Ŭ�� �� ����
    /// </summary>
    private void OnConfirmButtonClicked()
    {
        if (selectedSlot != null && selectedSlot.AssignedOperator != null)
        {
            OperatorSlotButton currentSlot = MainMenuManager.Instance.CurrentEditingSlot;
            if (currentSlot != null)
            {
                currentSlot.AssignOperator(selectedSlot.AssignedOperator);
            }

            // ���ư���
            MainMenuManager.Instance.ShowPanel(MainMenuManager.MenuPanel.SquadEdit);
        }
    }

    /// <summary>
    /// ��� ��ư Ŭ�� �� ����
    /// </summary>
    private void OnCancelButtonClicked()
    {
        MainMenuManager.Instance.ShowPanel(MainMenuManager.MenuPanel.SquadEdit);
    }

    private void ResetSelection()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
        }

        //confirmButton.interactable = false;
    }

    private void ClearSlots()
    {
        foreach (OperatorSlotButton slot in operatorSlots)
        {
            Destroy(slot.gameObject);
        }
        operatorSlots.Clear();
    }

    private void OnDisable()
    {
        ResetSelection();
    }
}
