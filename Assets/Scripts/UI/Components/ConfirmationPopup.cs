using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmationPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI textContent = default!;
    [SerializeField] private Button blurArea = default!; // �� ���� Ŭ�� �� �κ�� ���ư�
    public Button confirmButton = default!;
    public Button cancelButton = default!;

    private event Action OnConfirm;
    private event Action OnCancel;
    private CanvasGroup canvasGroup = default!;
    private float animationSpeed = 0.01f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        confirmButton.onClick.AddListener(() =>
        {
            OnConfirm?.Invoke();
            DisablePanelWithAnimation();
        });

        cancelButton.onClick.AddListener(() =>
        {
            OnCancel?.Invoke();
            DisablePanelWithAnimation();
        });
    }
    

    /// <summary>
    /// Ȯ�� �г��� �ʱ�ȭ�մϴ�.
    /// </summary>
    /// <param name="text">�޽��� ����</param>
    /// <param name="isCancelButton">��� ��ư ǥ�� ����</param>
    /// <param name="blurAreaActivation">��� Ŭ�� ���� Ȱ��ȭ ����</param>
    public void Initialize(string text, bool isCancelButton, bool blurAreaActivation, Action onConfirm = null, Action onCancel = null)
    {
        // ���� �̺�Ʈ ����
        OnConfirm = null;
        OnCancel = null;

        // �̺�Ʈ�� ���۽�ų �Լ��� ��ǲ���� ����
        if (onConfirm != null) OnConfirm += onConfirm;
        if (onCancel != null) OnCancel += onCancel;

        EnablePanelWithAnimation();

        // ��� ��ư Ȱ��ȭ ����
        cancelButton.gameObject.SetActive(isCancelButton);

        // �޹�� Ȱ��ȭ ����
        blurArea.gameObject.SetActive(blurAreaActivation);

        if (blurAreaActivation)
        {
            AddBlurAreaListener(isCancelButton);
        }

        SetTextContent(text);
    }

    private void SetTextContent(string text)
    {
        textContent.text = text;
    }

    private void AddBlurAreaListener(bool isCancelButtonActivation)
    {
        blurArea.onClick.RemoveAllListeners();

        // Cancel Button�� ������ Cancel ����, ������ Confirm ����
        if (isCancelButtonActivation)
        {
            blurArea.onClick.AddListener(() =>
            {
                OnCancel?.Invoke();
                DisablePanelWithAnimation();
            });
        }
        else
        {
            blurArea.onClick.AddListener(() =>
            {
                OnConfirm?.Invoke();
                DisablePanelWithAnimation();
            });
        }
    }

    private void EnablePanelWithAnimation()
    {
        gameObject.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, animationSpeed)
                .SetUpdate(true); // Time.timeScale ����
        }
    }

    private void DisablePanelWithAnimation()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, animationSpeed)
                .SetUpdate(true);

            gameObject.SetActive(false);
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        confirmButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();
        blurArea.onClick.RemoveAllListeners();
    }
}
