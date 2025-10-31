using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


public class SquadEditPanel : MonoBehaviour
{
    [Header("UI References")]
    //[SerializeField] private Transform operatorSlotsContainer = default!;
    [SerializeField] private List<OperatorSlot> operatorSlots = new List<OperatorSlot>();
    [SerializeField] private Button startButton = default!;

    private void Awake()
    {
        startButton.onClick.AddListener(OnStartButtonClicked);
    }

    private void Start()
    {
        InitializePanel();
        UpdateSquadUI();
        UpdateEnterButtonState();
    }

    private void InitializePanel()
    {
       // ���� �ʱ�ȭ
       for (int i = 0; i < operatorSlots.Count; i++)
        {
            OperatorSlot slot = operatorSlots[i];

            bool isActiveSlot = i < GameManagement.Instance!.UserSquadManager.MaxSquadSize;
            slot.Initialize(isActiveSlot);

            int slotIndex = i;  // �̷� ������ �����ؼ� ������ ����� - i�� ���� ������ ������ �ִܴ�.
            // OnSlotClicked�� i�� ���� slot�� ������ ����Ǵ� �������� ���� ������ i������ �ν��ϴ� �̽��� �ִٰ� ��
            // �̷� �����ʿ� �̺�Ʈ �߰��ϴ� ��Ȳ������ �����ϸ� �� ��.

            if (isActiveSlot)
            { 
                slot.OnSlotClicked.RemoveAllListeners();
                slot.OnSlotClicked.AddListener((clickedSlot) => OnSlotClicked(clickedSlot, slotIndex));
            }
        }
    }

    private void OnEnable()
    {
        GameManagement.Instance!.PlayerDataManager.OnSquadUpdated += UpdateSquadUI;
        InitializePanel();
        UpdateSquadUI();
    }


    private void UpdateSquadUI()
    {
        List<SquadOperatorInfo?> currentSquad = GameManagement.Instance!.PlayerDataManager.GetCurrentSquadWithNull();
        if (currentSquad == null) return; // ���� ������ ������ ������ ���� �ߴ�

        for (int i = 0; i < operatorSlots.Count; i++)
        {
            OperatorSlot slot = operatorSlots[i];
            bool isActiveSlot = i < GameManagement.Instance!.UserSquadManager.MaxSquadSize;

            if (isActiveSlot)
            {
                if (currentSquad[i] != null)
                {
                    // ���۷����� �Ҵ� ����
                    slot.AssignOperator(currentSquad[i].op);
                }
                else
                {
                    // �� ����
                    slot.InitializeEmptyOrDisabled(true);
                }
            }
            else
            {
                // ��� �Ұ��� ����
                slot.InitializeEmptyOrDisabled(false);
            }
        }

        UpdateEnterButtonState();
    }


    // OperatorSlot ��ư Ŭ�� �� OpeatorInventoryPanel�� �Ѿ
    private void OnSlotClicked(OperatorSlot clickedSlot, int slotIndex)
    {
        Logger.Log($"squadEditPanel ���� Ŭ�� : {clickedSlot}, {slotIndex}");

        // ���� ���� ���� �ε��� ����
        GameManagement.Instance!.UserSquadManager.StartEditingSlot(slotIndex);
        MainMenuManager.Instance!.FadeInAndHide(MainMenuManager.Instance!.PanelMap[MainMenuManager.MenuPanel.OperatorInventory], gameObject);
    }

    private void OnStartButtonClicked()
    {
        // � �������������� MainMenuManager���� ���� ��
        MainMenuManager.Instance!.StartStage();
    }

    /// <summary>
    /// Ȱ��ȭ�� ���� �߿��� ���۷����Ͱ� ��ġ�� ������ ��
    /// </summary>
    private int GetDeployedOperatorCount()
    {
        int count = 0;
        for (int i = 0; i < GameManagement.Instance!.UserSquadManager.MaxSquadSize; i++)
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
        startButton.interactable = GetDeployedOperatorCount() > 0;
    }

    private void OnDisable()
    {
        GameManagement.Instance!.PlayerDataManager.OnSquadUpdated -= UpdateSquadUI;
    }
}
