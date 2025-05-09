using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// ������ + �ؽ�Ʈ�� ������ ��ư
// ���ʿ� ��ư �����ܸ� ��Ÿ����
// �� ���¿��� Ŭ���ϸ� ������ �ؽ�Ʈ �ڽ��� ���̰� ��
public class ExpandableButton : MonoBehaviour
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

    private bool isInitializing = false; // �ʱ�ȭ �ÿ��� �ִϸ��̼� ������ ���� �÷���

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

        // �ʱ� ���� ����
        initialBoxHeight = expandableBoxRectTransform.sizeDelta.y;
        expandableBoxRectTransform.sizeDelta = new Vector2(0, initialBoxHeight);
        SetCanvasGroup(false);

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
            IsExpanded = false;
            secondClickEvent?.Invoke();
        }
    }

    // isExpanded�� ���� ���� �� �ڽ��� ��ġ�ų� ����
    private void UpdateButtonUI()
    {
        expandableBoxRectTransform.DOKill();
        expandableBoxCanvasGroup.DOKill();
        Debug.Log($"{name} ��ư �ʱ�ȭ ����");
        if (isInitializing)
        {
            Debug.Log($"{name} ��ư �ʱ�ȭ ���� isInitializing  : {isInitializing}");
            InitializeButtonUI(); // �ִϸ��̼��� ���� �ʱ�ȭ
            Debug.Log($"{name} ��ư �ʱ�ȭ �Ϸ�, isInitializing  : {isInitializing}");
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
        Debug.Log($"{name}�� setIsExpanded ����, isInitializing  : {isInitializing}");
    }

    private void OnDisable()
    {
        mainButton.onClick.RemoveAllListeners();
    }
}
