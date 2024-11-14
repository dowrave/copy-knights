using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 씬 전환 시에도 유지되어야 하는 전역 매니저를 관리하는 클래스
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    // 여러 패널들을 열거형으로 관리
    public enum MenuPanel
    {
        None, 
        StageSelect,
        SquadEdit,
        OperatorList
        // 새로운 패널은 열거형으로 계속 추가하면 됨
    }

    [System.Serializable]
    private class PanelInfo
    {
        public MenuPanel type;
        public GameObject panel;
    }


    [Header("Panel References")]
    [SerializeField] private List<PanelInfo> panels;

    [Header("Class Icon Data")]
    [SerializeField] private IconData classIconData;

    [SerializeField] private float panelTransitionSpeed;

    private Dictionary<MenuPanel, GameObject> panelMap = new Dictionary<MenuPanel, GameObject>();
    public Dictionary<MenuPanel, GameObject> PanelMap => panelMap;

    private StageData selectedStage;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Dict 초기화
        foreach (PanelInfo panelInfo in panels)
        {
            panelMap[panelInfo.type] = panelInfo.panel;
        }

        // OperatorClass 아이콘을 담당하는 IconHelper 초기화
        IconHelper.Initialize(classIconData);
    }

    private void Start()
    {
        // StageSelectPanel만 활성화
        foreach (PanelInfo panel in panels)
        {
            if (panel.type == MenuPanel.StageSelect)
            {
                panel.panel.SetActive(true);
            }
            else
            {
                panel.panel.SetActive(false);
            }
        }

        // 마지막 플레이 정보를 확인, 해당 스테이지 선택
        string lastPlayedStage = PlayerPrefs.GetString("LastPlayedStage", null);
        if (!string.IsNullOrEmpty(lastPlayedStage))
        {
            if (panelMap.TryGetValue(MenuPanel.StageSelect, out GameObject stageSelectObj))
            {
                var stageSelectPanel = stageSelectObj.GetComponent<StageSelectPanel>();
                if (stageSelectPanel != null)
                {
                    StageData targetStageData = stageSelectPanel.GetStageDataById(lastPlayedStage);
                    if (targetStageData != null)
                    {
                        SetSelectedStage(targetStageData);
                    }
                }
            }

            // 사용 정보는 삭제
            PlayerPrefs.DeleteKey("LastPlayedStage");
            PlayerPrefs.Save();
        }

        GameManagement.Instance.UserSquadManager.OnSquadUpdated += OnSquadUpdated;
    }

    // 새 패널 페이드 인 후 이전 패널 비활성화
    public void FadeInAndHide(GameObject panelToShow, GameObject panelToHide)
    {
        CanvasGroup showGroup = panelToShow.GetComponent<CanvasGroup>();
        panelToShow.SetActive(true);
        showGroup.alpha = 0f;
        showGroup.DOFade(1f, panelTransitionSpeed)
            .OnComplete(() => panelToHide.SetActive(false));
    }

    // 새 패널 활성화 후 이전 패널 페이드 아웃
    public void ActivateAndFadeOut(GameObject panelToShow, GameObject panelToHide)
    {
        // 이전 패널 즉시 활성화 -> 현재 패널 페이드 아웃
        panelToShow.SetActive(true);
        CanvasGroup hideGroup = panelToHide.GetComponent<CanvasGroup>();
        hideGroup.DOFade(0f, panelTransitionSpeed)
            .OnComplete(() => panelToHide.SetActive(false));
    }

    // 스테이지 시작
    public void StartStage()
    {
        List<OperatorData> currentSquad = GameManagement.Instance.UserSquadManager.GetActiveOperators();

        if (currentSquad.Count > 0) // null을 값으로 갖는다면 포함해서 세므로 GetActiveOperators()을 써야 한다.
        {
            GameManagement.Instance.StageLoader.LoadStage(selectedStage);
        }
        else
        {
            Debug.LogWarning("빈 스쿼드로는 스테이지를 시작할 수 없음");
        }
    }

    /// <summary>
    /// StageSelectPanel에서 이용 : 현재 스테이지 정보를 저장하고 SquadEditPanel로 넘어감
    /// </summary>
    public void SetSelectedStage(StageData stageData)
    {
        selectedStage = stageData;
    }

    private void OnSquadUpdated()
    {
        // UI 업데이트
        UpdateSquadUI();
    }

    private void UpdateSquadUI()
    {
        List<OperatorData> currentSquad = GameManagement.Instance.UserSquadManager.GetCurrentSquad();

        // UI 업데이트
    }

    // SquadEditPanel의 슬롯에 OperatorListPanel에서 오퍼레이터를 결정하고 할당할 때 사용
    public void OnOperatorSelected(int slotIndex, OperatorData newOperatorData)
    {
        GameManagement.Instance.UserSquadManager.TryReplaceOperator(slotIndex, newOperatorData);
    }

    private void OnDestroy()
    {
        GameManagement.Instance.UserSquadManager.OnSquadUpdated -= OnSquadUpdated;
    }
}
 