using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class BottomPanelOperatorBox : MonoBehaviour, IPointerClickHandler
{
    private Transform operatorIcon; // 오브젝트
    private Image operatorIconImage; // Image 컴포넌트
    private TextMeshProUGUI costText;
    private OperatorData operatorData; 

    public void Initialize(OperatorData data)
    {
        operatorData = data;
        operatorIcon = transform.Find("OperatorIcon");

        if (operatorIcon != null)
        {
            operatorIconImage = operatorIcon.GetComponentInChildren<Image>(); // OperatorIcon의 Image 컴포넌트에 접근
            costText = GetComponentInChildren<TextMeshProUGUI>(); // CostText의 TextMeshPro에 접근
        }
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // operatorData에 해당하는 아이콘이 있다면 그걸 넣고 
        if (operatorData.icon != null)
        {
            operatorIconImage.sprite = operatorData.icon;
            operatorIconImage.color = new Color(1f, 1f, 1f, 1f); // 일러스트에 변화를 주지 않고 그대로 넣음
        }

        // 없다면 operatorData에 해당하는 색깔만 가져와서 할당한다.
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

    // eventData : 유니티에 의해 자동 제공, 클릭 이벤트 정보 포함. (그래서 operatorData랑은 별개로 인풋을 저렇게 작성해야 함)
    public void OnPointerClick(PointerEventData eventData) 
    {
        OperatorManager.Instance.StartOperatorPlacement(operatorData);
    }
}
