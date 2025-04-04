using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 버튼을 클릭했을 때의 동작/상태 관리는 StageSelectPanel에서 진행
/// </summary>
public class StageButton : MonoBehaviour
{
    //[SerializeField] private TextMeshProUGUI stageIdText = default!; // 버튼에 표시되는 텍스트
    [SerializeField] private Image stageClearStar = default!;
    [SerializeField] private Color highlightColor;

    [SerializeField] private StageData stageData = default!;
    public StageData StageData => stageData;

    private Image image = default!;
    private Color originalColor = default!;
    private Button button = default!;
    private int star;

    // 클릭 이벤트
    // 이 스크립트 자체는 Button이 아니기 때문에 onClick 메서드를 이런 식으로 구현
    // 다른 스크립트에서 사용 시 StageButton과 Button이 헷갈리니까 이걸 만들어둔다.
    public UnityEvent<StageButton> onClick = new UnityEvent<StageButton>();

    private void Awake()
    {
        image = GetComponent<Image>();
        originalColor = image.color;

        button = GetComponent<Button>();
        button.onClick.AddListener(() => onClick.Invoke(this)); // 이벤트는 Button 컴포넌트의 OnClick으로 연결.

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
