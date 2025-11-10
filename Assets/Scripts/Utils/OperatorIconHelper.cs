using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// 클래스, 정예화 아이콘을 관리합니다.
public static class OperatorIconHelper
{
    private static OperatorIconData? iconData; // 아이콘들 정보. 수정 필요 시 ScriptableObject에서 수정하면 됨 
    private static Dictionary<OperatorElitePhase, Sprite> elitePhaseIcons = new Dictionary<OperatorElitePhase, Sprite>(); 

    public static event System.Action OnIconDataInitialized = delegate { };

    public static void Initialize(OperatorIconData data)
    {
        iconData = data;
        if (data != null)
        {
            data.Initialize();
            elitePhaseIcons = data.elitePhaseIcons.ToDictionary(
                i => i.phase, 
                i => i.icon
            );
            OnIconDataInitialized?.Invoke();
        }
    }

    public static void SetClassIcon(Image imageComponent, OperatorClass operatorClass)
    {
        // 기능만 수행
        Sprite? icon = iconData?.GetClassIcon(operatorClass);
        if (icon != null)
        {
            imageComponent.sprite = icon;
            imageComponent.gameObject.SetActive(true);
        }
        else
        {
            imageComponent.gameObject.SetActive(false);
        }
    }

    public static void SetElitePhaseIcon(Image imageComponent, OperatorElitePhase phase)
    {
        Sprite? icon = elitePhaseIcons.GetValueOrDefault(phase);

        if (icon != null)
        {
            imageComponent.sprite = icon;
            imageComponent.gameObject.SetActive(true);
        }
        else
        {
            imageComponent.gameObject.SetActive(false);
        }
    }
}
