
using UnityEngine;
using UnityEngine.UI;


public class MaskedDiamondOverlay : MonoBehaviour
{
    [SerializeField] private Image darkPanel = default!;
    [SerializeField] private DiamondMask diamondMask = default!;
    private Canvas canvas = default!;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }
        if (darkPanel == null)
        {
            darkPanel = transform.Find("DarkPanel").GetComponent<Image>();
        }
        if (diamondMask == null)
        {
            diamondMask = transform.Find("DiamondMask").GetComponent<DiamondMask>();
        }
        
    }

    public void Initialize(float darkPanelAlpha)
    {
        SetDarkPanelAlpha(darkPanelAlpha);
    }

    public void SetDarkPanelAlpha(float alpha)
    {
        Color color = darkPanel.color;
        color.a = alpha;
        darkPanel.color = color; 
    }

    public void SetMaskSize(float size)
    {
        diamondMask.rectTransform.sizeDelta = new Vector2(size, size);
    }

    public void SetLineWidth(float width)
    {
        //diamondMask.LineWidth = width; 
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
