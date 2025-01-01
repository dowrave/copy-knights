using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 아이템 상세 정보를 표시하는 패널. 화면 중앙에 표시되며 다른 UI 요소들을 흐리게 만든다.
/// </summary>
public class ItemInfoPopup : MonoBehaviour
{
    [Header("Panel Components")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Image backgroundDim; // 뒷배경 딤 처리용 이미지

    [SerializeField] private Image itemIconImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI itemCountText;
    //[SerializeField] private RectTransform contentContainer; // 실제 컨텐츠 컨테이너

    [Header("Panel Settings")]
    [SerializeField] private float dimAlpha = 0.8f; // 딤 처리 강도

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.1f;
    [SerializeField] private float scaleUpDuration = 0.1f;

    private CanvasGroup backgroundUICanvasGroup; // 흐리게 할 요소들의 CanvasGroup
    private ItemData currentItem;

    private void Awake()
    {
        SetUpPanel();
    }

    private void SetUpPanel()
    {
        // 배경 딤 영역 설정
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

        // 보유 수량
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
    /// 패널을 숨기고 원래 UI 상태로 복구한다.
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
    /// 캔버스를 1번 새로고침해서 ContentSizeFitter로 바뀌는 내용이 반영되도록 함
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
