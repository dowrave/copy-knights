using UnityEngine;
using UnityEngine.UI;

public static class IconHelper
{
    private static IconData iconData;
    public static event System.Action OnIconDataInitialized;

    public static void Initialize(IconData data)
    {
        iconData = data;
        if (data != null)
        {
            data.Initialize();
            OnIconDataInitialized?.Invoke();
        }
    }

    public static void SetClassIcon(Image imageComponent, OperatorData.OperatorClass operatorClass)
    {
        // 기능만 수행
        var icon = iconData?.GetClassIcon(operatorClass);
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
