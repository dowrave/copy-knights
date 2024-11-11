using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 씬에서 스테이지가 종료된 후에 나타날 패널
/// </summary>
public class StageResultPanel : MonoBehaviour
{
    [Header("Star Rating")]
    // 뭔 느낌인지 모르겠음
    [SerializeField] private Image[] starImages;
    [SerializeField] private Sprite filledStarSprite;
    [SerializeField] private Sprite emptyStarSprite;

    [Header("Result Text")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI stageIdText;

    [Header("Statistics")]
    [SerializeField] private GameObject statsContainer;
    [SerializeField] private StatisticItem statisticItemPrefab;
    [SerializeField] private Transform statisticItemContainer;
    [SerializeField] private Button toggleStatsButton;

    // 별도의 내비게이션은 사용하지 않음 : 즉 결과 패널이 끝나면 바로 로비로 감

    private StageResultData resultData;
    private bool showingStats = false;
    private List<StatisticItem> statItems = new List<StatisticItem>();

    private void Awake()
    {
        // 패널 영역 클릭 이벤트 설정
        var panelClickHandler = gameObject.GetComponent<Button>();
        panelClickHandler.transition = Selectable.Transition.None; // 시각적인 클릭 효과 제거
        panelClickHandler.onClick.AddListener(OnPanelClicked);

        // 토글 버튼 클릭이 패널 클릭을 트리거하지 않도록 설정
        toggleStatsButton.onClick.AddListener(() =>
        {
            ToggleStats();

            // 이벤트 전파 중지
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        });
    }

    private void OnPanelClicked()
    {
        // 토글 버튼(=스탯 전환 버튼) 영역을 클릭했을 때는 무시
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() && 
            UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == toggleStatsButton.gameObject)
        {
            return;
        }

        // 3성 클리어가 아니라면 현재 스테이지를 선택한 상태로 돌아감
        if (!resultData.isCleared || resultData.StarCount < 3)
        {
            GameManagement.Instance.StageLoader.ReturnToMainMenuWithStageSelected();
        }
        else
        {
            GameManagement.Instance.StageLoader.ReturnToMainMenu();
        }
    }

    public void Initialize(StageResultData data)
    {
        resultData = data;

        UpdateStarRating();
        UpdateTitleAndStageInfo();
        CreateStatItems();
        HideStats();
    }

    private void UpdateStarRating()
    {
        for (int i =0; i < starImages.Length; i ++)
        {
            starImages[i].sprite = i < resultData.StarCount ? filledStarSprite : emptyStarSprite;
        }
    }

    private void UpdateTitleAndStageInfo()
    {
        titleText.text = resultData.isCleared ? "Stage Clear!" : "Stage Failed";
        stageIdText.text = $"Stage {resultData.stageId}";
    }

    private void ToggleStats()
    {
        showingStats = !showingStats;
        statsContainer.SetActive(showingStats);
    }

    private void CreateStatItems()
    {
        foreach (var item in statItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        
        // 새로운 아이템 생성
        foreach (var stat in resultData.operatorStats)
        {
            StatisticItem item = Instantiate(statisticItemPrefab, statisticItemContainer);
            item.Initialize(stat.op, StatisticsManager.StatType.DamageDealt, false);
            statItems.Add(item);
        }
    }

    private void HideStats()
    {
        showingStats = false;
        statsContainer.SetActive(false);
    }

    private void OnDestroy()
    {
        toggleStatsButton.onClick.RemoveAllListeners(); 
    }
}