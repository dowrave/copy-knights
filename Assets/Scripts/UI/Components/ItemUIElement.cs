using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class ItemUIElement : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Components")]
    [SerializeField] private Image backgroundImage = default!;
    [SerializeField] private Image itemIconImage = default!;
    [SerializeField] private TextMeshProUGUI countText = default!;
    [SerializeField] private Image firstClearImage = default!;
    [SerializeField] private Image notEnoughImage = default!; 
    public Image itemCountBackground = default!; // 다른 스크립트에서 사용함

    [Header("Visual Settings")]
    [SerializeField] private Color commonColor = new Color(0.7f, 0.7f, 0.7f);
    [SerializeField] private Color rareColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color epicColor = new Color(0.8f, 0.3f, 1f);

    private ItemData itemData = default!;
    public ItemData ItemData => itemData;
    private int itemCount;
    private bool isOnStageScene;

    private Canvas canvas = default!;
    private RectTransform canvasRectTransform = default!;

    private void Awake()
    {
        // GetComponent 계열은 Awake에서 수행한다.
        canvas = GetComponentInParent<Canvas>();
        canvasRectTransform = canvas.GetComponent<RectTransform>();
    }

    // 아이템 클릭 시 호출 이벤트 
    //public System.Action<ItemData> OnItemClicked;

    public void Initialize(ItemData data, int count, bool isOnStageScene, bool isFirstClear = false, bool showNotEnough = false)
    {
        itemData = data;
        itemCount = count;
        this.isOnStageScene = isOnStageScene;

        // 아이템 아이콘 설정
        if (itemData.icon != null)
        {
            itemIconImage.sprite = itemData.icon;
            itemIconImage.enabled = true;
        }
        else
        {
            itemIconImage.enabled = false;
        }

        // 테두리 색상 : 아이템 타입에 따라 다른 색 적용
        Color borderColor = itemData.type switch
        {
            ItemData.ItemType.Exp => rareColor,
            ItemData.ItemType.EliteItem => epicColor,
            _ => commonColor
        };
        backgroundImage.color = borderColor;

        UpdateCount(count);

        // 첫 클리어 시 지급되는 아이템 표시
        firstClearImage.gameObject.SetActive(isFirstClear);

        // 아이템 부족 박스 표시 - promotionPanel에서만 사용
        notEnoughImage.gameObject.SetActive(showNotEnough);
        
    }

    public void UpdateCount(int newCount)
    {
        itemCount = newCount;
        countText.text = itemCount.ToString();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isOnStageScene)
        {
            Debug.Log("UIManager의 ShowItemPopup 메서드 동작");
            UIManager.Instance!.ShowItemPopup(this);
        }
        else if (PopupManager.Instance != null)
        {
            PopupManager.Instance.ShowItemInfoPopup(itemData);
        }
    }

    private void OnEnable()
    {
        notEnoughImage.gameObject.SetActive(false);
    }

    public (ItemData data, int count) getItemInfo()
    {
        return (itemData, itemCount);
    }
}
