using UnityEngine;
using UnityEngine.UI;

public class DiamondOutlineImage : Image
{
    [SerializeField]
    private float m_LineWidth = 0.05f;

    public float lineWidth
    {
        get => m_LineWidth;
        set
        {
            m_LineWidth = value;
            SetVerticesDirty();
        }
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;

        // 외부 마름모 꼭짓점
        Vector2 topOuter = center + new Vector2(0, rect.height / 2);
        Vector2 rightOuter = center + new Vector2(rect.width / 2, 0);
        Vector2 bottomOuter = center + new Vector2(0, -rect.height / 2);
        Vector2 leftOuter = center + new Vector2(-rect.width / 2, 0);

        // 내부 마름모 꼭짓점
        Vector2 topInner = center + new Vector2(0, (rect.height / 2) - lineWidth);
        Vector2 rightInner = center + new Vector2((rect.width / 2) - lineWidth, 0);
        Vector2 bottomInner = center + new Vector2(0, (-rect.height / 2) + lineWidth);
        Vector2 leftInner = center + new Vector2((-rect.width / 2) + lineWidth, 0);

        // 외곽선 추가
        vh.AddVert(CreateUIVertex(topOuter, color));
        vh.AddVert(CreateUIVertex(rightOuter, color));
        vh.AddVert(CreateUIVertex(bottomOuter, color));
        vh.AddVert(CreateUIVertex(leftOuter, color));
        vh.AddVert(CreateUIVertex(topInner, color));
        vh.AddVert(CreateUIVertex(rightInner, color));
        vh.AddVert(CreateUIVertex(bottomInner, color));
        vh.AddVert(CreateUIVertex(leftInner, color));

        // 외곽선 삼각형 추가
        vh.AddTriangle(0, 1, 5);
        vh.AddTriangle(0, 5, 4);
        vh.AddTriangle(1, 2, 6);
        vh.AddTriangle(1, 6, 5);
        vh.AddTriangle(2, 3, 7);
        vh.AddTriangle(2, 7, 6);
        vh.AddTriangle(3, 0, 4);
        vh.AddTriangle(3, 4, 7);
    }

    private UIVertex CreateUIVertex(Vector2 position, Color32 color)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        return vertex;
    }
}