using NUnit.Framework.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class StageItemInfoPopup : MonoBehaviour
{
    [Header("OnClick Detail Panel")]
    [SerializeField] private Button backArea = default!;
    [SerializeField] private Image detailArea = default!;
    [SerializeField] private TextMeshProUGUI popupItemNameText = default!;
    [SerializeField] private TextMeshProUGUI popupItemDetailText = default!;
    [SerializeField] private RectTransform popupItemNameBackground = default!;

    private ItemUIElement itemUIElement = default!;
    private RectTransform popupRectTransform = default!;
    private Canvas canvas = default!;
    private RectTransform canvasRectTransform = default!;

    private void Awake()
    {
        // GetComponent 계열은 Awake에서 수행한다.
        canvas = GetComponentInParent<Canvas>();
        canvasRectTransform = canvas.GetComponent<RectTransform>();
        popupRectTransform = detailArea.GetComponent<RectTransform>();
    }

    public void Show(ItemUIElement itemUIElement)
    {
        this.itemUIElement = itemUIElement;
        ItemData itemData = itemUIElement.ItemData;
        popupItemNameText.text = itemData.itemName;
        popupItemDetailText.text = itemData.description;

        // itemUIElement의 위치를 가져옴
        RectTransform itemRectTransform = itemUIElement.GetComponent<RectTransform>();
        Vector3 itemWorldPosition = itemRectTransform.position;

        // 위치 설정
        AdjustPopupLocation(itemWorldPosition);

        // backArea의 크기를 Canvas에 맞춤
        Rect canvasRect = canvasRectTransform.rect;
        RectTransform backAreaRectTransform = backArea.GetComponent<RectTransform>();
        backAreaRectTransform.position = -canvasRect.position; // canvasRect.position이 -960, -540으로 잡히는 원인 모를 문제가 있어서 일단 이렇게 설정
        backAreaRectTransform.sizeDelta = new Vector2(canvasRect.width, canvasRect.height);

        detailArea.gameObject.SetActive(true);
        backArea.gameObject.SetActive(true);

        // BackArea 클릭 시 detailArea을 닫는 리스너 추가
        backArea.onClick.AddListener(OnBackAreaClicked);
    }

    // 팝업이 화면에서 잘리면 왼쪽으로 띄움
    private void AdjustPopupLocation(Vector3 referenceWorldPosition)
    {
        // 월드 좌표를 쓰는 이유 : 캔버스 내의 절대적인 위치 개념임. 스크린 좌표는 해상도에 따라 값이 달라질 수 있다.
        float itemWorldX = referenceWorldPosition.x;

        // detailArea의 월드 좌표의 우측 가장자리 계산
        Vector3[] PopupCorners = new Vector3[4];
        popupRectTransform.GetWorldCorners(PopupCorners);
        float popupRightEdgeWorldX = PopupCorners[2].x; // 우측 상단 코너의 x좌표

        // 캔버스의 월드 좌표에서의 우측 가장자리 계산
        Vector3[] canvasCorners = new Vector3[4];
        canvasRectTransform.GetWorldCorners(canvasCorners);
        float canvasRightEdgeWorldX = canvasCorners[2].x; // 캔버스 우측 상단의 x좌표

        // 디테일 패널의 오른쪽 변이 화면에서 벗어나는 경우, 왼쪽으로 디테일 패널을 옮김
        if (popupRightEdgeWorldX > canvasRightEdgeWorldX)
        {
            SetPopupLeft();
        }
        else // 좌측으로 넘어가지 않더라도 detailArea의 위치를 잡아준다
        {
            SetPopupRight();
        }

        // 실시간으로 위치를 변화시킬 게 아니라서 이 정도로만 구현함
    }

    private void SetPopupLeft()
    {
        // 디테일 패널의 앵커, 피벗 변경
        popupRectTransform.anchorMin = new Vector2(1, 1);
        popupRectTransform.anchorMax = new Vector2(1, 1);
        popupRectTransform.pivot = new Vector2(1, 1);

        // 디테일 패널의 위치 변경
        Vector2 defaultDetailPanelLocalPosition = popupRectTransform.anchoredPosition;
        popupRectTransform.anchoredPosition = new Vector2(-defaultDetailPanelLocalPosition.x, defaultDetailPanelLocalPosition.y);

        // 아이템 이름 오브젝트의 앵커, 피벗 변경
        popupItemNameBackground.pivot = new Vector2(1, 0.5f);
        popupItemNameBackground.anchorMin = new Vector2(1, 0.5f);
        popupItemNameBackground.anchorMax = new Vector2(1, 0.5f);

        // 아이템 이름 오브젝트의 위치 변경
        popupItemNameBackground.anchoredPosition = new Vector2(-popupItemNameBackground.anchoredPosition.x,
            popupItemNameBackground.anchoredPosition.y);
    }

    private void SetPopupRight()
    {
        // 프리팹의 기본 설정과 동일해서 사실 만지진 않아도 됨

        popupRectTransform.anchorMin = new Vector2(0, 1);
        popupRectTransform.anchorMax = new Vector2(0, 1);
        popupRectTransform.pivot = new Vector2(0, 1);

        popupItemNameBackground.pivot = new Vector2(0, 0.5f);
        popupItemNameBackground.anchorMin = new Vector2(0, 0.5f);
        popupItemNameBackground.anchorMax = new Vector2(0, 0.5f);
    }


    public void Hide()
    {
        detailArea.gameObject.SetActive(false);

        backArea.onClick.RemoveAllListeners();
        backArea.gameObject.SetActive(false);
    }

    private void OnBackAreaClicked()
    {
        UIManager.Instance!.HideItemPopup();
    }
}
