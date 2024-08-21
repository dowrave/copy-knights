using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OperatorDeployingUI : MonoBehaviour
{
    // 자동으로 할당하지만 명시적으로 보여주는 게 좋다고 한다
    [SerializeField] private GameObject dragIndicator;
    [SerializeField]  private GameObject cancelButton;

    private const float INDICATOR_SIZE = 2.5f; // ?

    private void Awake()
    {
        // 시작 시 자식 오브젝트 참조 설정
        if (dragIndicator == null)
            dragIndicator = transform.Find("Drag Indicator").gameObject;
        if (cancelButton == null)
            cancelButton = transform.Find("Canvas/CancelButton").gameObject;

        SetupCancelButton();
    }

    public void Show(Vector3 position)
    {
        transform.position = position;

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void SetupCancelButton()
    {
        if (cancelButton != null)
        {
            Button button = cancelButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnCancelButtonClicked);
            }
        }
    }

    private void OnCancelButtonClicked()
    {
        OperatorManager.Instance.CancelOperatorSelection();
    }


}
