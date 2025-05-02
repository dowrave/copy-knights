
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class StageUIData
{
    public string stageId = string.Empty;
    public StageButton stageButton = default!;
    public Image lineImage = default!; 
}

public class StageSelectPanel : MonoBehaviour
{
    [SerializeField] private List<StageUIData> stageUIDataList = new List<StageUIData>();

    [Header("Side Panel Elements")]
    [SerializeField] private Button cancelArea = default!; // buttonContainer�� �� ������ ��
    [SerializeField] private GameObject stageDetailPanel = default!;
    [SerializeField] private TextMeshProUGUI stageIdText = default!;
    [SerializeField] private TextMeshProUGUI stageNameText = default!;
    [SerializeField] private TextMeshProUGUI stageDetailText = default!;
    [SerializeField] private Button confirmButton = default!;
    [SerializeField] private GameObject itemElementPrefab = default!;
    [SerializeField] private Transform itemContainerTransform = default!;

    public StageButton? CurrentStageButton { get; private set; }
    private StageData? selectedStage => MainMenuManager.Instance!.SelectedStage;

    private PlayerDataManager playerDataManager = default!;

    private void Start()
    {
        playerDataManager = GameManagement.Instance!.PlayerDataManager;

        InitializeStageButtons();
        InitializeDetailPanel();
    }

    private void InitializeStageButtons()
    {
        
        foreach (StageUIData stageUIData in stageUIDataList)
        {
            // ��ư�� ������ �߰�
            StageButton stageButton = stageUIData.stageButton;
            stageButton.onClick.AddListener(OnStageButtonClicked);

            bool isUnlocked = playerDataManager.IsStageUnlocked(stageUIData.stageId);
            stageUIData.stageButton.gameObject.SetActive(isUnlocked);

            // ���� �̹����� ���� ���������� �ִ� ������Ʈ�鿡�� �Ҵ�
            if (stageUIData.lineImage != null)
            {
                stageUIData.lineImage.gameObject.SetActive(isUnlocked);
            }

            // Ŭ����� ��������
            if (playerDataManager.IsStageCleared(stageUIData.stageId))
            {
                var stageResultInfo = playerDataManager.GetStageResultInfo(stageUIData.stageId);
                stageUIData.stageButton.SetUpStar(stageResultInfo.stars);
            }
        }
    }

    private void InitializeDetailPanel()
    {
        if (selectedStage != null)
        {
            stageDetailPanel.SetActive(true);
            SetStageButtonById(selectedStage.stageId);
        }
        else
        {
            stageDetailPanel.SetActive(false);
        }

        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        cancelArea.onClick.AddListener(OnCancelAreaClicked);

        InitializeDetailPanelButtons();
        cancelArea.interactable = false;
    }

    private void InitializeDetailPanelButtons()
    {
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        if (selectedStage != null)
        {
            confirmButton.interactable = true;
        }
        else
        {
            confirmButton.interactable = false;
        }
    }


    // stageId�� �ش��ϴ� ��ư�� Ŭ�� ���·� �ٲ�
    public void SetStageButtonById(string stageId)
    {
        StageButton targetButton = stageUIDataList.FirstOrDefault(stageUIData => stageUIData.stageId == stageId).stageButton;

        if (targetButton != null)
        {
            OnStageButtonClicked(targetButton);
        }
    }

    public StageData GetStageDataFromStageButton(StageButton stageButton)
    {
        return stageButton.StageData;
    }


    // ��ư Ŭ������ ������ ���� ������Ʈ Panel���� ������
    private void OnStageButtonClicked(StageButton clickedButton)
    {
        if (CurrentStageButton != null)
        {
            CurrentStageButton.SetSelected(false);
        }

        // ���ο� ���� ó��
        CurrentStageButton = clickedButton;
        CurrentStageButton.SetSelected(true);

        // DetailPanel ������Ʈ
        ShowDetailPanel(CurrentStageButton.StageData);
    }

