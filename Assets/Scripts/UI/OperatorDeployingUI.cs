using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OperatorDeployingUI : MonoBehaviour
{
    [SerializeField] private DiamondImage diamondImage;
    [SerializeField] private Button cancelButton;
    [SerializeField] private float lineWidth = 0.1f;

    private RectTransform diamondRect;
    private RectTransform cancelButtonRect;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;

        diamondRect = diamondImage.GetComponent<RectTransform>();
        cancelButtonRect = cancelButton.GetComponent<RectTransform>();

    }

    public void Initialize(OperatorData operatorData)
    {
        SetupDiamondIndicator();
        SetupCancelButton();

        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }
    }

    private void SetupDiamondIndicator()
    {
        //diamondRect.sizeDelta = new Vector2(diamondSize, diamondSize);
        diamondImage.LineWidth = lineWidth;
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
            // 마름모의 좌상단에 버튼 위치시키기
            //float buttonOffset = diamondSize * buttonOffsetRate;
            //cancelButtonRect.anchorMin = new Vector2(0, 1);
            //cancelButtonRect.anchorMax = new Vector2(0, 1);
            //cancelButtonRect.anchoredPosition = new Vector2(buttonOffset, -buttonOffset);

            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }
    }

    private void OnCancelButtonClicked()
    {
        OperatorManager.Instance.CancelOperatorSelection();
    }


}
