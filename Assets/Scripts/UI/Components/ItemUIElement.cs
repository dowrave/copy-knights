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
    [SerializeField] private Button backArea;
    public Image itemCountBackground;

    [Header("OnClick Detail Panel")]
    [SerializeField] private Image detailPanel;
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

    private Canvas canvas;
    private RectTransform canvasRectTransform;
    private RectTransform detailPanelRectTransform;


    private void Awake()
    {
        // GetComponent 계열은 Awake에서 수행한다. 많이 쓰는 건 좋지 않지만.
        canvas = GetComponentInParent<Canvas>();
        canvasRectTransform = canvas.GetComponent<RectTransform>();
        detailPanelRectTransform = detailPanel.GetComponent<RectTransform>();
    }

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

        HideDetailPanel();

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
        HideDetailPanel();
    }

    // 디테일 패널이 화면에서 잘리는 경우 패널을 왼쪽으로 띄우는 메서드
    private void AdjustDetailPanel()
    {
        // 월드 좌표를 쓰는 이유 : 캔버스 내의 절대적인 위치 개념임. 스크린 좌표는 해상도에 따라 값이 달라질 수 있다.

        // detailPanel의 월드 좌표의 우측 가장자리 계산
        Vector3[] detailPanelCorners = new Vector3[4];
        detailPanelRectTransform.GetWorldCorners(detailPanelCorners);
        float detailPanelRightEdgeWorldX = detailPanelCorners[2].x; // 우측 상단 코너의 x좌표

        // 캔버스의 월드 좌표에서의 우측 가장자리 계산
        Vector3[] canvasCorners = new Vector3[4];
        canvasRectTransform.GetWorldCorners(canvasCorners);
        float canvasRightEdgeWorldX = canvasCorners[2].x; // 캔버스 우측 상단의 x좌표

        // 디테일 패널의 오른쪽 변이 화면에서 벗어나는 경우, 왼쪽으로 디테일 패널을 옮김
        if (detailPanelRightEdgeWorldX > canvasRightEdgeWorldX)
        {
            Debug.Log("detailPanelRightEdge가 canvasWidth보다 크다");
            SetDetailPanelLeft();
        }
        else // 좌측으로 넘어가지 않더라도 detailPanel의 위치를 잡아준다
        {
            SetDetailPanelRight();
        }

        // 실시간으로 위치를 변화시킬 게 아니라서 이 정도로만 구현함
    }

    private void SetDetailPanelLeft()
    {
        // 디테일 패널의 앵커, 피벗 변경
        detailPanelRectTransform.anchorMin = new Vector2(1, 1);
        detailPanelRectTransform.anchorMax = new Vector2(1, 1);
        detailPanelRectTransform.pivot = new Vector2(1, 1);

        // 디테일 패널의 위치 변경
        Vector2 defaultDetailPanelLocalPosition = detailPanelRectTransform.anchoredPosition;
        detailPanelRectTransform.anchoredPosition = new Vector2(-defaultDetailPanelLocalPosition.x, defaultDetailPanelLocalPosition.y);

        // 아이템 이름 오브젝트의 앵커, 피벗 변경
        detailPanelItemNameBackground.pivot = new Vector2(1, 0.5f);
        detailPanelItemNameBackground.anchorMin = new Vector2(1, 0.5f);
        detailPanelItemNameBackground.anchorMax = new Vector2(1, 0.5f);

        // 아이템 이름 오브젝트의 위치 변경
        detailPanelItemNameBackground.anchoredPosition = new Vector2(-detailPanelItemNameBackground.anchoredPosition.x, 
            detailPanelItemNameBackground.anchoredPosition.y);
    }

    // DetailPanel과 BackArea는 함께 동작해야 함

    private void ShowDetailPanel()
    {
        // DetailPanel 위치 설정
        AdjustDetailPanel();

        // backArea의 크기를 Canvas에 맞춤
        Rect canvasRect = canvasRectTransform.rect;
        RectTransform backAreaRectTransform = backArea.GetComponent<RectTransform>();
        backAreaRectTransform.position = -canvasRect.position; // canvasRect.position이 -960, -540으로 잡히는 원인 모를 문제가 있어서 일단 이렇게 설정
        backAreaRectTransform.sizeDelta = new Vector2(canvasRect.width, canvasRect.height);

        detailPanel.gameObject.SetActive(true);
        backArea.gameObject.SetActive(true);

        // BackArea 클릭 시 detailPanel을 닫는 리스너 추가
        backArea.onClick.AddListener(OnBackAreaClicked);
    }

    private void HideDetailPanel()
    {
        detailPanel.gameObject.SetActive(false);

        backArea.onClick.RemoveAllListeners();
        backArea.gameObject.SetActive(false);
    }

    private void SetDetailPanelRight()
    {
        // 프리팹의 기본 설정과 동일해서 사실 만지진 않아도 됨

        detailPanelRectTransform.anchorMin = new Vector2(0, 1);
        detailPanelRectTransform.anchorMax = new Vector2(0, 1);
        detailPanelRectTransform.pivot = new Vector2(0, 1);

        detailPanelItemNameBackground.pivot = new Vector2(0, 0.5f);
        detailPanelItemNameBackground.anchorMin = new Vector2(0, 0.5f);
        detailPanelItemNameBackground.anchorMax = new Vector2(0, 0.5f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //OnItemClicked?.Invoke(itemData);
        if (isOnStageScene)
        {
            ShowDetailPanel();
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
