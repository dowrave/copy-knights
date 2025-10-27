using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// UI 요소 뒤에 다른 UI 요소가 있을 경우 해당 UI 요소를 클릭할 수 있게 하기 위한 스크립트.
[RequireComponent(typeof(Image))] // 정상적으로 동작하기 위해 Image 컴포넌트가 필요함
public class RaycastPassThroughFilter : MonoBehaviour, ICanvasRaycastFilter
{
    // 검사해야 할 World Space 캔버스의 GraphicRaycaster를 Inspector에서 할당해줍니다.
    [SerializeField]
    private List<GraphicRaycaster> passThroughTargets = new List<GraphicRaycaster>();

    private PointerEventData pointerEventData = default!;
    private List<RaycastResult> results = new List<RaycastResult>();

    private void Start()
    {
        // EventSystem은 씬에 하나만 있으므로 캐싱해둔다.
        pointerEventData = new PointerEventData(EventSystem.current);
    }
    
    /// <summary>
    /// 이 컴포넌트가 붙은 그래픽이 레이캐스트의 유효한 대상인지 결정합니다.
    /// </summary>
    /// <param name="screenPoint">현재 마우스/터치 스크린 좌표</param>
    /// <param name="eventCamera">이 캔버스를 렌더링하는 카메라</param>
    /// <returns>true를 반환하면 클릭을 받고, false를 반환하면 클릭을 통과시킵니다.</returns>
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        // 검사할 대상이 없다면 무조건 클릭을 받음
        if (passThroughTargets == null || passThroughTargets.Count == 0)
        {
            return true;
        }

        pointerEventData.position = screenPoint;

        // 모든 대상 Raycaster에 대해 검사를 수행
        foreach (var raycaster in passThroughTargets)
        {
            if (raycaster == null || !raycaster.gameObject.activeInHierarchy) continue;

            results.Clear();
            raycaster.Raycast(pointerEventData, results);

            // 뒤쪽에서 UI가 감지된다면 뒤의 UI를 클릭하도록 함
            if (results.Count > 0)
            {
                return false;
            }
        }

        return true;
    }
}