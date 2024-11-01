using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 버튼을 클릭했을 때의 동작/상태 관리는 StageSelectPanel에서 진행
/// </summary>
public class StageButton : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stageIdText; // 버튼에 표시되는 텍스트

    [SerializeField] private Color highlightColor;

    [SerializeField] private StageData stageData;
    public StageData StageData => stageData;

    private Image image;
    private Color originalColor;
    private Button button;

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
