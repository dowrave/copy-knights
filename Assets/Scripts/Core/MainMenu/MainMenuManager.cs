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

    [SerializeField] private IconData classIconData;

    private Dictionary<MenuPanel, GameObject> panelMap = new Dictionary<MenuPanel, GameObject>();
    private MenuPanel currentPanel; // 디폴트는 0번에 있는 값. null이 아님.

    private StageData selectedStageData;


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
                ShowPanel(panel.type, false);
            } 
            else
            {
                HidePanel(panel.panel, false);
            }
        }

        UserSquadManager.Instance.OnSquadUpdated += OnSquadUpdated; 
    }

    public void ShowPanel(MenuPanel newPanel, bool animate = true)
    {
        if (currentPanel == newPanel) return;

        // 현재 패널 숨기기
        if (panelMap.TryGetValue(currentPanel, out GameObject currentPanelObj))
        {
            HidePanel(currentPanelObj, animate);
        }

        Debug.Log($"{newPanel}을 보여주려고 한다");
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
                Debug.Log($"{panel} 활성화 완료");
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, 0.3f);
                return;
            }
        }

        panel.SetActive(true);
        Debug.Log($"{panel} 활성화 완료");
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
        else
        {
            panel.SetActive(false);
        }
    }

    // 스테이지 시작
    public void StartStage(string stageName)
    {
        List<OperatorData> currentSquad = UserSquadManager.Instance.GetCurrentSquad();
        if (currentSquad.Count > 0)
        {
            SceneManager.LoadScene(stageName);
        }
        else
        {
            Debug.LogWarning("빈 스쿼드로는 스테이지를 시작할 수 없음");
        }
    }

    public void OnStageSelected(StageData stageData)
    {
        selectedStageData = stageData;
        ShowPanel(MenuPanel.SquadEdit);
    }

    private void OnSquadUpdated()
    {
        // UI 업데이트
        UpdateSquadUI();
    }

    private void UpdateSquadUI()
    {
        List<OperatorData> currentSquad = UserSquadManager.Instance.GetCurrentSquad();

        // UI 업데이트
    }

    // SquadEditPanel의 슬롯에 OperatorListPanel에서 오퍼레이터를 결정하고 할당할 때 사용
    public void OnOperatorSelected(int slotIndex, OperatorData newOperatorData)
    {
        UserSquadManager.Instance.TryReplaceOperator(slotIndex, newOperatorData);
    }

    private void OnDestroy()
    {
        UserSquadManager.Instance.OnSquadUpdated -= OnSquadUpdated;
    }
}
 