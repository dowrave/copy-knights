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


    // �� ��ũ��Ʈ���� �� Area�� Ȱ��ȭ�� �����ϸ� ��������.

    // ���ʿ� �г��� UIManager.Awake���� Hide�� ȣ��Ǹ鼭 ���� ��Ȱ��ȭ��
    private void Awake()
    {
        // GetComponent �迭�� Awake���� �����Ѵ�.
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

        // �˾� ��� ��ġ �� Ȱ��ȭ 
        ActivatePopupElements();
    }

    private void ActivatePopupElements()
    {
        // (Horizontal Layout Group�� ����) ���̾ƿ� ������Ʈ�� ���� �� ������ ���
        //yield return new WaitForEndOfFrame();
        // Canvas.ForceUpdateCavases(); �� ���� ������Ʈ�� �����ϴٰ� ��

        popupItemNameText.text = itemData.itemName;
        popupItemDetailText.text = itemData.description;

        // ���� itemUIElement�� ��ġ ���
        RectTransform originalRect = itemUIElement.GetComponent<RectTransform>();
        Vector3 finalWorldPosition = originalRect.position;

        // backArea ���� ���� ���纻 �ν��Ͻ�ȭ
        copiedItem = Instantiate(itemUIElement.gameObject, transform, originalRect);

        // �˾� ����� ��ġ ����
        AdjustPopupLocation();

        // BackArea Ŭ�� �� popupArea�� �ݴ� ������ �߰�
        backArea.onClick.AddListener(OnBackAreaClicked);

        // ��ũ�� ��ġ �� ���� ����
        scrollRect.verticalNormalizedPosition = 1.0f;

        // ���纻�� ���� ���� �˾��� ��� - ���纻�� ���������� �����̴� ���� ����
        popupArea.gameObject.SetActive(true);
        //gameObject.SetActive(true);
    }



    // �˾��� ȭ�鿡�� �߸��� �������� ���
    private void AdjustPopupLocation()
    {
        // itemUIElement�� ��ġ�� ũ�� ��������
        RectTransform itemRectTransform = itemUIElement.GetComponent<RectTransform>();
        Vector3 itemWorldPosition = itemRectTransform.position;
        float itemWorldX = itemWorldPosition.x;

        // itemUIElement, �˾��� �ʺ�
        float itemUIWidth = itemRectTransform.rect.width;
        float popupWidth = popupRectTransform.rect.width;

        // ĵ������ ��� ��� (���� ��ǥ ���)
        float canvasRightEdge = canvasRectTransform.rect.width;

        Debug.Log($"������ ��ġ + UI �ʺ� + �˾� �ʺ� : {itemWorldX + itemUIWidth + popupWidth}");
        Debug.Log($"ĵ���� ���� : {canvasRightEdge}");


        // ���ǿ� ���� �˾� ��ġ ����
        if (itemWorldX + itemUIWidth + popupWidth > canvasRightEdge)
        {
            // ĵ���� ������ �ʰ��ϸ� ���ʿ� ���
            SetPopupLeft(itemWorldPosition, itemRectTransform.rect);
        }
        else
        {
            // �׷��� ������ �����ʿ� ���
            SetPopupRight(itemWorldPosition, itemRectTransform.rect);
        }
    }

    private void SetPopupLeft(Vector3 itemWorldPosition, Rect rect)
    {
        // �˾��� ��Ŀ, �ǹ� ����
        popupRectTransform.anchorMin = new Vector2(1, 1);
        popupRectTransform.anchorMax = new Vector2(1, 1);
        popupRectTransform.pivot = new Vector2(1, 1);

        // �˾��� ��ġ ����. Left�� ��� itemUIElement ���� ���� ����� ����
        // itemUIElement�� �ǹ��� (0.5, 0.5)��
        float popupX = itemWorldPosition.x - rect.width / 2;
        float popupY = itemWorldPosition.y + rect.height / 2;
        popupRectTransform.position = new Vector2(popupX, popupY);
        Debug.Log($"�˾��� ��ġ : {popupRectTransform.position}");


        // ������ �̸� ������Ʈ�� ��Ŀ, �ǹ� ����
        popupItemNameBackground.pivot = new Vector2(1, 0.5f);
        popupItemNameBackground.anchorMin = new Vector2(1, 0.5f);
        popupItemNameBackground.anchorMax = new Vector2(1, 0.5f);

        // ������ �̸� ������Ʈ�� ��ġ ����
        popupItemNameBackground.anchoredPosition = new Vector2(-originalItemNamePosition.x,
            originalItemNamePosition.y);
    }

    private void SetPopupRight(Vector3 itemWorldPosition, Rect rect)
    {

        // ��Ŀ, �ǹ� ����
        popupRectTransform.anchorMin = new Vector2(0, 1);
        popupRectTransform.anchorMax = new Vector2(0, 1);
        popupRectTransform.pivot = new Vector2(0, 1);

        // �˾��� ��ġ ����
        float popupX = itemWorldPosition.x + rect.width / 2;
        float popupY = itemWorldPosition.y + rect.height / 2;
        popupRectTransform.position = new Vector2(popupX, popupY);
        Debug.Log($"�˾��� ��ġ : {popupRectTransform.position}");

        popupItemNameBackground.pivot = new Vector2(0, 0.5f);
        popupItemNameBackground.anchorMin = new Vector2(0, 0.5f);
        popupItemNameBackground.anchorMax = new Vector2(0, 0.5f);

        popupItemNameBackground.anchoredPosition = originalItemNamePosition;
    }


    private IEnumerator WaitAndActivatePopupElements()
    {
        // (Horizontal Layout Group�� ����) ���̾ƿ� ������Ʈ�� ���� �� ������ ���
        yield return new WaitForEndOfFrame();
        // Canvas.ForceUpdateCavases(); �� ���� ������Ʈ�� �����ϴٰ� ��

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
