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

    private bool CanMoveToNextPage { get { return currentPageIndex < maxPageIndex && !isTyping; } }
    private bool DialogueFinished { get { return currentPageIndex == maxPageIndex && !isTyping; } }


    private string currentText = string.Empty; // 1���ھ� ��Ÿ��. ���� ��Ÿ�� ����
    private string fullText = string.Empty; // �̹� dialogue���� ��Ÿ���� �� ��ü ����
    private float typingSpeed = 0.05f;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    // �ؽ�Ʈ ����
    private int currentPageIndex = -1;
    private int maxPageIndex = -1;

    // �ϴ��� Dialogue�� Step ������ ���ư���
    TutorialData.TutorialStep step;

    // ������ �������� ���� ���� Invoke��
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

        // �ʱ�ȭ
        textComponent.text = string.Empty;
        boxRect = dialogueBox.GetComponent<RectTransform>();

        // ��� �г� �̺�Ʈ �ʱ�ȭ �� ���
        transparentPanel.onClick.RemoveAllListeners();
        transparentPanel.onClick.AddListener(OnClick);
        Debug.Log("transParentPanel�� OnClick �޼��� ���");

        SetActive(true);
        SetPosition(step.dialogueBoxPosition.x, step.dialogueBoxPosition.y);

        // �ε��� ����
        currentPageIndex = 0;
        maxPageIndex = step.dialogues.Count - 1;

        // ���� �ڷ�ƾ �ߴ�
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // ù ������ Ÿ���� ����
        typingCoroutine = StartCoroutine(TypeText());
    }

    private void SetPosition(float xPos, float yPos)
    {
        boxRect.anchoredPosition = new Vector2(xPos, yPos);
    }

    // 1���ھ� ��ȭ�� ����
    private IEnumerator TypeText()
    {
        if (currentPageIndex > maxPageIndex) yield break;

        fullText = step.dialogues[currentPageIndex];
        currentText = string.Empty;
        isTyping = true;
        SetPageIndicator(false);

        foreach (char c in fullText)
        {
            currentText += c;
            textComponent.text = currentText;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        // ���ڰ� ��� ��Ÿ�� ���� ó��

        OnCurrentPageFinish();
    }

    // �̹� �������� ���ڰ� ��� ��Ÿ���� ��
    private void OnCurrentPageFinish()
    {
        // �ڷ�ƾ ����
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        SetPageIndicator(true);

        // ������ ��������� �������� �˸�
        if (DialogueFinished) OnDialogueCompleted?.Invoke();

    }

    // Ŭ�� �� ����
    public void OnClick()
    {
        // ���ڰ� ��Ÿ���� �� : ���� �������� ���ڸ� ��� �����ش�
        if (isTyping)
        {
            textComponent.text = fullText;
            isTyping = false;
            OnCurrentPageFinish();
            return; // Ÿ���� ���� �� Ŭ���ص� ���� ��ȭ�� �Ѿ�� �ʰԲ� ��
        }

        // ���� �������� �Ѿ �� ���� : ���� �������� �Ѿ��
        if (CanMoveToNextPage)
        {
            currentPageIndex++;
            StartCoroutine(TypeText());
            return;
        }
    }

    // transparentPanel.onClick�� �����ʸ� �߰��ϴ� �޼���
    public void AddClickListener(UnityEngine.Events.UnityAction action)
    {
        transparentPanel.onClick.AddListener(action);
    }

    private void SetPageIndicator(bool show)
    {
        boxRightBottomImage.gameObject.SetActive(show);
    }
}
