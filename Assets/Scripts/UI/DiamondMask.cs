using UnityEngine;
using UnityEngine.UI;

public class DiamondMask : Image
{
    private Vector2[] diamondPoints = new Vector2[4];
    private float diamondSize = 4f; // Width, Height�� 4��
    public float screenDiamondRadius;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;

        // ������ ������
        Vector2 top = center + new Vector2(0, diamondSize / 2);
        Vector2 right = center + new Vector2(diamondSize / 2, 0);
        Vector2 bottom = center + new Vector2(0, -diamondSize / 2);
        Vector2 left = center + new Vector2(-diamondSize / 2, 0);

        // ������ ��� ä���
        vh.AddVert(CreateUIVertex(top, color));
        vh.AddVert(CreateUIVertex(right, color));
        vh.AddVert(CreateUIVertex(bottom, color));
        vh.AddVert(CreateUIVertex(left, color));

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(0, 2, 3);

        // ������ ������ ���
        Vector2 rectSize = rectTransform.rect.size;
        diamondPoints[0] = new Vector2(0, rectSize.y / 2);              // Top
        diamondPoints[1] = new Vector2(rectSize.x / 2, 0);              // Right
        diamondPoints[2] = new Vector2(0, -rectSize.y / 2);             // Bottom
        diamondPoints[3] = new Vector2(-rectSize.x / 2, 0);             // Left

    }

    private UIVertex CreateUIVertex(Vector2 position, Color32 color)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        return vertex;
    }

    /// <summary>
    /// Ŭ���� ���� ������ ���ο� �ִ��� üũ
    /// �������� ���� ���� �� ��ǥ�� ���۷������� ��ġ�� ī�޶� ���� ��ǥ�� ������ ����
    /// Ŭ���� �Ÿ��� �������� ȭ��� ���� ���� �ִ����� �˻���
    /// �������̹Ƿ� L1 ���Ͻ��� �̿���
    /// </summary>
    public bool IsPointInsideDiamond(Vector2 screenPoint)
    {
        // �������� �߽����� ���� ��ǥ�� ��ȯ
        Vector3 worldCenter = transform.position;

        // �������� ���� �������� ���� ��ǥ�� ��ȯ(�� ��� ���� ������)
        Vector3 worldRight= worldCenter + new Vector3(diamondSize / 2, 0, 0);

        // �߽����� ���� �������� ��ũ�� ��ǥ�� ��ȯ
        Vector2 screenCenter = Camera.main.WorldToScreenPoint(worldCenter);
        Vector2 screenRight = Camera.main.WorldToScreenPoint(worldRight);

        // ��ũ�� �󿡼��� ������ "������" ���
        screenDiamondRadius = Vector2.Distance(screenCenter, screenRight);
        DeployableManager.Instance.SetMinDirectionDistance(screenDiamondRadius);

        //Debug.Log($"Screen Center: {screenCenter}, Click Point: {screenPoint}");
        //Debug.Log($"Screen Top Right: {screenRight}, Screen Diamond Radius: {screenDiamondRadius}");

        // ����ư �Ÿ� ���
        float dx = Mathf.Abs(screenPoint.x - screenCenter.x);
        float dy = Mathf.Abs(screenPoint.y - screenCenter.y);
        float manhattanDistance = dx + dy;

        //Debug.Log($"Manhattan Distance: {manhattanDistance}, Threshold: {screenDiamondRadius}");

        // ����ư �Ÿ��� ��ũ�� �� ������ "������"���� �۰ų� ������ ���η� �Ǵ�
        return manhattanDistance <= screenDiamondRadius;
    }
}