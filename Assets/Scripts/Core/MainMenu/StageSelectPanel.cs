
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class StageSelectPanel : MonoBehaviour
{
    [SerializeField] List<StageButton> stageButtons;
    [SerializeField] private GameObject stageDetailPanel;
    [SerializeField] private TextMeshProUGUI stageTitleText;
    [SerializeField] private TextMeshProUGUI stageDetailText;
    [SerializeField] private Button confirmButton;

    private StageButton currentSelectedStageButton;

    private void Start()
    {
        InitializeButtons();
        InitializeDetailPanel();
    }

    private void InitializeButtons()
    {
        foreach (StageButton button in stageButtons)
        {
            button.onClick.AddListener(OnStageButtonClicked);
        }
    }

    private void InitializeDetailPanel()
    {
        stageDetailPanel.SetActive(false);
        confirmButton.onClick.AddListener(() => OnConfirmButtonClicked());
        confirmButton.interactable = false;
    }

    public StageData GetStageDataById(string stageId)
    {
        var targetButton = stageButtons.FirstOrDefault(btn => btn.StageData.stageId == stageId);

        if (targetButton != null)
        {
            OnStageButtonClicked(targetButton);
            ShowDetailPanel(targetButton.StageData);
            return targetButton.StageData;
        }

        return null;
    }

    /// <summary>
    /// ��ư Ŭ������ ������ ���� ������Ʈ Panel���� ������
    /// </summary>
    private void OnStageButtonClicked(StageButton clickedButton)
    {
        if (currentSelectedStageButton != null)
        {
            currentSelectedStageButton.SetSelected(false);
        }

        // ���ο� ���� ó��
        currentSelectedStageButton = clickedButton;
        currentSelectedStageButton.SetSelected(true);

        // DetailPanel ������Ʈ
        ShowDetailPanel(currentSelectedStageButton.StageData);
    }

    public void ShowDetailPanel(StageData stageData)
    {
        if (stageDetailPanel != null)
        {
            stageDetailPanel.SetActive(true);
            stageTitleText.text = stageData.stageId;
            stageDetailText.text = stageData.stageDetail;
            confirmButton.interactable = true;
        }
    }


    private void OnConfirmButtonClicked()
    {
        if (currentSelectedStageButton != null)
        {
            MainMenuManager.Instance.SetSelectedStage(currentSelectedStageButton.StageData);

            GameObject squadEditPanel = MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.SquadEdit];
            GameObject stageSelectPanel = MainMenuManager.Instance.PanelMap[MainMenuManager.MenuPanel.StageSelect];

            // squadEditPanel�� �����ְ� stageSelectPanel�� ����
            MainMenuManager.Instance.FadeInAndHide(squadEditPanel, stageSelectPanel);
        }
    }

    /// <summary>
    /// �г� ��Ȱ��ȭ �� ���� �ʱ�ȭ
    /// </summary>
    private void OnDisable()
    {
        if (currentSelectedStageButton != null)
        {
            currentSelectedStageButton.SetSelected(false);
            currentSelectedStageButton = null;
        }

        stageDetailPanel.SetActive(false);
        confirmButton.interactable = false;
    }
}
