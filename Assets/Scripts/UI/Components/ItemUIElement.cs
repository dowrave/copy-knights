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

    [Header("OnClick Detail Panel")]
    [SerializeField] private Image detailPanel;
    [SerializeField] private Button detailBackArea;
    [SerializeField] private TextMeshProUGUI detailPanelItemNameText;
    [SerializeField] private TextMeshProUGUI detailPanelItemDetailText;
    [SerializeField] private RectTransform detailPanelItemNameBackground;

    [Header("Visual Settings")]
    [SerializeField] private Color commonColor = new Color(0.7f, 0.7f, 0.7f);
    [SerializeField] private Color rareColor = new Color(0.2f, 0.5f, 1f);
    [SerializeField] private Color epicColor = new Color(0.8f, 0.3f, 1f);

    private ItemData itemData;
    private int itemCount;
    private bool isOnStageScene;


    // 아이템 클릭 시 호출 이벤트 
    //public System.Action<ItemData> OnItemClicked;

    public void Initialize(ItemData data, int count, bool isOnStageScene)
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

        detailPanel.gameObject.SetActive(false);

        // detailPanel 설정
        if (isOnStageScene)
        {
            detailPanelItemNameText.text = itemData.itemName;
            detailPanelItemDetailText.text = itemData.description;
        }
    }

    public void UpdateCount(int newCount)
    {
        itemCount = newCount;
        countText.text = itemCount.ToString();
    }

    private void OnBackAreaClicked()
    {
        Debug.Log("ItemUIElement의 뒷배경이 클릭되었음");
        detailPanel.gameObject.SetActive(false);
        detailBackArea.onClick.RemoveAllListeners();
    }

    // 디테일 패널이 화면에서 잘리는 경우 대비
    //private void AdjustDetailPanel()
    //{
    //    Canvas canvas = GetComponentInParent<Canvas>();
    //    RectTransform canvasRectTransform = canvas.GetComponent<RectTransform>();
    //    RectTransform detailPanelRectTransform = detailPanel.GetComponent<RectTransform>();

    //    // 1. ItemUIElement의 월드 좌표를 RectTransformUtility.WorldToScreenPoint를 사용해 스크린 좌표로 변환.
    //    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, transform.position);

    //    // 2. 스크린 좌표를 Canvas 내의 로컬 좌표로 변환.
    //    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, screenPoint, canvas.worldCamera, out Vector2 localPoint);

    //    Debug.Log($"localPoint : {localPoint}");

    //    float canvasWidth = canvasRectTransform.rect.width;
    //    float canvasHeight = canvasRectTransform.rect.height;

    //    // UIElement 기준 로컬 x 포지션 설정 - 주의) rectTransform.position은 월드 포지션
    //    float detailPanelPosX = detailPanelRectTransform.anchoredPosition.x;

    //    // 피벗이 중앙이라서 너비를 2로 나눔
    //    float detailPanelRightEdge = localPoint.x + detailPanelPosX + detailPanelRectTransform.rect.width / 2;

    //    Debug.Log($"화면 오른쪽 변 위치 : {detailPanelRightEdge}");
    //    Debug.Log($"캔버스 너비 : {canvasWidth}");


    //    // 3. 디테일 패널의 오른쪽 변이 화면에서 벗어나는 경우, 왼쪽으로 디테일 패널을 옮김
    //    if (detailPanelRightEdge > canvasWidth)
    //    {
    //        Debug.Log("detailPanelRightEdge가 canvasWidth보다 크다");
    //        Debug.Log($"초기 detailPanelPosX : {detailPanelPosX}");
    //        Debug.Log($"초기 아이템 이름 위치 값 : {detailPanelItemNameBackground.anchoredPosition}");


    //        // detailPanel 위치 왼쪽으로 변경
    //        detailPanelPosX = -detailPanelPosX;

    //        // 패널 내의 아이템 이름을 오른쪽으로 옮김
    //        detailPanelItemNameBackground.pivot = new Vector2(1, 0.5f);
    //        detailPanelItemNameBackground.anchorMin = new Vector2(1, 0.5f);
    //        detailPanelItemNameBackground.anchorMax = new Vector2(1, 0.5f);

    //        Vector2 defaultItemNameBackgroundPosition = detailPanelItemNameBackground.anchoredPosition;
    //        detailPanelItemNameBackground.anchoredPosition = new Vector2(-defaultItemNameBackgroundPosition.x, -defaultItemNameBackgroundPosition.y);
    //    }
    //    // 실시간으로 위치를 변화시킬 게 아니라서 이 정도로만 구현함
    //}

    public void OnPointerClick(PointerEventData eventData)
    {
        //OnItemClicked?.Invoke(itemData);
        if (isOnStageScene)
        {
            //AdjustDetailPanel();
            detailPanel.gameObject.SetActive(true);
            detailBackArea.onClick.AddListener(OnBackAreaClicked);
        }
        else
        {
            PopupManager.Instance.ShowItemInfoPopup(itemData);
        }
    }

    public (ItemData data, int count) getItemInfo()
    {
        return (itemData, itemCount);
    }
}
