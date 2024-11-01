
using System.Collections.Generic;
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
        stageDetailPanel.SetActive(true);
        stageTitleText.text = clickedButton.StageData.stageId;
        stageDetailText.text = clickedButton.StageData.stageDetail;
        confirmButton.interactable = true;
    }

    private void OnConfirmButtonClicked()
    {
        if (currentSelectedStageButton != null)
        {
            MainMenuManager.Instance.OnStageSelected(currentSelectedStageButton.StageData);
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
