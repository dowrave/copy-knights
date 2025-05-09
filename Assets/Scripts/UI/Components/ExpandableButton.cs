using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// 아이콘 + 텍스트로 구성된 버튼
// 최초엔 버튼 아이콘만 나타나고
// 그 상태에서 클릭하면 숨겨진 텍스트 박스가 보이게 함
public class ExpandableButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button mainButton;
    [SerializeField] private RectTransform expandableBoxRectTransform;
    [SerializeField] private CanvasGroup expandableBoxCanvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float expandDuration = 0.3f; // 애니메이션 지속 시간
    [SerializeField] private float targetWidth = 200f; // expandableBox의 Width
    [SerializeField] Ease expandEase = Ease.OutQuad;

    [Header("Events")]
    public UnityEvent firstClickEvent; // 1번째 클릭 시의 이벤트
    public UnityEvent secondClickEvent; // 2번째 클릭 시의 이벤트

    private bool isInitializing = false; // 초기화 시에는 애니메이션 동작을 막는 플래그

    private bool _isExpanded = false;
    public bool IsExpanded
    {
        get => _isExpanded;
        private set
        {
            _isExpanded = value;
            UpdateButtonUI();
        }
    }
    private float initialBoxHeight;
    private void OnEnable()
    {
        mainButton.onClick.AddListener(OnMainButtonClicked);

        // 초기 상태 설정
        initialBoxHeight = expandableBoxRectTransform.sizeDelta.y;
        expandableBoxRectTransform.sizeDelta = new Vector2(0, initialBoxHeight);
        SetCanvasGroup(false);

        IsExpanded = false;
    }

    private void OnMainButtonClicked()
    {
        expandableBoxRectTransform.DOKill();
        expandableBoxCanvasGroup.DOKill();

        // 숨겨진 상태
        if (!IsExpanded)
        {
            IsExpanded = true;
            firstClickEvent?.Invoke(); // 다른 버튼 수축을 위한 이벤트
        }
        else
        {
            IsExpanded = false;
            secondClickEvent?.Invoke();
        }
    }

    // isExpanded의 상태 변경 후 박스를 펼치거나 접음
    private void UpdateButtonUI()
    {
        expandableBoxRectTransform.DOKill();
        expandableBoxCanvasGroup.DOKill();
        Debug.Log($"{name} 버튼 초기화 시작");
        if (isInitializing)
        {
            Debug.Log($"{name} 버튼 초기화 시작 isInitializing  : {isInitializing}");
            InitializeButtonUI(); // 애니메이션이 없는 초기화
            Debug.Log($"{name} 버튼 초기화 완료, isInitializing  : {isInitializing}");
        }
        else
        {
            if (IsExpanded)
            {
                // 펼치기
                // 너비 확장 애니메이션
                expandableBoxRectTransform.DOSizeDelta(new Vector2(targetWidth, initialBoxHeight), expandDuration)
                    .SetEase(expandEase);

                // 알파 애니메이션
                SetCanvasGroup(true);
            }
            else
            {
                // 접기
                expandableBoxRectTransform.DOSizeDelta(new Vector2(0, initialBoxHeight), expandDuration)
                    .SetEase(expandEase);

                SetCanvasGroup(false);
            }
        }
    }

    // 초기화 시에는 애니메이션이 없어도 ㅇㅋ
    private void InitializeButtonUI()
    {
        Debug.Log("버튼 초기화 로직 동작");
        if (IsExpanded)
        {
            // 펼치기 (애니메이션 없음)
            expandableBoxRectTransform.sizeDelta = new Vector2(targetWidth, initialBoxHeight);
            expandableBoxCanvasGroup.alpha = 1f;
            expandableBoxCanvasGroup.interactable = true;
            expandableBoxCanvasGroup.blocksRaycasts = true;
        }
        else
        {
            // 접기 (애니메이션 없음)
            expandableBoxRectTransform.sizeDelta = new Vector2(0, initialBoxHeight);
            expandableBoxCanvasGroup.alpha = 0f;
            expandableBoxCanvasGroup.interactable = false;
            expandableBoxCanvasGroup.blocksRaycasts = false;
        }

        isInitializing = false;
    }
    
    private void SetCanvasGroup(bool active)
    {
        float endAlpha = active ? 1f : 0f;
        expandableBoxCanvasGroup.DOFade(endAlpha, expandDuration)
            .SetEase(expandEase)
            .OnComplete(() =>
            {
                expandableBoxCanvasGroup.interactable = active; // 확장 전에는 클릭 방지
                expandableBoxCanvasGroup.blocksRaycasts = active;
            });
    }

    public void SetIsExpanded(bool state, bool isInitializing = false)
    {
        // 순서 중요
        this.isInitializing = isInitializing;
        IsExpanded = state;
        Debug.Log($"{name}의 setIsExpanded 동작, isInitializing  : {isInitializing}");
    }

    private void OnDisable()
    {
        mainButton.onClick.RemoveAllListeners();
    }
}
