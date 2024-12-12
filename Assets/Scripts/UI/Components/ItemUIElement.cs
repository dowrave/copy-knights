using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class ItemUIElement : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Components")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TextMeshProUGUI countText;
    public Image itemCountBackground;

    [Header("Visual Settings")]
    [SerializeField] private Color commonColor = new Color(0.7f, 0.7f, 0.7f);
    [SerializeField] private Color rareColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color epicColor = new Color(0.8f, 0.3f, 1f);



    private ItemData itemData;
    private int itemCount;


    // ������ Ŭ�� �� ȣ�� �̺�Ʈ 
    public System.Action<ItemData> OnItemClicked;

    public void Initialize(ItemData data, int count)
    {
        itemData = data;
        itemCount = count; 

        // ������ ������ ����
        if (itemData.icon != null)
        {
            itemIconImage.sprite = itemData.icon;
            itemIconImage.enabled = true;
        }
        else
        {
            itemIconImage.enabled = false;
        }

        // �׵θ� ���� : ������ Ÿ�Կ� ���� �ٸ� �� ����
        Color borderColor = itemData.type switch
        {
            ItemData.ItemType.Exp => rareColor,
            ItemData.ItemType.EliteItem => epicColor,
            _ => commonColor
        };
        backgroundImage.color = borderColor;

        UpdateCount(count);
    }

    public void UpdateCount(int newCount)
    {
        itemCount = newCount;
        countText.text = itemCount.ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnItemClicked?.Invoke(itemData);
        PopupManager.Instance.ShowItemInfoPopup(itemData);
    }

    public (ItemData data, int count) getItemInfo()
    {
        return (itemData, itemCount);
    }
}
