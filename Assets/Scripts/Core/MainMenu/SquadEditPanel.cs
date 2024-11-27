using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


public class SquadEditPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform operatorSlotsContainer;
    [SerializeField] private List<OperatorSlot> operatorSlots = new List<OperatorSlot>();
    [SerializeField] private Button startButton;

    private List<OperatorData> currentSquad = new List<OperatorData>();
    private GameObject stageSelectPanel;
    private GameObject operatorListPanel;

    private void Awake()
    {
        startButton.onClick.AddListener(HandleStartButtonClicked);
    }

    private void Start()
    {
        InitializePanel();
        UpdateEnterButtonState();
    }

    private void InitializePanel()
    {
       // ���� �ʱ�ȭ
       for (int i = 0; i < operatorSlots.Count; i++)
        {
            int slotIndex = i;  // �̷� ������ �����ؼ� ������ ����� - i�� ���� ������ ������ �ִܴ�.
            OperatorSlot slot = operatorSlots[i];

            bool isActiveSlot = i < GameManagement.Instance.UserSquadManager.MaxSquadSize;
            slot.Initialize(isActiveSlot);

            if (isActiveSlot)
            {
                slot.OnSlotClicked.AddListener((clickedSlot) => HandleSlotClicked(clickedSlot, slotIndex));
            }
        }

        // �����带 �����ͼ� UI�� �Ҵ�
        UpdateSquadUI();
    }

    private void OnEnable()
    {
        UpdateSquadUI();
    }

    private void UpdateSquadUI()
    {
        List<OwnedOperator> currentSquad = GameManagement.Instance.PlayerDataManager.GetCurrentSquadWithNull();
        if (currentSquad.Count == 0) return; // ���� ������ ������ ������ ���� �ߴ�

        for (int i = 0; i < operatorSlots.Count; i++)
        {
            OperatorSlot slot = operatorSlots[i];
            bool isActiveSlot = i < GameManagement.Instance.UserSquadManager.MaxSquadSize;

            if (isActiveSlot)
            {
                if (currentSquad[i] != null)
                {
                    // ���۷����� �Ҵ� ����
                    slot.AssignOperator(currentSquad[i]);
                }
                else
                {
                    // �� ����
                    slot.SetEmptyOrDisabled(true);
                }
            }
            else
            {
                // ��� �Ұ��� ����
                slot.SetEmptyOrDisabled(false);
            }
        }

        UpdateEnterButtonState();
    }

    /// <summary>
    /// OperatorSlot ��ư Ŭ�� �� OpeatorListPanel�� �Ѿ
    /// </summary>
    private void HandleSlotClicked(OperatorSlot clickedSlot, int slotIndex)
    {
        // ���� ���� ���� �ε��� ����
        GameManagement.Instance.UserSquadManager.StartEditingSlot(slotIndex);
        MainMenuManager.Instance.FadeInAndHide(MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.OperatorInventory], gameObject);
    }

    private void HandleStartButtonClicked()
    {
        // � �������������� MainMenuManager���� ���� ��
        MainMenuManager.Instance.StartStage();
    }

    /// <summary>
    /// Ȱ��ȭ�� ���� �߿��� ���۷����Ͱ� ��ġ�� ������ ��
    /// </summary>
    private int GetDeployedOperatorCount()
    {
        int count = 0;
        for (int i = 0; i < GameManagement.Instance.UserSquadManager.MaxSquadSize; i++)
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
}
