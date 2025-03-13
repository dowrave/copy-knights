using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;


// 
public class TutorialPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI textComponent = default!;
    [SerializeField] private int maxCharactersPerPage;
    [SerializeField] private Image dialogueBox;
    [SerializeField] private Button transparentPanel;

    private RectTransform boxRect;

    private bool CanMoveToNextPage { get { return currentPageIndex < maxPageIndex && !isTyping; } }

    private string currentText = string.Empty; // 1글자씩 나타남. 현재 나타난 글자
    private string fullText = string.Empty; // 이번 dialogue에서 나타나야 할 전체 글자
    private float typingSpeed = 0.05f;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    // 텍스트 분할
    private int currentPageIndex = -1;
    private int maxPageIndex = -1;

    // 일단은 Dialogue는 Step 단위로 돌아간다
    TutorialData.TutorialStep step;

    // 마지막 페이지에 들어서는 순간 Invoke함
    public System.Action OnDialogueCompleted;

    private void Start()
    {
        textComponent.text = string.Empty;
    }

    public void SetActive(bool active)
    {
        gameObject.SetActive(active);
    }

    public void Initialize(TutorialData.TutorialStep step)
    {
        this.step = step;

        // 초기화
        textComponent.text = string.Empty;
        boxRect = dialogueBox.GetComponent<RectTransform>();

        // 배경 패널 이벤트 초기화 및 등록
        transparentPanel.onClick.RemoveAllListeners();
        transparentPanel.onClick.AddListener(OnClick);
        Debug.Log("transParentPanel에 OnClick 메서드 등록");

        SetActive(true);
        SetPosition(step.dialogueBoxPosition.x, step.dialogueBoxPosition.y);

        // 인덱스 설정
        currentPageIndex = 0;
        maxPageIndex = step.dialogues.Count - 1;

        // 기존 코루틴 중단
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // 첫 페이지 타이핑 시작
        typingCoroutine = StartCoroutine(TypeText());
    }

    private void SetPosition(float xPos, float yPos)
    {
        boxRect.anchoredPosition = new Vector2(xPos, yPos);
    }

    // 1글자씩 대화를 띄운다
    private IEnumerator TypeText()
    {
        if (currentPageIndex > maxPageIndex) yield break;

        fullText = step.dialogues[currentPageIndex];
        currentText = string.Empty;
        isTyping = true;

        // 마지막 페이지에 진입하면 곧바로 알림
        if (currentPageIndex == maxPageIndex)
        {
            OnDialogueCompleted?.Invoke();
        }

        foreach (char c in fullText)
        {
            currentText += c;
            textComponent.text = currentText;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        // 글자가 모두 나타난 후의 처리

        OnCurrentPageFinish();
    }

    // 이번 페이지의 글자가 모두 나타났을 때
    private void OnCurrentPageFinish()
    {
        // 코루틴 제거
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // 다음 페이지 인디케이터 표시
        if (CanMoveToNextPage)
        {
            ShowPageContinueHint(true);
        }
    }

    // 클릭 시 동작
    public void OnClick()
    {
        if (isTyping)
        {
            textComponent.text = fullText;
            isTyping = false;
            OnCurrentPageFinish();
            return; // 타이핑 중일 때 클릭해도 다음 대화로 넘어가지 않게끔 함
        }

        if (CanMoveToNextPage)
        {
            currentPageIndex++;
            StartCoroutine(TypeText());
        }
    }

    private void ShowPageContinueHint(bool show)
    {
        // 페이지 계속 표시를 보여줌
        // 우측 하단에 화살표 아이콘을 띄우는 식으로 구현해보자
    }
}
