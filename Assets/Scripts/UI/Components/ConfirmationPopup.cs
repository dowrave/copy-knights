using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmationPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI textContent = default!;
    [SerializeField] private Button blurArea = default!; // 빈 영역 클릭 시 로비로 돌아감
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
    /// 확인 패널을 초기화합니다.
    /// </summary>
    /// <param name="text">메시지 내용</param>
    /// <param name="isCancelButton">취소 버튼 표시 여부</param>
    /// <param name="blurAreaActivation">배경 클릭 영역 활성화 여부</param>
    public void Initialize(string text, bool isCancelButton, bool blurAreaActivation, Action onConfirm = null, Action onCancel = null)
    {
        // 이전 이벤트 정리
        OnConfirm = null;
        OnCancel = null;

        // 이벤트에 동작시킬 함수도 인풋으로 받음
        if (onConfirm != null) OnConfirm += onConfirm;
        if (onCancel != null) OnCancel += onCancel;

        EnablePanelWithAnimation();

        // 취소 버튼 활성화 여부
        cancelButton.gameObject.SetActive(isCancelButton);

        // 뒷배경 활성화 여부
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

        // Cancel Button이 있으면 Cancel 동작, 없으면 Confirm 동작
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
                .SetUpdate(true); // Time.timeScale 무시
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
