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
    public Image itemCountBackground = default!; // �ٸ� ��ũ��Ʈ���� �����

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
        // GetComponent �迭�� Awake���� �����Ѵ�.
        canvas = GetComponentInParent<Canvas>();
        canvasRectTransform = canvas.GetComponent<RectTransform>();
    }

    // ������ Ŭ�� �� ȣ�� �̺�Ʈ 
    //public System.Action<ItemData> OnItemClicked;

    public void Initialize(ItemData data, int count, bool isOnStageScene, bool isFirstClear = false, bool showNotEnough = false)
    {
        itemData = data;
        itemCount = count;
        this.isOnStageScene = isOnStageScene;

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

        // ù Ŭ���� �� ���޵Ǵ� ������ ǥ��
        firstClearImage.gameObject.SetActive(isFirstClear);

        // ������ ���� �ڽ� ǥ�� - promotionPanel������ ���
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
            Debug.Log("UIManager�� ShowItemPopup �޼��� ����");
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
