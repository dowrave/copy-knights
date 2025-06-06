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
    [SerializeField] private Image dialogueBox = default!;
    [SerializeField] private Image boxRightBottomImage = default!;
    [SerializeField] private Button transparentPanel = default!;

    private RectTransform boxRect;

    private bool CanMoveToNextPage { get { return currentPageIndex < maxPageIndex && !isTyping && typingCoroutine == null;  } }
    private bool DialogueFinished { get { return currentPageIndex == maxPageIndex && !isTyping && typingCoroutine == null; } }

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
        transparentPanel.gameObject.SetActive(true);
        transparentPanel.onClick.RemoveAllListeners();
        transparentPanel.onClick.AddListener(OnClick);

        SetActive(true);
        SetPosition(step.dialogueBoxPosition.x, step.dialogueBoxPosition.y);

        // 인덱스 설정
        currentPageIndex = 0;
        maxPageIndex = step.dialogues.Count - 1;

        // 기존 코루틴 중단
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
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
        SetPageIndicator(false);

        int index = 0;
        while (index < fullText.Length)
        {
            // 만약 현재 문자가 '<'이면 태그 시작 -> '>'까지 전체를 한 번에 추가
            if (fullText[index] == '<')
            {
                int tagEndIndex = fullText.IndexOf('>', index);
                if (tagEndIndex != -1)
                {
                    string tag = fullText.Substring(index, tagEndIndex - index + 1);
                    currentText += tag;
                    textComponent.text = currentText;
                    index = tagEndIndex + 1;

                    yield return new WaitForSecondsRealtime(typingSpeed);
                    continue;
                }
            }
            
            // 그냥 글자 출력
            currentText += fullText[index];
            index++;
            textComponent.text = currentText;
            yield return new WaitForSecondsRealtime(typingSpeed);
            
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

        SetPageIndicator(true);

        // 마지막 페이지라면 끝났음을 알림
        if (DialogueFinished) OnDialogueCompleted?.Invoke();

    }

    // 클릭 시 동작
    public void OnClick()
    {
        // 글자가 나타나는 중 : 현재 페이지의 글자를 모두 보여준다
        if (isTyping)
        {
            // 실행 중인 코루틴을 먼저 종료하고, 나머지 텍스트 조작이 들어가야 함
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }
            textComponent.text = fullText;
            isTyping = false;
            OnCurrentPageFinish();
            return; // 타이핑 중일 때 클릭하면 다음 대화로 넘어가지 않음
        }

        // 다음 페이지로 넘어갈 수 있음 : 다음 페이지로 넘어간다
        if (CanMoveToNextPage)
        {
            currentPageIndex++;
            typingCoroutine = StartCoroutine(TypeText());
            return;
        }
    }
    
    public void SetTransparentPanel(bool show)
    {
        transparentPanel.gameObject.SetActive(show);
    }

    // transparentPanel.onClick에 리스너를 추가하는 메서드
    public void AddClickListener(UnityEngine.Events.UnityAction action)
    {
        transparentPanel.onClick.AddListener(action);
    }

    public void RemoveClickListener(UnityEngine.Events.UnityAction action)
    {
        transparentPanel.onClick.RemoveListener(action);
    }

    public void RemoveAllClickListeners()
    {
        transparentPanel.onClick.RemoveAllListeners();
    }

    private void SetPageIndicator(bool show)
    {
        boxRightBottomImage.gameObject.SetActive(show);
    }
}
