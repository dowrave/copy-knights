using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    // 여러 패널들을 열거형으로 관리
    public enum MenuPanel
    {
        None, 
        StageSelect,
        SquadEdit,
        OperatorSelect
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

    private Dictionary<MenuPanel, GameObject> panelMap = new Dictionary<MenuPanel, GameObject>();
    private MenuPanel currentPanel; // enum 타입이라 따로 초기화 안하면 0번인 StageSelect로 들어감

    private StageData selectedStageData;
    private OperatorSlotButton currentEditingSlot; // 현재 할당할 오퍼레이터 슬롯
    public OperatorSlotButton CurrentEditingSlot 
    { 
        get => currentEditingSlot;
        private set
        {
            currentEditingSlot = value;
        }
    }


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
        foreach (var panelInfo in panels)
        {
            panelMap[panelInfo.type] = panelInfo.panel;
        }
    }

    private void Start()
    {
        foreach (PanelInfo panel in panels)
        {
            if (panel.type == MenuPanel.StageSelect)
            {
                Debug.Log($"현재 패널 {panel.type} / 탄 조건문 : ShowPanel");
                ShowPanel(panel.type, false);
            } 
            else
            { 
                Debug.Log($"현재 패널 {panel.type} / 탄 조건문 : HidePanel");
                HidePanel(panel.panel, false);
            }
        }
    }

    public void ShowPanel(MenuPanel newPanel, bool animate = true)
    {
        Debug.Log($"현재 패널 {currentPanel} / 넣으려는 패널 : {newPanel}");

        if (currentPanel == newPanel) return;

        // 현재 패널 숨기기
        if (panelMap.TryGetValue(currentPanel, out GameObject currentPanelObj))
        {
            HidePanel(currentPanelObj, animate);
        }

        // 새 패널 표시
        if (panelMap.TryGetValue(newPanel, out GameObject newPanelObj))
        {
            ShowPanelObject(newPanelObj, animate);
            currentPanel = newPanel; 
        }
    }

    private void ShowPanelObject(GameObject panel, bool animate)
    {
        if (animate)
        {
            // 애니메이션 처리
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                panel.SetActive(true);
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.3f);
            }
            else
            {
                panel.SetActive(true);
            }
        }
        else
        {
            panel.SetActive(true);
        }
    }

    private void HidePanel(GameObject panel, bool animate)
    {
        if (animate)
        {
            CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, 0.3f).OnComplete(() => panel.SetActive(false));
            }
            else
            {
                panel.SetActive(false);
            }
        }
    }

    // 스테이지 시작
    public void StartStage(string stageName)
    {
        SceneManager.LoadScene(stageName);
    }

    public void OnStageSelected(StageData stageData)
    {
        selectedStageData = stageData;
        ShowPanel(MenuPanel.SquadEdit);
    }

    /// <summary>
    /// 현재 슬롯을 매니저에 할당하고, 오퍼레이터 선택 패널을 띄웁니다.
    /// </summary>
    public void StartOperatorSelection(OperatorSlotButton clickedSlot)
    {
        CurrentEditingSlot = clickedSlot;
        ShowPanel(MenuPanel.OperatorSelect);
    }

}
 