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
        // GetComponent �迭�� Awake���� �����Ѵ�.
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

        // itemUIElement�� ��ġ�� ������
        RectTransform itemRectTransform = itemUIElement.GetComponent<RectTransform>();
        Vector3 itemWorldPosition = itemRectTransform.position;

        // ��ġ ����
        AdjustPopupLocation(itemWorldPosition);

        // backArea�� ũ�⸦ Canvas�� ����
        Rect canvasRect = canvasRectTransform.rect;
        RectTransform backAreaRectTransform = backArea.GetComponent<RectTransform>();
        backAreaRectTransform.position = -canvasRect.position; // canvasRect.position�� -960, -540���� ������ ���� �� ������ �־ �ϴ� �̷��� ����
        backAreaRectTransform.sizeDelta = new Vector2(canvasRect.width, canvasRect.height);

        detailArea.gameObject.SetActive(true);
        backArea.gameObject.SetActive(true);

        // BackArea Ŭ�� �� detailArea�� �ݴ� ������ �߰�
        backArea.onClick.AddListener(OnBackAreaClicked);
    }

    // �˾��� ȭ�鿡�� �߸��� �������� ���
    private void AdjustPopupLocation(Vector3 referenceWorldPosition)
    {
        // ���� ��ǥ�� ���� ���� : ĵ���� ���� �������� ��ġ ������. ��ũ�� ��ǥ�� �ػ󵵿� ���� ���� �޶��� �� �ִ�.
        float itemWorldX = referenceWorldPosition.x;

        // detailArea�� ���� ��ǥ�� ���� �����ڸ� ���
        Vector3[] PopupCorners = new Vector3[4];
        popupRectTransform.GetWorldCorners(PopupCorners);
        float popupRightEdgeWorldX = PopupCorners[2].x; // ���� ��� �ڳ��� x��ǥ

        // ĵ������ ���� ��ǥ������ ���� �����ڸ� ���
        Vector3[] canvasCorners = new Vector3[4];
        canvasRectTransform.GetWorldCorners(canvasCorners);
        float canvasRightEdgeWorldX = canvasCorners[2].x; // ĵ���� ���� ����� x��ǥ

        // ������ �г��� ������ ���� ȭ�鿡�� ����� ���, �������� ������ �г��� �ű�
        if (popupRightEdgeWorldX > canvasRightEdgeWorldX)
        {
            SetPopupLeft();
        }
        else // �������� �Ѿ�� �ʴ��� detailArea�� ��ġ�� ����ش�
        {
            SetPopupRight();
        }

        // �ǽð����� ��ġ�� ��ȭ��ų �� �ƴ϶� �� �����θ� ������
    }

    private void SetPopupLeft()
    {
        // ������ �г��� ��Ŀ, �ǹ� ����
        popupRectTransform.anchorMin = new Vector2(1, 1);
        popupRectTransform.anchorMax = new Vector2(1, 1);
        popupRectTransform.pivot = new Vector2(1, 1);

        // ������ �г��� ��ġ ����
        Vector2 defaultDetailPanelLocalPosition = popupRectTransform.anchoredPosition;
        popupRectTransform.anchoredPosition = new Vector2(-defaultDetailPanelLocalPosition.x, defaultDetailPanelLocalPosition.y);

        // ������ �̸� ������Ʈ�� ��Ŀ, �ǹ� ����
        popupItemNameBackground.pivot = new Vector2(1, 0.5f);
        popupItemNameBackground.anchorMin = new Vector2(1, 0.5f);
        popupItemNameBackground.anchorMax = new Vector2(1, 0.5f);

        // ������ �̸� ������Ʈ�� ��ġ ����
        popupItemNameBackground.anchoredPosition = new Vector2(-popupItemNameBackground.anchoredPosition.x,
            popupItemNameBackground.anchoredPosition.y);
    }

    private void SetPopupRight()
    {
        // �������� �⺻ ������ �����ؼ� ��� ������ �ʾƵ� ��

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
