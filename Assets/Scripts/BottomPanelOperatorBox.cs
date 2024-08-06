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
        // operatorData에 해당하는 아이콘이 있다면 그걸 넣고 
        if (operatorData.icon != null)
        {
            operatorIcon.sprite = operatorData.icon;
        }

        // 없다면 operatorData에 해당하는 색깔만 가져와서 할당한다.
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

    // eventData : 유니티에 의해 자동 제공, 클릭 이벤트 정보 포함. (그래서 operatorData랑은 별개로 인풋을 저렇게 작성해야 함)
    public void OnPointerClick(PointerEventData eventData) 
    {
        OperatorManager.Instance.StartOperatorPlacement(operatorData);
    }
}
