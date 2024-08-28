using UnityEngine;
using UnityEngine.UI;

public class DiamondImage : Image
{

    public float _lineWidth = 0.1f;

    public float LineWidth
    {
        get { return _lineWidth; }
        set
        {
            if (_lineWidth != value)
            {
                _lineWidth = value;
                SetAllDirty();
            }
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Vector2 center = rectTransform.rect.center;
        float width = rectTransform.rect.width;
        float height = rectTransform.rect.height;

        // �ܺ� ������ ������
        Vector2 topOuter = center + new Vector2(0, height / 2);
        Vector2 rightOuter = center + new Vector2(width / 2, 0);
        Vector2 bottomOuter = center + new Vector2(0, -height / 2);
        Vector2 leftOuter = center + new Vector2(-width / 2, 0);

        // ���� ������ ������
        Vector2 topInner = center + new Vector2(0, height / 2 - LineWidth);
        Vector2 rightInner = center + new Vector2(width / 2 - LineWidth, 0);
        Vector2 bottomInner = center + new Vector2(0, -height / 2 + LineWidth);
        Vector2 leftInner = center + new Vector2(-width / 2 + LineWidth, 0);

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        // �ܺ� ������ �߰�
        vertex.position = topOuter; vh.AddVert(vertex);    // 0
        vertex.position = rightOuter; vh.AddVert(vertex);  // 1
        vertex.position = bottomOuter; vh.AddVert(vertex); // 2
        vertex.position = leftOuter; vh.AddVert(vertex);   // 3

        // ���� ������ �߰�
        vertex.position = topInner; vh.AddVert(vertex);    // 4
        vertex.position = rightInner; vh.AddVert(vertex);  // 5
        vertex.position = bottomInner; vh.AddVert(vertex); // 6
        vertex.position = leftInner; vh.AddVert(vertex);   // 7

        // ��� �ﰢ����
        vh.AddTriangle(0, 1, 5);
        vh.AddTriangle(0, 5, 4);

        // ���� �ﰢ����
        vh.AddTriangle(1, 2, 6);
        vh.AddTriangle(1, 6, 5);

        // �ϴ� �ﰢ����
        vh.AddTriangle(2, 3, 7);
        vh.AddTriangle(2, 7, 6);

        // ���� �ﰢ����
        vh.AddTriangle(3, 0, 4);
        vh.AddTriangle(3, 4, 7);
    }

    // Inspector���� ���� ����� �� ȣ��˴ϴ�.
    protected override void OnValidate()
    {
        base.OnValidate();
        SetAllDirty();
    }

    // �޽ø� �ٽ� �׸����� Unity�� �˸��ϴ�.
    public void SetAllDirty()
    {
        SetVerticesDirty();
        SetMaterialDirty();
    }

#if UNITY_EDITOR
    // �����Ϳ��� �ǽð����� ��������� �ݿ��մϴ�.
    protected override void Reset()
    {
        base.Reset();
        SetAllDirty();
    }
#endif
}