using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DiamondMask : Image
{
    private Vector2[] diamondPoints = new Vector2[4];
    private float diamondSize = 4f; // Width, Height가 4임
    public float screenDiamondRadius;

    // UI의 모양을 그림 - 마름모 생성
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

    // public void OnPointerDown(PointerEventData eventData)
    // {
    //     if (IsPointInsideDiamond(eventData))
    //     {
    //         Debug.Log("DiamondMask 내부 클릭 감지 - 방향 설정 시작");

    //         DeploymentInputHandler.Instance!.SetDraggingState(true);

    //         // 드래그 시작에 필요한 최소 거리 설정
    //         // float screenRadius = CalculateScreenRadius();
    //         // DeploymentInputHandler.Instance!.SetMinDirectionDistance(screenRadius);

    //         eventData.Use(); // 뒷쪽의 다른 UI로 전달되지 않도록 막음
    //     }

    //     // 마름모 외부 클릭 - 아무것도 하지 않음
    // }

    // 클릭된 점이 마름모 내부에 있는지 체크
    // public bool IsPointInsideDiamond(Vector2 screenPoint)
    public bool IsPointInsideDiamond(PointerEventData eventData)
    {
        Vector2 localPoint;

        // 스크린 좌표(eventData.position)를 이 RectTransform의 로컬 좌표로 변환
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localPoint);

        // 로컬 좌표계 - 마름모의 반지름은 너비의 절반
        float localRadius = rectTransform.rect.width / 2f;

        // 맨해튼 거리 계산 - |x| + |y| <= radius
        float manhattanDistance = Mathf.Abs(localPoint.x) + Mathf.Abs(localPoint.y);

        return manhattanDistance <= localRadius;

        // // 마름모의 중심점을 월드 좌표로 변환
        // Vector3 worldCenter = transform.position;

        // // 마름모의 한쪽 꼭지점을 월드 좌표로 변환(이 경우 우측 꼭지점)
        // Vector3 worldRight = worldCenter + new Vector3(diamondSize / 2, 0, 0);

        // // 중심점과 우측 꼭지점을 스크린 좌표로 변환
        // Vector2 screenCenter = Camera.main.WorldToScreenPoint(worldCenter);
        // Vector2 screenRight = Camera.main.WorldToScreenPoint(worldRight);

        // // 스크린 상에서의 마름모 "반지름" 계산
        // screenDiamondRadius = Vector2.Distance(screenCenter, screenRight);
        // DeploymentInputHandler.Instance!.SetMinDirectionDistance(screenDiamondRadius);

        // // 맨해튼 거리 계산
        // float dx = Mathf.Abs(screenPoint.x - screenCenter.x);
        // float dy = Mathf.Abs(screenPoint.y - screenCenter.y);
        // float manhattanDistance = dx + dy;

        // // 맨해튼 거리가 스크린 상 마름모 "반지름"보다 작거나 같으면 내부로 판단
        // return manhattanDistance <= screenDiamondRadius;
    }

    private float CalculateScreenRadius()
    {
        Vector3 worldCenter = transform.position;
        Vector3 worldRight = transform.TransformPoint(new Vector2(rectTransform.rect.width / 2, 0)); // 로컬 -> 월드 좌표

        // 월드 -> 스크린 좌표 변환
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenCenter = RectTransformUtility.WorldToScreenPoint(eventCamera, worldCenter);
        Vector2 screenRight = RectTransformUtility.WorldToScreenPoint(eventCamera, worldRight);

        // 두 스크린 좌표 사이의 거리를 반환함
        return Vector2.Distance(screenCenter, screenRight);
    }
}