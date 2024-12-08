using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 아이템 상세 정보를 표시하는 패널. 화면 중앙에 표시되며 다른 UI 요소들을 흐리게 만든다.
/// </summary>
public class ItemDetailPanel : MonoBehaviour
{
    [Header("Panel Components")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private Image backgroundDim; // 뒷배경 딤 처리용 이미지

    [SerializeField] private Image itemIconImage;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI itemCountText;
    [SerializeField] private RectTransform contentContainer; // 실제 컨텐츠 컨테이너

    [Header("Panel Settings")]
    [SerializeField] private float dimAlpha = 0.7f; // 딤 처리 강도

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float scaleUpDuration = 0.3f;

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
        backgroundDim.gameObject.AddComponent<Button>().onClick.AddListener(Hide);

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

    /// <summary>
    /// 패널을 보여주고 애니메이션을 재생한다
    /// </summary>
    private void Show()
    {

        gameObject.SetActive(true);

        if (backgroundUICanvasGroup != null)
        {
            backgroundUICanvasGroup.DOFade(0.3f, fadeInDuration);
            backgroundUICanvasGroup.interactable = false;
            backgroundUICanvasGroup.blocksRaycasts = false; 
        }

        PlayShowAnimation();
    }

    private void PlayShowAnimation()
    {
        panelCanvasGroup.DOKill();
        transform.DOKill();

        panelCanvasGroup.alpha = 0f;
        transform.localScale = Vector3.zero;

        Sequence showSequence = DOTween.Sequence();
        showSequence.Append(panelCanvasGroup.DOFade(1f, fadeInDuration));
        showSequence.Join(transform.DOScale(1f, scaleUpDuration).SetEase(Ease.OutBack));
    }

    /// <summary>
    /// 패널을 숨기고 원래 UI 상태로 복구한다.
    /// </summary>
    public void Hide()
    {
        Sequence hideSequence = DOTween.Sequence();

        hideSequence.Append(panelCanvasGroup.DOFade(0f, fadeInDuration));
        hideSequence.Join(transform.DOScale(0.8f, fadeInDuration));

        if (backgroundUICanvasGroup != null)
        {
            backgroundUICanvasGroup.DOFade(1f, fadeInDuration);
            backgroundUICanvasGroup.interactable = true;
            backgroundUICanvasGroup.blocksRaycasts = true;
        }

        hideSequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
            currentItem = null; // 아이템 정보 초기화
        });
    }

    private void OnDisable()
    {
        panelCanvasGroup?.DOKill();
        transform?.DOKill();
        backgroundUICanvasGroup?.DOKill();
    }

}
