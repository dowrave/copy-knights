
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
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
    [SerializeField] private Button cancelArea = default!; // buttonContainer가 이 역할을 함
    [SerializeField] private GameObject stageDetailPanel = default!;
    [SerializeField] private TextMeshProUGUI stageIdText = default!;
    [SerializeField] private TextMeshProUGUI stageNameText = default!;
    [SerializeField] private TextMeshProUGUI stageDetailText = default!;
    [SerializeField] private Button confirmButton = default!;
    [SerializeField] private GameObject itemElementPrefab = default!;
    [SerializeField] private Transform itemContainerTransform = default!;
    [SerializeField] private float animationDuration = .5f;

    public StageButton? CurrentStageButton { get; private set; }
    private StageData? selectedStage => MainMenuManager.Instance!.SelectedStage;

    private PlayerDataManager playerDataManager = default!;

    // 사이드 패널 위치 관련
    RectTransform detailPanelRect;
    private bool isPanelOpen; 
    private float openPosX; // 사이드 패널 활성화됐을 때의 위치
    private float closePosX; // 사이드 패널 비활성화됐을 때의 위치

    private void Awake()
    {
        InitializeDetailPanelPosition();
    }

    private void Start()
    {
        playerDataManager = GameManagement.Instance!.PlayerDataManager;

        InitializeStageButtons();
        InitializeDetailPanel();
    }

    // 애니메이션 구현을 위한 디테일 패널 위치 초기화
    private void InitializeDetailPanelPosition()
    {
        // 디테일 패널 초기 위치 설정
        detailPanelRect = stageDetailPanel.GetComponent<RectTransform>();

        // 에디터에서 보이는 위치
        openPosX = detailPanelRect.anchoredPosition.x;

        // 최초에 가려진 위치
        closePosX = openPosX + detailPanelRect.rect.width;

        // 화면 밖으로 패널을 이동시킴
        detailPanelRect.DOAnchorPosX(closePosX, 0);
    }

    private void OnEnable()
    {
        detailPanelRect.DOAnchorPosX(closePosX, 0);
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        cancelArea.onClick.AddListener(OnCancelAreaClicked);
    }

    private void InitializeStageButtons()
    {
        
        foreach (StageUIData stageUIData in stageUIDataList)
        {
            // 버튼에 리스너 추가
            StageButton stageButton = stageUIData.stageButton;
            stageButton.onClick.AddListener(OnStageButtonClicked);

            bool isUnlocked = playerDataManager.IsStageUnlocked(stageUIData.stageId);
            stageUIData.stageButton.gameObject.SetActive(isUnlocked);

            // 라인 이미지는 이전 스테이지가 있는 오브젝트들에만 할당
            if (stageUIData.lineImage != null)
            {
                stageUIData.lineImage.gameObject.SetActive(isUnlocked);
            }

            // 클리어된 스테이지
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
            SetStageButtonById(selectedStage.stageId);
            OpenDetailPanel();
        }
        else
        {
            CloseDetailPanel();
        }

        // InitializeDetailPanelButtons();
        // cancelArea.interactable = false;
    }

    // private void InitializeDetailPanelButtons()
    // {
        

    //     // if (selectedStage != null)
    //     // {
    //     //     confirmButton.interactable = true;
    //     // }
    //     // else
    //     // {
    //     //     confirmButton.interactable = false;
    //     // }

        
    // }


    // stageId에 해당하는 버튼을 클릭 상태로 바꿈
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


    // 버튼 클릭시의 동작은 상위 오브젝트 Panel에서 관리함
    private void OnStageButtonClicked(StageButton clickedButton)
    {
        if (CurrentStageButton != null)
        {
            CurrentStageButton.SetSelected(false);
        }

        // 새로운 선택 처리
        CurrentStageButton = clickedButton;
        CurrentStageButton.SetSelected(true);

        // DetailPanel 업데이트
        ShowDetailPanel(CurrentStageButton.StageData);
    }

    private void OpenDetailPanel()
    {
        // stageDetailPanel.SetActive(true);
        detailPanelRect.DOAnchorPosX(openPosX, animationDuration).SetEase(Ease.OutCubic);
    }
    
    private void CloseDetailPanel()
    {
        detailPanelRect.DOAnchorPosX(closePosX, animationDuration).SetEase(Ease.InCubic);
        // stageDetailPanel.SetActive(false);
    }


    private void OnCancelAreaClicked()
    {
        CloseDetailPanel();

        if (CurrentStageButton != null)
        {
            CurrentStageButton.SetSelected(false);
            CurrentStageButton = null;
        }
        // stageDetailPanel.SetActive(false);
        // cancelArea.interactable = false;
    }

    private void ShowDetailPanel(StageData stageData)
    {
        if (stageDetailPanel != null)
        {
            // cancelArea.interactable = true;
            stageIdText.text = stageData.stageId;
            stageNameText.text = stageData.stageName;
            stageDetailText.text = stageData.stageDetail;
            // confirmButton.interactable = true;
            ShowRewardItems(stageData);

            OpenDetailPanel();
        }
    }

    private void ShowRewardItems(StageData stageData)
    {
        // 기존 아이템 UI 제거
        foreach (Transform child in itemContainerTransform)
        {
            Destroy(child.gameObject);
        }

        // 선택된 스테이지가 없으면 종료
        if (stageData == null || CurrentStageButton == null)
            return;

        var stageResultInfo = GameManagement.Instance!.PlayerDataManager.GetStageResultInfo(stageData.stageId);
        int prevStars = stageResultInfo != null ? stageResultInfo.stars : 0;

        // 첫 클리어 보상 - 3성 클리어 기준, 남은 보상들을 보여준다.
        if (stageResultInfo == null || stageResultInfo.stars != 3)
        {
            // 배율 설정
            float firstItemRewardMultiplier = GameManagement.Instance!.RewardManager.GetResultFirstClearItemRate(prevStars);

            foreach (var reward in stageData.FirstClearRewardItems)
            {
                int fullCount = reward.count;

                // 배율은 경험치 아이템에만 적용, 정예화 아이템은 1
                int remainingCount = reward.itemData.type == ItemData.ItemType.EliteItem ?
                    reward.count : // 정예화 아이템은 갯수 그대로
                    Mathf.FloorToInt(fullCount * firstItemRewardMultiplier); // 경험치 아이템은 배율 적용

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
        

        // 기본 보상(반복 보상): 항상 전체 수량(3성 클리어 기준)으로 보여줌
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

            // squadEditPanel을 보여주고 stageSelectPanel을 숨김
            MainMenuManager.Instance!.ChangePanel(squadEditPanel, stageSelectPanel);
            // confirmButton.interactable = false; // 중복 클릭 방지
            //Logger.Log("StageSelectPanel : ConfirmButton 클릭됨");
        }
    }


    // 패널 비활성화 시 상태 초기화
    private void OnDisable()
    {
        if (CurrentStageButton != null)
        {
            CurrentStageButton.SetSelected(false);
            CurrentStageButton = null;
        }

        // stageDetailPanel.SetActive(false);
        // cancelArea.interactable = false;
        // confirmButton.interactable = false;

        confirmButton.onClick.RemoveAllListeners();
        cancelArea.onClick.RemoveAllListeners();
    }
}
