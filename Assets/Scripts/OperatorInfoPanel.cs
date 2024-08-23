
using UnityEngine;
using UnityEngine.UI;


public class OperatorInfoPanel : MonoBehaviour
{
    public static OperatorInfoPanel Instance { get; private set; }

    //[SerializeField] private RectTransform infoPanel;

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

    public void ShowOperatorInfo(OperatorData operatorData)
    {
        // 패널 활성화
        gameObject.SetActive(true);

        // 카메라 이동 및 크기 조절
        CameraManager.Instance.AdjustForOperatorInfo(true);

        // 오퍼레이터 정보 표시 -> 구현 필요
    }

    public void HideOperatorInfo()
    {
        // 패널 비활성화
        gameObject.SetActive(false);

        //infoPanel.gameObject.SetActive(false);

        // 카메라 원위치
        CameraManager.Instance.AdjustForOperatorInfo(false);

    }

}
