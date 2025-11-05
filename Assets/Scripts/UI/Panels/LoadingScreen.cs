using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 스테이지 로딩 사용
public class LoadingScreen : MonoBehaviour
{
    [Header("Screen Fade")]
    [SerializeField] private CanvasGroup screenFader = default!;
    [SerializeField] private float fadeOutDuration = 1f; // 화면이 어두워지는데 걸리는 시간
    [SerializeField] private float fadeInDuration = 1f; // 화면이 밝아지는 데 걸리는 시간

    [Header("Stage Info")]
    [SerializeField] private CanvasGroup stageInfoPanel = default!;
    [SerializeField] private TextMeshProUGUI stageIdText = default!;
    [SerializeField] private TextMeshProUGUI stageNameText = default!;
    [SerializeField] private Slider loadingSlider = default!;
    [SerializeField] private TextMeshProUGUI progressText = default!;

    // [Header("Background")]
    // [SerializeField] private Image backgroundPanel = default!;
    // [SerializeField] private Color loadingColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    // [SerializeField] private Color completedColor = new Color(0.2f, 0.2f, 0.3f, 1f);

    private Sequence? currentSequence;
    public event Action OnFadeInCompleted = delegate { }; // 이 스크린의 페이드인 동작이 완료되었을 때 발생
    public event Action OnHideComplete = delegate { }; // 이 스크린이 완전히 투명해졌을 때 발생

    private void Awake()
    {
        ResetState();
    }

    private void ResetState()
    {
        if (currentSequence != null)
        {
            currentSequence.Kill();
        }

        // 패널 정보 초기화
        stageIdText.text = "";
        stageNameText.text = "";
        loadingSlider.value = 0f;
        progressText.text = "";

        screenFader.alpha = 0;
        stageInfoPanel.alpha = 0f;
    }

    // 검은 화면만 사용할 경우
    public void Initialize()
    {
        ResetState();

        stageInfoPanel.gameObject.SetActive(false); // 정보 패널 부분은 비활성화

        // 어두운 패널이 서서히 나타남 - Append 개념은 순차적으로 동작함
        currentSequence = DOTween.Sequence();
        currentSequence.Append(screenFader.DOFade(1f, fadeOutDuration))
            .OnComplete(() =>
            {
                OnFadeInCompleted?.Invoke(); // 이 패널이 완전히 나타난 후 이벤트 발생
            })
            .SetUpdate(true);

        currentSequence.Play();
    }

    // 스테이지 진입 시 사용 - 스테이지 이름과 로딩 게이지를 함께 보여줌
    // 로딩 화면을 띄우고 스테이지 매니저의 로딩 완료를 기다림
    public void Initialize(string stageId, string stageName)
    {
        ResetState();

        // 스테이지 정보 및 로딩 게이지 활성화 
        stageIdText.text = stageId;
        stageNameText.text = stageName;
        loadingSlider.gameObject.SetActive(true);

        // 어두운 패널이 서서히 나타남 - Append 개념은 순차적으로 동작함
        currentSequence = DOTween.Sequence();
        currentSequence.Append(screenFader.DOFade(1f, fadeOutDuration))
            .OnComplete(() =>
            {
                OnFadeInCompleted?.Invoke(); // 이 패널이 완전히 나타난 후 이벤트 발생
            })
            .SetUpdate(true);
        currentSequence.AppendCallback(() =>
        {
            stageInfoPanel.alpha = 1f; // 페이드인 후 인포 패널 등장
        });

        // 시퀀스 실행
        StartCoroutine(WaitStageManagerAndPlaySequence());
    }
    
    // StageManager의 초기화를 기다린 후 이벤트 실행 및 시퀀
    private IEnumerator WaitStageManagerAndPlaySequence()
    {
        yield return new WaitUntil(() => StageManager.Instance != null);
        StageManager.Instance!.OnPreparationCompleted += HandleStagePreparationComplete;
        currentSequence.Play();
    }

    public void UpdateProgress(float progress)
    {
        loadingSlider.value = progress;
        progressText.text = progress >= 1f ? "스테이지 로딩 완료" : $"스테이지 로딩중 : {progress * 100:F0}%";
    }

    // 스테이지 매니저 인스턴스가 생성되면 패널을 감추기 시작
    private void HandleStagePreparationComplete()
    {
        StartCoroutine(HideRoutine());
    }

    private IEnumerator HideRoutine()
    {
        yield return screenFader.DOFade(0f, fadeOutDuration) // 패널이 서서히 사라짐
            .OnComplete(() =>
            {
                // 패널이 완전히 사라진 후
                OnHideComplete?.Invoke();
                StageManager.Instance!.OnPreparationCompleted -= HandleStagePreparationComplete;
            })
            .WaitForCompletion();
    }

    private void OnDisable()
    {
        if (StageManager.Instance != null)
        {
            StageManager.Instance!.OnPreparationCompleted -= HandleStagePreparationComplete;
        }
        currentSequence?.Kill();
    }
}
