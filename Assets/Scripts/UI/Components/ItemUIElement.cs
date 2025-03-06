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
        // GetComponent �迭�� Awake���� �����Ѵ�. ���� ���� �� ���� ������.
        canvas = GetComponentInParent<Canvas>();
        canvasRectTransform = canvas.GetComponent<RectTransform>();
        detailPanelRectTransform = detailPanel.GetComponent<RectTransform>();
    }

    // ������ Ŭ�� �� ȣ�� �̺�Ʈ 
    //public System.Action<ItemData> OnItemClicked;

    public void Initialize(ItemData data, int count, bool isOnStageScene)
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

        HideDetailPanel();

        // detailPanel ����
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
        Debug.Log("ItemUIElement�� �޹���� Ŭ���Ǿ���");
        HideDetailPanel();
    }

    // ������ �г��� ȭ�鿡�� �߸��� ��� �г��� �������� ���� �޼���
    private void AdjustDetailPanel()
    {
        // ���� ��ǥ�� ���� ���� : ĵ���� ���� �������� ��ġ ������. ��ũ�� ��ǥ�� �ػ󵵿� ���� ���� �޶��� �� �ִ�.

        // detailPanel�� ���� ��ǥ�� ���� �����ڸ� ���
        Vector3[] detailPanelCorners = new Vector3[4];
        detailPanelRectTransform.GetWorldCorners(detailPanelCorners);
        float detailPanelRightEdgeWorldX = detailPanelCorners[2].x; // ���� ��� �ڳ��� x��ǥ

        // ĵ������ ���� ��ǥ������ ���� �����ڸ� ���
        Vector3[] canvasCorners = new Vector3[4];
        canvasRectTransform.GetWorldCorners(canvasCorners);
        float canvasRightEdgeWorldX = canvasCorners[2].x; // ĵ���� ���� ����� x��ǥ

        // ������ �г��� ������ ���� ȭ�鿡�� ����� ���, �������� ������ �г��� �ű�
        if (detailPanelRightEdgeWorldX > canvasRightEdgeWorldX)
        {
            Debug.Log("detailPanelRightEdge�� canvasWidth���� ũ��");
            SetDetailPanelLeft();
        }
        else // �������� �Ѿ�� �ʴ��� detailPanel�� ��ġ�� ����ش�
        {
            SetDetailPanelRight();
        }

        // �ǽð����� ��ġ�� ��ȭ��ų �� �ƴ϶� �� �����θ� ������
    }

    private void SetDetailPanelLeft()
    {
        // ������ �г��� ��Ŀ, �ǹ� ����
        detailPanelRectTransform.anchorMin = new Vector2(1, 1);
        detailPanelRectTransform.anchorMax = new Vector2(1, 1);
        detailPanelRectTransform.pivot = new Vector2(1, 1);

        // ������ �г��� ��ġ ����
        Vector2 defaultDetailPanelLocalPosition = detailPanelRectTransform.anchoredPosition;
        detailPanelRectTransform.anchoredPosition = new Vector2(-defaultDetailPanelLocalPosition.x, defaultDetailPanelLocalPosition.y);

        // ������ �̸� ������Ʈ�� ��Ŀ, �ǹ� ����
        detailPanelItemNameBackground.pivot = new Vector2(1, 0.5f);
        detailPanelItemNameBackground.anchorMin = new Vector2(1, 0.5f);
        detailPanelItemNameBackground.anchorMax = new Vector2(1, 0.5f);

        // ������ �̸� ������Ʈ�� ��ġ ����
        detailPanelItemNameBackground.anchoredPosition = new Vector2(-detailPanelItemNameBackground.anchoredPosition.x, 
            detailPanelItemNameBackground.anchoredPosition.y);
    }

    // DetailPanel�� BackArea�� �Բ� �����ؾ� ��

    private void ShowDetailPanel()
    {
        // DetailPanel ��ġ ����
        AdjustDetailPanel();

        // backArea�� ũ�⸦ Canvas�� ����
        Rect canvasRect = canvasRectTransform.rect;
        RectTransform backAreaRectTransform = backArea.GetComponent<RectTransform>();
        backAreaRectTransform.position = -canvasRect.position; // canvasRect.position�� -960, -540���� ������ ���� �� ������ �־ �ϴ� �̷��� ����
        backAreaRectTransform.sizeDelta = new Vector2(canvasRect.width, canvasRect.height);

        detailPanel.gameObject.SetActive(true);
        backArea.gameObject.SetActive(true);

        // BackArea Ŭ�� �� detailPanel�� �ݴ� ������ �߰�
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
        // �������� �⺻ ������ �����ؼ� ��� ������ �ʾƵ� ��

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
