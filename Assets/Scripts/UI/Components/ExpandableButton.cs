using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// ������ + �ؽ�Ʈ�� ������ ��ư
// ���ʿ� ��ư �����ܸ� ��Ÿ����
// �� ���¿��� Ŭ���ϸ� ������ �ؽ�Ʈ �ڽ��� ���̰� ��
public class ExpandableButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private Button mainButton;
    [SerializeField] private RectTransform expandableBoxRectTransform;
    [SerializeField] private CanvasGroup expandableBoxCanvasGroup;

    [Header("Animation Settings")]
    [SerializeField] private float expandDuration = 0.3f; // �ִϸ��̼� ���� �ð�
    [SerializeField] private float targetWidth = 200f; // expandableBox�� Width
    [SerializeField] Ease expandEase = Ease.OutQuad;

    [Header("Events")]
    public UnityEvent firstClickEvent; // 1��° Ŭ�� ���� �̺�Ʈ
    public UnityEvent secondClickEvent; // 2��° Ŭ�� ���� �̺�Ʈ

    // ��ư�� ���콺 Ŀ�� ���� ��ȣ�ۿ� ǥ�ø� ���� ��ҵ�
    [Header("Visual Components")]
    public List<Image> imagesToTint; // �ν����Ϳ��� ������ �̹�����
    public Color normalColor = Color.white;
    public Color highlightedColor = new Color(0.9f, 0.9f, 0.9f);
    public Color pressedColor = new Color(0.7f, 0.7f, 0.7f);
    public Color selectedColor = Color.white; // �ʿ信 ���� �߰�
    public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    // �ʱ� ���� ���� ��ųʸ�
    private Dictionary<Image, Color> _initialImageColors = new Dictionary<Image, Color>();
    private Color _currentColorModifier = Color.white; // ���� ����� ������ ������

    private bool isInitializing = false; // �ʱ�ȭ �ÿ��� �ִϸ��̼� ������ ���� �÷���
    private bool _isPressed = false; // ���콺 ���� ���� ���� �÷���

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
        // �ʱ� ���� ����
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

        // �ʱ� ���� ����
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

        // ������ ����
        if (!IsExpanded)
        {
            IsExpanded = true;
            firstClickEvent?.Invoke(); // �ٸ� ��ư ������ ���� �̺�Ʈ
        }
        else
        {
            // ������ ���¿��� ��Ŭ���ص� �ٽ� ������ �ʰ���
            // �ϴ� ���ݱ����� ���������� �ٸ� ��ư�� Ŭ���� ���� ������ ��ư�� ������ ���
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
                // �ʱ� ���� ���� ������ ���� ���� ����, ���Ĵ� ���� ���� ����
                Color finalColor = initialColor * _currentColorModifier;
                finalColor.a = _currentColorModifier.a;
                img.color = finalColor;
            }
        }
    }
    // isExpanded�� ���� ���� �� �ڽ��� ��ġ�ų� ����
    private void UpdateButtonUI()
    {
        expandableBoxRectTransform.DOKill();
        expandableBoxCanvasGroup.DOKill();
        if (isInitializing)
        {
            InitializeButtonUI(); // �ִϸ��̼��� ���� �ʱ�ȭ
        }
        else
        {
            if (IsExpanded)
            {
                // ��ġ��
                // �ʺ� Ȯ�� �ִϸ��̼�
                expandableBoxRectTransform.DOSizeDelta(new Vector2(targetWidth, initialBoxHeight), expandDuration)
                    .SetEase(expandEase);

                // ���� �ִϸ��̼�
                SetCanvasGroup(true);
            }
            else
            {
                // ����
                expandableBoxRectTransform.DOSizeDelta(new Vector2(0, initialBoxHeight), expandDuration)
                    .SetEase(expandEase);

                SetCanvasGroup(false);
            }
        }
    }

    // �ʱ�ȭ �ÿ��� �ִϸ��̼��� ��� ����
    private void InitializeButtonUI()
    {
        Debug.Log("��ư �ʱ�ȭ ���� ����");
        if (IsExpanded)
        {
            // ��ġ�� (�ִϸ��̼� ����)
            expandableBoxRectTransform.sizeDelta = new Vector2(targetWidth, initialBoxHeight);
            expandableBoxCanvasGroup.alpha = 1f;
            expandableBoxCanvasGroup.interactable = true;
            expandableBoxCanvasGroup.blocksRaycasts = true;
        }
        else
        {
            // ���� (�ִϸ��̼� ����)
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
                expandableBoxCanvasGroup.interactable = active; // Ȯ�� ������ Ŭ�� ����
                expandableBoxCanvasGroup.blocksRaycasts = active;
            });
    }

    public void SetIsExpanded(bool state, bool isInitializing = false)
    {
        // ���� �߿�
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

        // �����Ͱ� ��ư ���� �ִ��� Ȯ��
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
            if (!currentInteractable) // ��Ȱ��ȭ�� ��
            {
                _currentColorModifier = disabledColor;
            }
            else // ��Ȱ��ȭ���� Ȱ��ȭ�� �ٲ� ���
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
