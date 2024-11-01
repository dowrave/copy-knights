using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// ��ư�� Ŭ������ ���� ����/���� ������ StageSelectPanel���� ����
/// </summary>
public class StageButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stageIdText; // ��ư�� ǥ�õǴ� �ؽ�Ʈ

    [SerializeField] private Color highlightColor;

    [SerializeField] private StageData stageData;
    public StageData StageData => stageData;

    private Image image;
    private Color originalColor;
    private Button button;

    // Ŭ�� �̺�Ʈ
    // �� ��ũ��Ʈ ��ü�� Button�� �ƴϱ� ������ onClick �޼��带 �̷� ������ ����
    // �ٸ� ��ũ��Ʈ���� ��� �� StageButton�� Button�� �򰥸��ϱ� �̰� �����д�.
    public UnityEvent<StageButton> onClick = new UnityEvent<StageButton>();

    private void Awake()
    {
        image = GetComponent<Image>();
        originalColor = image.color;

        button = GetComponent<Button>();
        button.onClick.AddListener(() => onClick.Invoke(this)); // �̺�Ʈ�� Button ������Ʈ�� OnClick���� ����.
    }

    public void SetUp(StageData data)
    {
        stageData = data;
        stageIdText.text = stageData.stageId; 
    }

    public void SetSelected(bool isSelected)
    {
        image.color = isSelected ? highlightColor : originalColor;
    }
}
