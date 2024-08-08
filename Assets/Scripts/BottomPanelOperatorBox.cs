using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BottomPanelOperatorBox : MonoBehaviour, IPointerClickHandler
{
    private Transform operatorIcon; // ������Ʈ
    private Image operatorIconImage; // Image ������Ʈ
    private Color originalIconColor;
    private TextMeshProUGUI costText;
    private OperatorData operatorData;

    public void Initialize(OperatorData data)
    {
        operatorData = data;
        operatorIcon = transform.Find("OperatorIcon");


        if (operatorIcon != null)
        {
            operatorIconImage = operatorIcon.GetComponentInChildren<Image>(); // OperatorIcon�� Image ������Ʈ�� ����
            costText = GetComponentInChildren<TextMeshProUGUI>(); // CostText�� TextMeshPro�� ����
        }

        UpdateVisuals();
        // �ڽ�Ʈ ���� ���� ��������Ʈ�� �̺�Ʈ �߰�
        StageManager.Instance.OnDeploymentCostChanged += UpdateAvailability;
    }

    private void OnDestroy()
    {
        StageManager.Instance.OnDeploymentCostChanged -= UpdateAvailability;
    }

    private void UpdateVisuals()
    {
        // operatorData�� �ش��ϴ� �������� �ִٸ� �װ� �ְ� 
        if (operatorData.icon != null)
        {
            operatorIconImage.sprite = operatorData.icon;
            operatorIconImage.color = new Color(1f, 1f, 1f, 1f); // �Ϸ���Ʈ�� ��ȭ�� ���� �ʰ� �״�� ����
        }

        // ���ٸ� operatorData�� �ش��ϴ� ���� �����ͼ� �Ҵ��Ѵ�.
        else if (operatorData.prefab != null)
        {
            Renderer modelRenderer = operatorData.prefab.GetComponentInChildren<Renderer>();
            if (modelRenderer != null && modelRenderer.sharedMaterial != null)
            {   
                originalIconColor = modelRenderer.sharedMaterial.color;
                operatorIconImage.color = originalIconColor;
            }
        }

        costText.text = operatorData.deploymentCost.ToString();
        //UpdateAvailability();
    }

    // eventData : ����Ƽ�� ���� �ڵ� ����, Ŭ�� �̺�Ʈ ���� ����. (�׷��� operatorData���� ������ ��ǲ�� ������ �ۼ��ؾ� ��)
    public void OnPointerClick(PointerEventData eventData) 
    {
        if (StageManager.Instance.CurrentDeploymentCost >= operatorData.deploymentCost)
        {
            OperatorManager.Instance.StartOperatorPlacement(operatorData);
        }
    }

    private void UpdateAvailability()
    {
        bool isAvailable = StageManager.Instance.CurrentDeploymentCost >= operatorData.deploymentCost;
        Color iconColor = isAvailable ? originalIconColor : new Color(originalIconColor.r, originalIconColor.g, originalIconColor.b, 0.3f);
        operatorIconImage.color = iconColor;
        //costText.color = isAvailable ? Color.white : Color.gray;
    }
}
