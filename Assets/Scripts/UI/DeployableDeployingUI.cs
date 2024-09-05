using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeployableDeployingUI : MonoBehaviour
{
    [SerializeField] private MaskedDiamondOverlay maskedOverlay;
    [SerializeField] private Button cancelButton;

    private float darkPanelAlpha = 0.3f;

    private RectTransform diamondRect;
    private RectTransform cancelButtonRect;

    private Camera mainCamera;

    public void Initialize(IDeployable deployable)
    {
        mainCamera = Camera.main;
        cancelButtonRect = cancelButton.GetComponent<RectTransform>();

        SetupDiamondImage();
        SetupCancelButton();

        maskedOverlay.Initialize(darkPanelAlpha); // 알파 조정

        if (mainCamera != null)
        {
            //transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
            transform.rotation = Quaternion.Euler(90, 0, 0);
        }
    }

    private void SetupDiamondImage()
    {
        //diamondImage.LineWidth = lineWidth;
    }

    public void Show(Vector3 position)
    {
        transform.position = position;
        maskedOverlay.Show();
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        maskedOverlay.Hide();
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
        DeployableManager.Instance.CancelDeployableSelection();
    }


}