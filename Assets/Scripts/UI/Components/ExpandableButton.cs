using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// 아이콘 + 텍스트로 구성된 버튼
// 최초엔 버튼 아이콘만 나타나고
// 그 상태에서 클릭하면 숨겨진 텍스트 박스가 보이게 함
public class ExpandableButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
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

    // 버튼과 마우스 커서 간의 상호작용 표시를 위한 요소들
    [Header("Visual Components")]
    public List<Image> imagesToTint; // 인스펙터에서 연결할 이미지들
    public Color normalColor = Color.white;
    public Color highlightedColor = new Color(0.9f, 0.9f, 0.9f);
    public Color pressedColor = new Color(0.7f, 0.7f, 0.7f);
    public Color selectedColor = Color.white; // 필요에 따라 추가
    public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    // 초기 색상 저장 딕셔너리
    private Dictionary<Image, Color> _initialImageColors = new Dictionary<Image, Color>();
    private Color _currentColorModifier = Color.white; // 현재 적용된 ㅅㄱ상 조정값

    private bool isInitializing = false; // 초기화 시에는 애니메이션 동작을 막는 플래그
    private bool _isPressed = false; // 마우스 눌림 상태 추적 플래그

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


    private void Awake()
    {
        // 초기 색상 수집
        if (imagesToTint != null)
        {
            foreach (Image img in imagesToTint)
            {
                if (img != null && !_initialImageColors.ContainsKey(img))
                {
                    _initialImageColors.Add(img, img.color);
                }
            }
        } 
    }

    private void OnEnable()
    {
        mainButton.onClick.AddListener(OnMainButtonClicked);

        // 초기 상태 설정
        initialBoxHeight = expandableBoxRectTransform.sizeDelta.y;
        expandableBoxRectTransform.sizeDelta = new Vector2(0, initialBoxHeight);
        SetCanvasGroup(false);

        _isPressed = false;
        _currentColorModifier = GetInitialColorModifier();
        ApplyColorModifier();


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
            // 펼쳐진 상태에서 재클릭해도 다시 접지는 않겠음
            // 일단 지금까지의 구현에서는 다른 버튼을 클릭할 때만 펼쳐진 버튼이 접히는 방식
            secondClickEvent?.Invoke();
        }
    }

    private Color GetInitialColorModifier()
    {
        if (mainButton == null || !mainButton.interactable)
        {
            return disabledColor;
        }
        return normalColor; 
    }

    private void ApplyColorModifier()
    {
        if (imagesToTint == null) return;
        foreach (Image img in imagesToTint)
        {
            if (img != null && _initialImageColors.TryGetValue(img, out Color initialColor))
            {
                // 초기 색에 현재 상태의 조정 색을 곱함, 알파는 조정 색을 따라감
                Color finalColor = initialColor * _currentColorModifier;
                finalColor.a = _currentColorModifier.a;
                img.color = finalColor;
            }
        }
    }
    // isExpanded의 상태 변경 후 박스를 펼치거나 접음
    private void UpdateButtonUI()
    {
        expandableBoxRectTransform.DOKill();
        expandableBoxCanvasGroup.DOKill();
        if (isInitializing)
        {
            InitializeButtonUI(); // 애니메이션이 없는 초기화
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
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (mainButton == null || !mainButton.interactable || _isPressed) return;
        _currentColorModifier = highlightedColor;
        ApplyColorModifier();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (mainButton == null || !mainButton.interactable) return;
        if (!_isPressed)
        {
            _currentColorModifier = normalColor;
            ApplyColorModifier();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (mainButton == null || !mainButton.interactable) return;
        _isPressed = true;
        _currentColorModifier = pressedColor;
        ApplyColorModifier();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (mainButton == null | !mainButton.interactable) return;
        _isPressed = false;

        // 포인터가 버튼 위에 있는지 확인
        if (RectTransformUtility.RectangleContainsScreenPoint(transform as RectTransform, eventData.position, eventData.pressEventCamera))
        {
            _currentColorModifier = highlightedColor;
        }
        else
        {
            _currentColorModifier = normalColor;
        }
        ApplyColorModifier();
    }

    private bool _wasInteractable; 
    void Update()
    {
        if (mainButton == null) return;

        bool currentInteractable = mainButton.interactable;
        if (currentInteractable != _wasInteractable)
        {
            if (!currentInteractable) // 비활성화일 때
            {
                _currentColorModifier = disabledColor;
            }
            else // 비활성화에서 활성화로 바뀐 경우
            {
                _currentColorModifier = normalColor; 
            }
            ApplyColorModifier();
        }

        _wasInteractable = currentInteractable;
    }
    void OnDisable()
    {
        _currentColorModifier = normalColor;
        ApplyColorModifier();

        if (mainButton != null)
        {
            mainButton.onClick.RemoveAllListeners();
        }
    }
}
