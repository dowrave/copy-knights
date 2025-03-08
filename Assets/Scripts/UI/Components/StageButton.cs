using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// ��ư�� Ŭ������ ���� ����/���� ������ StageSelectPanel���� ����
/// </summary>
public class StageButton : MonoBehaviour
{
    //[SerializeField] private TextMeshProUGUI stageIdText = default!; // ��ư�� ǥ�õǴ� �ؽ�Ʈ
    [SerializeField] private Image stageClearStar = default!;
    [SerializeField] private Color highlightColor;

    [SerializeField] private StageData stageData = default!;
    public StageData StageData => stageData;

    private Image image = default!;
    private Color originalColor = default!;
    private Button button = default!;
    private int star;

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

        //stageClearStar.color = new Color(1f, 1f, 1f, 0f);
        stageClearStar.gameObject.SetActive(false);
    }

    public void SetUpStar(int stars)
    {
        this.star = stars;
        if (stars > 0f)
        {
            stageClearStar.gameObject.SetActive(true);
            stageClearStar.color = new Color(1f, 1f, 1f, 1f);

            if (stars == 3)
            {
                stageClearStar.sprite = GameManagement.Instance!.ResourceManager.StageButtonStar3;
            }
            else if (stars == 2)
            {
                stageClearStar.sprite = GameManagement.Instance!.ResourceManager.StageButtonStar2;
            }
            else
            {
                stageClearStar.sprite = GameManagement.Instance!.ResourceManager.StageButtonStar1;
            }
        }
        else
        {
            stageClearStar.gameObject.SetActive(false);
        }
    }

    public void SetSelected(bool isSelected)
    {
        image.color = isSelected ? highlightColor : originalColor;
        if (stageClearStar.enabled == false) return;
        stageClearStar.color = isSelected  ? new Color(0.3f, 0.3f, 0.3f, 1f) : new Color(1f, 1f, 1f, 1f);
    }
}
