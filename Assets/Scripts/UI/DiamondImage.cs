using UnityEngine;
using UnityEngine.UI;

public class DiamondImage : Image
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;

        // 마름모 꼭짓점
        Vector2 top = center + new Vector2(0, rect.height / 2);
        Vector2 right = center + new Vector2(rect.width / 2, 0);
        Vector2 bottom = center + new Vector2(0, -rect.height / 2);
        Vector2 left = center + new Vector2(-rect.width / 2, 0);

        // 마름모 모양 채우기
        vh.AddVert(CreateUIVertex(top, color));
        vh.AddVert(CreateUIVertex(right, color));
        vh.AddVert(CreateUIVertex(bottom, color));
        vh.AddVert(CreateUIVertex(left, color));

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(0, 2, 3);
    }

    private UIVertex CreateUIVertex(Vector2 position, Color32 color)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        return vertex;
    }
}