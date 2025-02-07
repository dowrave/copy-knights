using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class StageLoadingScreen : MonoBehaviour
{
    [Header("Screen Fade")]
    [SerializeField] private CanvasGroup screenFader;
    [SerializeField] private float fadeInDuration = 1f;

    [Header("Stage Info")]
    [SerializeField] private CanvasGroup infoPanel;
    [SerializeField] private TextMeshProUGUI stageIdText;
    [SerializeField] private TextMeshProUGUI stageNameText;

    [Header("Background")]
    [SerializeField] private Image backgroundPanel;
    [SerializeField] private Color loadingColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color completedColor = new Color(0.2f, 0.2f, 0.3f, 1f);

    private Sequence currentSequence;
    public event System.Action OnHideComplete;

    private void Awake()
    {
        screenFader.alpha = 0;
        infoPanel.alpha = 0;
        backgroundPanel.color = loadingColor;
    }

    // 로딩 화면을 띄우고 스테이지 매니저의 로딩 완료를 기다림
    public void Initialize(string stageId, string stageName)
    {
        gameObject.SetActive(true);

        if (currentSequence != null)
        {
            currentSequence.Kill();
        }

        stageIdText.text = stageId;
        stageNameText.text = stageName;

        currentSequence = DOTween.Sequence();
        currentSequence.Append(screenFader.DOFade(1f, fadeInDuration * 0.1f));
        currentSequence.Append(infoPanel.DOFade(1f, fadeInDuration * 0.5f));
        currentSequence.Play();

        StartCoroutine(WaitForStageManagerAndSubscribe());
    }

    private IEnumerator WaitForStageManagerAndSubscribe()
    {
        yield return new WaitUntil(() => StageManager.Instance != null);
        StageManager.Instance.OnPreparationComplete += HandlePreparationComplete;
    }

    private void HandlePreparationComplete()
    {
        backgroundPanel.DOColor(completedColor, fadeInDuration * 0.01f).OnComplete(() =>
        {
            StartCoroutine(HideRoutine());
        });
    }

    private IEnumerator HideRoutine()
    {
        yield return screenFader.DOFade(0f, fadeInDuration * 2f) // 패널이 서서히 사라짐
            .OnComplete(() =>
            {
                // 패널이 완전히 사라진 후
                OnHideComplete?.Invoke();
                gameObject.SetActive(false);
            })
            .WaitForCompletion();
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance.OnPreparationComplete -= HandlePreparationComplete;
        }
        currentSequence?.Kill();
    }
}
