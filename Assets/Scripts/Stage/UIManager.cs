using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private GameObject gameOverPanel;
    private GameObject gameWinPanel; // ����� �ִϸ��̼��� ���ϱ� �ϴ� ���α⸸ �մϴ�
    private GameObject DeploymentCostPanel;
    private GameObject TopCenterPanel; // ���� �� ��, ������ ��
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

        // �г� ��Ȱ��ȭ
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
