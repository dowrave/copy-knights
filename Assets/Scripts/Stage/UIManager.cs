using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private GameObject gameOverPanel;
    private GameObject gameWinPanel; // 여기는 애니메이션이 들어가니까 일단 냅두기만 합니다
    private GameObject DeploymentCostPanel;
    private GameObject TopCenterPanel; // 남은 적 수, 라이프 수
    private GameObject BottomPanel;

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
    }

    // Start is called before the first frame update
    void Start()
    {
        gameOverPanel = transform.Find("GameOverPanel").gameObject;
        gameWinPanel = transform.Find("GameWinPanel").gameObject;

        // 패널 비활성화
        gameOverPanel.SetActive(false);
        gameWinPanel.SetActive(false);
    }

    public void ShowGameOverUI()
    {
        gameOverPanel.SetActive(true);
    }
    public void ShowGameWinUI()
    {
        gameWinPanel.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
