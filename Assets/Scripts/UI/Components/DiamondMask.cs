using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DiamondMask : Image
{
    private Vector2[] diamondPoints = new Vector2[4];
    private float diamondSize = 4f; // Width, Height�� 4��
    public float screenDiamondRadius;

    // UI�� ����� �׸� - ������ ����
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

    // public void OnPointerDown(PointerEventData eventData)
    // {
    //     if (IsPointInsideDiamond(eventData))
    //     {
    //         Debug.Log("DiamondMask ���� Ŭ�� ���� - ���� ���� ����");

    //         DeploymentInputHandler.Instance!.SetDraggingState(true);

    //         // �巡�� ���ۿ� �ʿ��� �ּ� �Ÿ� ����
    //         // float screenRadius = CalculateScreenRadius();
    //         // DeploymentInputHandler.Instance!.SetMinDirectionDistance(screenRadius);

    //         eventData.Use(); // ������ �ٸ� UI�� ���޵��� �ʵ��� ����
    //     }

    //     // ������ �ܺ� Ŭ�� - �ƹ��͵� ���� ����
    // }

    // Ŭ���� ���� ������ ���ο� �ִ��� üũ
    // public bool IsPointInsideDiamond(Vector2 screenPoint)
    public bool IsPointInsideDiamond(PointerEventData eventData)
    {
        Vector2 localPoint;

        // ��ũ�� ��ǥ(eventData.position)�� �� RectTransform�� ���� ��ǥ�� ��ȯ
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);

        // ���� ��ǥ�� - �������� �������� �ʺ��� ����
        float localRadius = rectTransform.rect.width / 2f;

        // ����ư �Ÿ� ��� - |x| + |y| <= radius
        float manhattanDistance = Mathf.Abs(localPoint.x) + Mathf.Abs(localPoint.y);

        return manhattanDistance <= localRadius;

        // // �������� �߽����� ���� ��ǥ�� ��ȯ
        // Vector3 worldCenter = transform.position;

        // // �������� ���� �������� ���� ��ǥ�� ��ȯ(�� ��� ���� ������)
        // Vector3 worldRight = worldCenter + new Vector3(diamondSize / 2, 0, 0);

        // // �߽����� ���� �������� ��ũ�� ��ǥ�� ��ȯ
        // Vector2 screenCenter = Camera.main.WorldToScreenPoint(worldCenter);
        // Vector2 screenRight = Camera.main.WorldToScreenPoint(worldRight);

        // // ��ũ�� �󿡼��� ������ "������" ���
        // screenDiamondRadius = Vector2.Distance(screenCenter, screenRight);
        // DeploymentInputHandler.Instance!.SetMinDirectionDistance(screenDiamondRadius);

        // // ����ư �Ÿ� ���
        // float dx = Mathf.Abs(screenPoint.x - screenCenter.x);
        // float dy = Mathf.Abs(screenPoint.y - screenCenter.y);
        // float manhattanDistance = dx + dy;

        // // ����ư �Ÿ��� ��ũ�� �� ������ "������"���� �۰ų� ������ ���η� �Ǵ�
        // return manhattanDistance <= screenDiamondRadius;
    }

    private float CalculateScreenRadius()
    {
        Vector3 worldCenter = transform.position;
        Vector3 worldRight = transform.TransformPoint(new Vector2(rectTransform.rect.width / 2, 0)); // ���� -> ���� ��ǥ

        // ���� -> ��ũ�� ��ǥ ��ȯ
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCenter);
        Vector2 screenRight = RectTransformUtility.WorldToScreenPoint(eventCamera, worldRight);

        // �� ��ũ�� ��ǥ ������ �Ÿ��� ��ȯ��
        return Vector2.Distance(screenCenter, screenRight);
    }
}