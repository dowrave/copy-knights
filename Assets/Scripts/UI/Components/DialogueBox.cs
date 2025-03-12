using UnityEngine;
using TMPro;
using System.Collections;


// 
public class DialogueBox : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textComponent = default!;
    [SerializeField] private int maxCharactersPerPage;

    private RectTransform rect;

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
        Debug.Log("dialogueBox 실행됨");
        
        this.step = step;

        textComponent.text = string.Empty;
        rect = GetComponent<RectTransform>();

        // 대화 시작
        SetActive(true);
        SetPosition(step.dialogueBoxPosition.x, step.dialogueBoxPosition.y);

        currentPageIndex = 0;
        maxPageIndex = step.dialogues.Count;

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
        rect.anchoredPosition = new Vector2(xPos, yPos);
    }

    // 1글자씩 대화를 띄운다
    private IEnumerator TypeText()
    {
        if (currentPageIndex > maxPageIndex) yield break;
        if (currentPageIndex == maxPageIndex)
        {
            OnDialogueCompleted?.Invoke();
        }
        fullText = step.dialogues[currentPageIndex];
        currentText = string.Empty;
        isTyping = true;

        //string pageText = textPages[currentPageIndex];

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
        if (CanMoveToNextPage)
        {
            ShowPageContinueHint(true);
        }
    }

    // 클릭 시 동작
    public void OnClick()
    {
        // 타이핑 중일 때 : 클릭하면 모든 텍스트가 즉시 표시됨
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

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
