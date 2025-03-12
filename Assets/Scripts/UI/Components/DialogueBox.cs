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
        Debug.Log("dialogueBox �����");
        
        this.step = step;

        textComponent.text = string.Empty;
        rect = GetComponent<RectTransform>();

        // ��ȭ ����
        SetActive(true);
        SetPosition(step.dialogueBoxPosition.x, step.dialogueBoxPosition.y);

        currentPageIndex = 0;
        maxPageIndex = step.dialogues.Count;

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
        rect.anchoredPosition = new Vector2(xPos, yPos);
    }

    // 1���ھ� ��ȭ�� ����
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

        // ���ڰ� ��� ��Ÿ�� ���� ó��

        OnCurrentPageFinish();
    }

    // �̹� �������� ���ڰ� ��� ��Ÿ���� ��
    private void OnCurrentPageFinish()
    {
        if (CanMoveToNextPage)
        {
            ShowPageContinueHint(true);
        }
    }

    // Ŭ�� �� ����
    public void OnClick()
    {
        // Ÿ���� ���� �� : Ŭ���ϸ� ��� �ؽ�Ʈ�� ��� ǥ�õ�
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
            return; // Ÿ���� ���� �� Ŭ���ص� ���� ��ȭ�� �Ѿ�� �ʰԲ� ��
        }

        if (CanMoveToNextPage)
        {
            currentPageIndex++;
            StartCoroutine(TypeText());
        }
    }

    private void ShowPageContinueHint(bool show)
    {
        // ������ ��� ǥ�ø� ������
        // ���� �ϴܿ� ȭ��ǥ �������� ���� ������ �����غ���
    }
}
