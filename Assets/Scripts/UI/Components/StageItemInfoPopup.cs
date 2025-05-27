using System.Collections;
using NUnit.Framework.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class StageItemInfoPopup : MonoBehaviour
{
    [Header("OnClick Detail Panel")]
    [SerializeField] private Button backArea = default!;
    [SerializeField] private Image popupArea = default!;
    [SerializeField] private TextMeshProUGUI popupItemNameText = default!;
    [SerializeField] private TextMeshProUGUI popupItemDetailText = default!;
    [SerializeField] private RectTransform popupItemNameBackground = default!;
    [SerializeField] private ScrollRect scrollRect = default!;

    private ItemData itemData = default!;

    private ItemUIElement itemUIElement = default!;
    private RectTransform popupRectTransform = default!;
    private Canvas canvas = default!;
    private RectTransform canvasRectTransform = default!;
    private GameObject copiedItem = default!;

    private Vector2 originalItemNamePosition = default!;


    // 이 스크립트에서 각 Area의 활성화는 웬만하면 지켜주자.

    // 최초에 패널은 UIManager.Awake에서 Hide가 호출되면서 의해 비활성화됨
    private void Awake()
    {
        // GetComponent 계열은 Awake에서 수행한다.
        canvas = GetComponentInParent<Canvas>();
        canvasRectTransform = canvas.GetComponent<RectTransform>();
        popupRectTransform = popupArea.GetComponent<RectTransform>();

        Debug.Log($"popupRectTransform : {popupRectTransform}");

        originalItemNamePosition = popupItemNameBackground.anchoredPosition;
    }

    public void Show(ItemUIElement itemUIElement)
    {
        gameObject.SetActive(true);

        this.itemUIElement = itemUIElement;
        itemData = itemUIElement.ItemData;

        // 팝업 요소 배치 및 활성화 
        ActivatePopupElements();
    }

    private void ActivatePopupElements()
    {
        // (Horizontal Layout Group에 의한) 레이아웃 업데이트를 위한 한 프레임 대기
        //yield return new WaitForEndOfFrame();
        // Canvas.ForceUpdateCavases(); 로 강제 업데이트도 가능하다고 함

        popupItemNameText.text = itemData.itemName;
        popupItemDetailText.text = itemData.description;

        // 기존 itemUIElement의 위치 얻기
        RectTransform originalRect = itemUIElement.GetComponent<RectTransform>();
        Vector3 finalWorldPosition = originalRect.position;

        // backArea 위에 오는 복사본 인스턴스화
        copiedItem = Instantiate(itemUIElement.gameObject, transform, originalRect);

        // 팝업 요소의 위치 설정
        AdjustPopupLocation();

        // BackArea 클릭 시 popupArea을 닫는 리스너 추가
        backArea.onClick.AddListener(OnBackAreaClicked);

        // 스크롤 위치 맨 위로 설정
        scrollRect.verticalNormalizedPosition = 1.0f;

        // 복사본을 만든 다음 팝업을 띄움 - 복사본이 순간적으로 깜빡이는 현상 방지
        popupArea.gameObject.SetActive(true);
        //gameObject.SetActive(true);
    }



    // 팝업이 화면에서 잘리면 왼쪽으로 띄움
    private void AdjustPopupLocation()
    {
        // itemUIElement의 위치와 크기 가져오기
        RectTransform itemRectTransform = itemUIElement.GetComponent<RectTransform>();
        Vector3 itemWorldPosition = itemRectTransform.position;
        float itemWorldX = itemWorldPosition.x;

        // itemUIElement, 팝업의 너비
        float itemUIWidth = itemRectTransform.rect.width;
        float popupWidth = popupRectTransform.rect.width;

        // 캔버스의 경계 계산 (월드 좌표 사용)
        float canvasRightEdge = canvasRectTransform.rect.width;

        Debug.Log($"아이템 위치 + UI 너비 + 팝업 너비 : {itemWorldX + itemUIWidth + popupWidth}");
        Debug.Log($"캔버스 우측 : {canvasRightEdge}");


        // 조건에 따라 팝업 위치 설정
        if (itemWorldX + itemUIWidth + popupWidth > canvasRightEdge)
        {
            // 캔버스 우측을 초과하면 왼쪽에 띄움
            SetPopupLeft(itemWorldPosition, itemRectTransform.rect);
        }
        else
        {
            // 그렇지 않으면 오른쪽에 띄움
            SetPopupRight(itemWorldPosition, itemRectTransform.rect);
        }
    }

    private void SetPopupLeft(Vector3 itemWorldPosition, Rect rect)
    {
        // 팝업의 앵커, 피벗 변경
        popupRectTransform.anchorMin = new Vector2(1, 1);
        popupRectTransform.anchorMax = new Vector2(1, 1);
        popupRectTransform.pivot = new Vector2(1, 1);

        // 팝업의 위치 설정. Left의 경우 itemUIElement 기준 좌측 상단을 잡음
        // itemUIElement는 피벗이 (0.5, 0.5)임
        float popupX = itemWorldPosition.x - rect.width / 2;
        float popupY = itemWorldPosition.y + rect.height / 2;
        popupRectTransform.position = new Vector2(popupX, popupY);
        Debug.Log($"팝업의 위치 : {popupRectTransform.position}");


        // 아이템 이름 오브젝트의 앵커, 피벗 변경
        popupItemNameBackground.pivot = new Vector2(1, 0.5f);
        popupItemNameBackground.anchorMin = new Vector2(1, 0.5f);
        popupItemNameBackground.anchorMax = new Vector2(1, 0.5f);

        // 아이템 이름 오브젝트의 위치 변경
        popupItemNameBackground.anchoredPosition = new Vector2(-originalItemNamePosition.x,
            originalItemNamePosition.y);
    }

    private void SetPopupRight(Vector3 itemWorldPosition, Rect rect)
    {

        // 앵커, 피벗 설정
        popupRectTransform.anchorMin = new Vector2(0, 1);
        popupRectTransform.anchorMax = new Vector2(0, 1);
        popupRectTransform.pivot = new Vector2(0, 1);

        // 팝업의 위치 설정
        float popupX = itemWorldPosition.x + rect.width / 2;
        float popupY = itemWorldPosition.y + rect.height / 2;
        popupRectTransform.position = new Vector2(popupX, popupY);
        Debug.Log($"팝업의 위치 : {popupRectTransform.position}");

        popupItemNameBackground.pivot = new Vector2(0, 0.5f);
        popupItemNameBackground.anchorMin = new Vector2(0, 0.5f);
        popupItemNameBackground.anchorMax = new Vector2(0, 0.5f);

        popupItemNameBackground.anchoredPosition = originalItemNamePosition;
    }


    private IEnumerator WaitAndActivatePopupElements()
    {
        // (Horizontal Layout Group에 의한) 레이아웃 업데이트를 위한 한 프레임 대기
        yield return new WaitForEndOfFrame();
        // Canvas.ForceUpdateCavases(); 로 강제 업데이트도 가능하다고 함

        ActivatePopupElements();
    }


    public void Hide()
    {
        Destroy(copiedItem);

        popupArea.gameObject.SetActive(false);

        backArea.onClick.RemoveAllListeners();
        gameObject.SetActive(false);
    }

    private void OnBackAreaClicked()
    {
        StageUIManager.Instance!.HideItemPopup();
    }
}
