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
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }
    }

    private void OnCancelButtonClicked()
    {
        DeployableManager.Instance.CancelDeployableSelection();
    }


}