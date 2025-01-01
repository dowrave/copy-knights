using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// ������ �� ������ ǥ���ϴ� �г�. ȭ�� �߾ӿ� ǥ�õǸ� �ٸ� UI ��ҵ��� �帮�� �����.
/// </summary>
public class ItemInfoPopup : MonoBehaviour
{
    [Header("Panel Components")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Image backgroundDim; // �޹�� �� ó���� �̹���

    [SerializeField] private Image itemIconImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI itemCountText;
    //[SerializeField] private RectTransform contentContainer; // ���� ������ �����̳�

    [Header("Panel Settings")]
    [SerializeField] private float dimAlpha = 0.8f; // �� ó�� ����

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.1f;
    [SerializeField] private float scaleUpDuration = 0.1f;

    private CanvasGroup backgroundUICanvasGroup; // �帮�� �� ��ҵ��� CanvasGroup
    private ItemData currentItem;

    private void Awake()
    {
        SetUpPanel();
    }

    private void SetUpPanel()
    {
        // ��� �� ���� ����
        backgroundDim.color = new Color(0f, 0f, 0f, dimAlpha);
        backgroundDim.gameObject.GetComponent<Button>().onClick.AddListener(Hide);

        backgroundUICanvasGroup = transform.root.GetComponent<CanvasGroup>();

        gameObject.SetActive(false);
    }

    public void Initialize(ItemData itemData)
    {
        currentItem = itemData;
        UpdatePanelContent();
        Show();
    }

    private void UpdatePanelContent()
    {
        if (currentItem == null) return;

        itemNameText.text = currentItem.itemName;
        itemDescriptionText.text = currentItem.description;
        itemIconImage.sprite = currentItem.icon;
        itemIconImage.enabled = currentItem.icon != null;

        // ���� ����
        int count = GameManagement.Instance.PlayerDataManager.GetItemCount(currentItem.itemName);
        itemCountText.text = count.ToString();

    }

    private void Show()
    {
        gameObject.SetActive(true);
        ShowWithLayout();
    }

    private void PlayShowAnimation()
    {
        panelCanvasGroup.DOKill();
        transform.DOKill();

        panelCanvasGroup.alpha = 0f;

        Sequence showSequence = DOTween.Sequence();
        showSequence.Append(panelCanvasGroup.DOFade(1f, fadeInDuration));
    }

    /// <summary>
    /// �г��� ����� ���� UI ���·� �����Ѵ�.
    /// </summary>
    public void Hide()
    {
        Sequence hideSequence = DOTween.Sequence();

        hideSequence.Append(panelCanvasGroup.DOFade(0f, fadeInDuration));
        //hideSequence.Join(transform.DOScale(0.8f, fadeInDuration));
        hideSequence.Join(backgroundDim.DOFade(1f, fadeInDuration));

        hideSequence.OnComplete(() =>
        {
            PopupManager.Instance.OnPopupClosed(this);

            ClearData();
        });
    }

    public void ClearData()
    {
        currentItem = null;
    }

    /// <summary>
    /// ĵ������ 1�� ���ΰ�ħ�ؼ� ContentSizeFitter�� �ٲ�� ������ �ݿ��ǵ��� ��
    /// </summary>
    private void ShowWithLayout()
    {
        panelCanvasGroup.alpha = 0f;
        backgroundDim.DOFade(dimAlpha, fadeInDuration);
        PlayShowAnimation();
    }

    private void OnDisable()
    {
        panelCanvasGroup?.DOKill();
        transform?.DOKill();
        backgroundUICanvasGroup?.DOKill();
    }

}
