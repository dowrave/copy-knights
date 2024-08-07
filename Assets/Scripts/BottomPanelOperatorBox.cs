using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class BottomPanelOperatorBox : MonoBehaviour, IPointerClickHandler
{
    private Transform operatorIcon; // ������Ʈ
    private Image operatorIconImage; // Image ������Ʈ
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
                operatorIconImage.color = modelRenderer.sharedMaterial.color;
            }
        }

        costText.text = operatorData.deploymentCost.ToString(); 
    }

    // eventData : ����Ƽ�� ���� �ڵ� ����, Ŭ�� �̺�Ʈ ���� ����. (�׷��� operatorData���� ������ ��ǲ�� ������ �ۼ��ؾ� ��)
    public void OnPointerClick(PointerEventData eventData) 
    {
        OperatorManager.Instance.StartOperatorPlacement(operatorData);
    }
}