    private void OnCancelAreaClicked()
    {
        if (CurrentStageButton != null)
        {
            CurrentStageButton.SetSelected(false);
            CurrentStageButton = null;
        }
        stageDetailPanel.SetActive(false);
        cancelArea.interactable = false;
    }

    private void ShowDetailPanel(StageData stageData)
    {
        if (stageDetailPanel != null)
        {
            stageDetailPanel.SetActive(true);
            cancelArea.interactable = true;
            stageIdText.text = stageData.stageId;
            stageNameText.text = stageData.stageName;
            stageDetailText.text = stageData.stageDetail;
            confirmButton.interactable = true;
            ShowRewardItems(stageData);
        }
    }

    private void ShowRewardItems(StageData stageData)
    {
        // ���� ������ UI ����
        foreach (Transform child in itemContainerTransform)
        {
            Destroy(child.gameObject);
        }

        // ���õ� ���������� ������ ����
        if (stageData == null || CurrentStageButton == null)
            return;

        var stageResultInfo = GameManagement.Instance!.PlayerDataManager.GetStageResultInfo(stageData.stageId);
        int prevStars = stageResultInfo != null ? stageResultInfo.stars : 0;

        Debug.Log($"stageResultInfo : {stageResultInfo}");

        // ù Ŭ���� ���� - 3�� Ŭ���� ����, ���� ������� �����ش�.
        if (stageResultInfo == null || stageResultInfo.stars != 3)
        {
            // ���� ����
            float firstItemRewardMultiplier = GameManagement.Instance!.RewardManager.GetResultFirstClearItemRate(prevStars);

            foreach (var reward in stageData.FirstClearRewardItems)
            {
                int fullCount = reward.count;

                // ������ ����ġ �����ۿ��� ����, ����ȭ �������� 1
                int remainingCount = reward.itemData.type == ItemData.ItemType.EliteItem ? 1 
                    : Mathf.FloorToInt(fullCount * firstItemRewardMultiplier);


                if (remainingCount > 0)
                {
                    GameObject rewardUI = Instantiate(itemElementPrefab, itemContainerTransform);
                    ItemUIElement uiElement = rewardUI.GetComponent<ItemUIElement>();
                    if (uiElement != null)
                    {
                        uiElement.Initialize(reward.itemData, remainingCount, true, false);
                    }
                }
            }
        }
        

        // �⺻ ����(�ݺ� ����): �׻� ��ü ����(3�� Ŭ���� ����)���� ������
        foreach (var reward in stageData.BasicClearRewardItems)
        {
            int fullCount = reward.count;
            GameObject rewardUI = Instantiate(itemElementPrefab, itemContainerTransform);
            ItemUIElement uiElement = rewardUI.GetComponent<ItemUIElement>();
            if (uiElement != null)
            {
                uiElement.Initialize(reward.itemData, fullCount);
            }
        }
    }



    private void OnConfirmButtonClicked()
    {
        if (CurrentStageButton != null)
        {
            MainMenuManager.Instance!.SetSelectedStage(CurrentStageButton.StageData);

            GameObject squadEditPanel = MainMenuManager.Instance!.PanelMap[MainMenuManager.MenuPanel.SquadEdit];
            GameObject stageSelectPanel = MainMenuManager.Instance!.PanelMap[MainMenuManager.MenuPanel.StageSelect];

            // squadEditPanel�� �����ְ� stageSelectPanel�� ����
            MainMenuManager.Instance!.FadeInAndHide(squadEditPanel, stageSelectPanel);
        }
    }


    // �г� ��Ȱ��ȭ �� ���� �ʱ�ȭ
    private void OnDisable()
    {
        if (CurrentStageButton != null)
        {
            CurrentStageButton.SetSelected(false);
            CurrentStageButton = null;
        }

        stageDetailPanel.SetActive(false);
        cancelArea.interactable = false;
        confirmButton.interactable = false;
    }
}
