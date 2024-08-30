using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameWinPanel; // 여기는 애니메이션이 들어가니까 일단 냅두기만 합니다
    [SerializeField] private GameObject deploymentCostPanel;
    [SerializeField] private GameObject topCenterPanel; // 남은 적 수, 라이프 수
    [SerializeField] private GameObject bottomPanel;
    [SerializeField] private GameObject operatorInfoPanel;
    //[SerializeField] private GameObject overlayPanel;

    private OperatorInfoPanel operatorInfoPanelScript;
    //private OverlayPanel overlayPanelScript;


    // Awake는 모든 오브젝트의 초기화 전에 실행되어서 다른 스크립트가 참조할 수 있도록 한다. 특히 UI는 Awake를 쓸 것.
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } 
        else
        {
            Destroy(gameObject);
        }


        // 인스펙터에서 할당하겠지만, 안전성을 위해
        if (gameOverPanel == null)
        {
            gameOverPanel = transform.Find("GameOverPanel").gameObject;
        }
        if (gameWinPanel == null)
        {
            gameWinPanel = transform.Find("GameWinPanel").gameObject;
        }
        if (operatorInfoPanel == null)
        {
            operatorInfoPanel = transform.Find("OperatorInfoPanel").gameObject;
        }

        // 비활성화 전에 참조를 넣어두는 게 좋다
        operatorInfoPanelScript = operatorInfoPanel.GetComponent<OperatorInfoPanel>();

        // 패널 비활성화
        gameOverPanel.SetActive(false);
        gameWinPanel.SetActive(false);
        operatorInfoPanel.SetActive(false);

    }

    public void ShowGameOverUI()
    {
        gameOverPanel.SetActive(true);
    }
    public void ShowGameWinUI()
    {
        gameWinPanel.SetActive(true);
    }

    public void ShowOperatorInfo(OperatorData operatorData, Operator op = null)
    {
        if (operatorInfoPanelScript != null)
        {
            operatorInfoPanel.SetActive(true);

            operatorInfoPanelScript.UpdateInfo(operatorData, op);
            CameraManager.Instance.AdjustForOperatorInfo(true, op);
        }
    }

    public void HideOperatorInfo()
    {
        if (operatorInfoPanel != null)
        {
            operatorInfoPanel.SetActive(false);
            CameraManager.Instance.AdjustForOperatorInfo(false);
        }
    }

}
