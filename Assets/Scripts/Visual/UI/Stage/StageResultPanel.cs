using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// �������� ������ ���������� ����� �Ŀ� ��Ÿ�� �г�
/// </summary>
public class StageResultPanel : MonoBehaviour
{
    [Header("Star Rating")]
    // �� �������� �𸣰���
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

    // ������ ������̼��� ������� ���� : �� ��� �г��� ������ �ٷ� �κ�� ��

    private StageResultData resultData;
    private bool showingStats = false;
    private List<StatisticItem> statItems = new List<StatisticItem>();

    private void Awake()
    {
        // �г� ���� Ŭ�� �̺�Ʈ ����
        var panelClickHandler = gameObject.GetComponent<Button>();
        panelClickHandler.transition = Selectable.Transition.None; // �ð����� Ŭ�� ȿ�� ����
        panelClickHandler.onClick.AddListener(OnPanelClicked);

        // ��� ��ư Ŭ���� �г� Ŭ���� Ʈ�������� �ʵ��� ����
        toggleStatsButton.onClick.AddListener(() =>
        {
            ToggleStats();

            // �̺�Ʈ ���� ����
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        });
    }

    private void OnPanelClicked()
    {
        // ��� ��ư(=���� ��ȯ ��ư) ������ Ŭ������ ���� ����
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() && 
            UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == toggleStatsButton.gameObject)
        {
            return;
        }

        // 3�� Ŭ��� �ƴ϶�� ���� ���������� ������ ���·� ���ư�
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
        
        // ���ο� ������ ����
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