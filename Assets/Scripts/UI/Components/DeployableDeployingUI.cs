using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class DeployableDeployingUI : MonoBehaviour
{
    [SerializeField] private MaskedDiamondOverlay maskedOverlay = default!;
    [SerializeField] private Button cancelButton = default!;

    private float darkPanelAlpha = 0.3f;
    private Camera mainCamera = default!;
    private RectTransform cancelButtonRect = default!;

    private DeployableUnitEntity deployable;
    public DeployableUnitEntity Deployable => deployable;


    public void Initialize(DeployableUnitEntity deployable)
    {
        this.deployable = deployable;

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
        DeployableManager.Instance!.CancelDeployableSelection();
    }


}