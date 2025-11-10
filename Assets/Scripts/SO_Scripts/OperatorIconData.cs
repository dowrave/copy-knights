using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IconData", menuName = "Game/Icon Data")]
public class OperatorIconData: ScriptableObject
{
    public List<ClassIconData> classIcons = new List<ClassIconData>();
    public List<ElitePhaseIconData> elitePhaseIcons = new List<ElitePhaseIconData>();

    public Dictionary<OperatorClass, Sprite> iconDictionary = new Dictionary<OperatorClass, Sprite>();

    public void Initialize()
    {
        foreach (var iconData in classIcons)
        {
            iconDictionary[iconData.operatorClass] = iconData.icon;
        }
    }

    public Sprite? GetClassIcon(OperatorClass operatorClass)
    {
        if (iconDictionary == null)
        {
            Initialize();
        }

        if (iconDictionary!.TryGetValue(operatorClass, out Sprite icon))
        {
            return icon;
        }

        return null;
    }

    [System.Serializable]
    public class ClassIconData
    {
        public OperatorClass operatorClass;
        public Sprite icon = default!;
    }

    [System.Serializable] 
    public class ElitePhaseIconData
    {
        public OperatorElitePhase phase;
        public Sprite icon = default!; 
    }
}