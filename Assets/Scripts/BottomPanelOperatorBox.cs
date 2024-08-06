using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BottomPanelOperatorBox : MonoBehaviour
{
    private Image operatorIcon;
    private Text costText;
    private OperatorData operatorData; 

    public void Initialize(OperatorData data)
    {
        operatorData = data;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // operatorData�� �ش��ϴ� �������� �ִٸ� �װ� �ְ� 
        if (operatorData.icon != null)
        {
            operatorIcon.sprite = operatorData.icon;
        }

        // ���ٸ� operatorData�� �ش��ϴ� ���� �����ͼ� �Ҵ��Ѵ�.
        else if (operatorData.prefab != null)
        {
            Renderer modelRenderer = operatorData.prefab.GetComponentInChildren<Renderer>();
            if (modelRenderer != null && modelRenderer.sharedMaterial != null)
            {
                Debug.Log($"{operatorIcon}");
                
                operatorIcon.color = modelRenderer.sharedMaterial.color;
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
