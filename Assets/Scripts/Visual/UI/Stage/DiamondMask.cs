using UnityEngine;
using UnityEngine.UI;

public class DiamondMask : Image
{
    private Vector2[] diamondPoints = new Vector2[4];
    private float diamondSize = 4f; // Width, Height가 4임
    public float screenDiamondRadius;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;

        // 마름모 꼭짓점
        Vector2 top = center + new Vector2(0, diamondSize / 2);
        Vector2 right = center + new Vector2(diamondSize / 2, 0);
        Vector2 bottom = center + new Vector2(0, -diamondSize / 2);
        Vector2 left = center + new Vector2(-diamondSize / 2, 0);

        // 마름모 모양 채우기
        vh.AddVert(CreateUIVertex(top, color));
        vh.AddVert(CreateUIVertex(right, color));
        vh.AddVert(CreateUIVertex(bottom, color));
        vh.AddVert(CreateUIVertex(left, color));

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(0, 2, 3);

        // 마름모 꼭짓점 계산
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
    /// 클릭된 점이 마름모 내부에 있는지 체크
    /// 마름모의 게임 월드 상 좌표와 오퍼레이터의 위치를 카메라 상의 좌표로 투영한 다음
    /// 클릭된 거리가 마름모의 화면상 길이 내에 있는지를 검사함
    /// 마름모이므로 L1 디스턴스를 이용함
    /// </summary>
    public bool IsPointInsideDiamond(Vector2 screenPoint)
    {
        // 마름모의 중심점을 월드 좌표로 변환
        Vector3 worldCenter = transform.position;

        // 마름모의 한쪽 꼭지점을 월드 좌표로 변환(이 경우 우측 꼭지점)
        Vector3 worldRight= worldCenter + new Vector3(diamondSize / 2, 0, 0);

        // 중심점과 우측 꼭지점을 스크린 좌표로 변환
        Vector2 screenCenter = Camera.main.WorldToScreenPoint(worldCenter);
        Vector2 screenRight = Camera.main.WorldToScreenPoint(worldRight);

        // 스크린 상에서의 마름모 "반지름" 계산
        screenDiamondRadius = Vector2.Distance(screenCenter, screenRight);
        DeployableManager.Instance.SetMinDirectionDistance(screenDiamondRadius);

        //Debug.Log($"Screen Center: {screenCenter}, Click Point: {screenPoint}");
        //Debug.Log($"Screen Top Right: {screenRight}, Screen Diamond Radius: {screenDiamondRadius}");

        // 맨해튼 거리 계산
        float dx = Mathf.Abs(screenPoint.x - screenCenter.x);
        float dy = Mathf.Abs(screenPoint.y - screenCenter.y);
        float manhattanDistance = dx + dy;

        //Debug.Log($"Manhattan Distance: {manhattanDistance}, Threshold: {screenDiamondRadius}");

        // 맨해튼 거리가 스크린 상 마름모 "반지름"보다 작거나 같으면 내부로 판단
        return manhattanDistance <= screenDiamondRadius;
    }
}