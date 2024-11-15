using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;


public class SquadEditPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform operatorSlotsContainer;
    [SerializeField] private List<OperatorSlot> operatorSlots = new List<OperatorSlot>();
    [SerializeField] private Button startButton;
    [SerializeField] private Button backButton;

    private List<OperatorData> currentSquad = new List<OperatorData>();
    private GameObject stageSelectPanel;
    private GameObject operatorListPanel;

    private void Awake()
    {
        startButton.onClick.AddListener(HandleStartButtonClicked);
        backButton.onClick.AddListener(HandleBackButtonClicked);
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
            int slotIndex = i; // Ŭ������ ���� ���� ������ ����
            OperatorSlot slot = operatorSlots[i];

            bool isActiveSlot = i < GameManagement.Instance.UserSquadManager.MaxSquadSize;
            slot.Initialize(isActiveSlot);

            if (isActiveSlot)
            {
                // Ŭ������ ������� �ʰ� i�� ���� ������ ���� �Ŀ��� ��� �����ʰ� ������ i���� �����ϰ� �ȴ�
                // Ŭ������ ���� �ݺ����� ������ ���ο� ���� slotIndex�� �� �����ʰ� �ڽŸ��� �ε����� ����ϰ� �ȴ�.
                slot.OnSlotClicked.AddListener((clickedSlot) => HandleSlotClicked(clickedSlot, slotIndex));
            }
        }

        // �����带 �����ͼ� UI�� �Ҵ�
        UpdateSquadUI();
    }

    private void OnEnable()
    {
        UpdateSquadUI();
        //UpdateVisuals();
    }

    private void UpdateSquadUI()
    {
        List<OperatorData> currentSquad = GameManagement.Instance.UserSquadManager.GetCurrentSquad();

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

    private void HandleBackButtonClicked()
    {
        MainMenuManager.Instance.ActivateAndFadeOut(MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.StageSelect], gameObject);
    }

    /// <summary>
    /// Ȱ��ȭ�� ���� �߿��� ���۷����Ͱ� ��ġ�� ������ ��
    /// </summary>
    /// <returns></returns>
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
