using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IconData", menuName = "Game/Icon Data")]
public class OperatorIconData: ScriptableObject
{
    public List<ClassIconData> classIcons = new List<ClassIconData>();
    public List<ElitePhaseIconData> elitePhaseIcons = new List<ElitePhaseIconData>();

    public Dictionary<OperatorData.OperatorClass, Sprite> iconDictionary;

    public void Initialize()
    {
        iconDictionary = new Dictionary<OperatorData.OperatorClass, Sprite>();

        foreach (var iconData in classIcons)
        {
            iconDictionary[iconData.operatorClass] = iconData.icon;
        }
    }

    public Sprite GetClassIcon(OperatorData.OperatorClass operatorClass)
    {
        if (iconDictionary == null)
        {
            Initialize();
        }

        if (iconDictionary.TryGetValue(operatorClass, out Sprite icon))
        {
            return icon;
        }
        return null;
    }

    [System.Serializable]
    public class ClassIconData
    {
        public OperatorData.OperatorClass operatorClass;
        public Sprite icon;
    }

    [System.Serializable] 
    public class ElitePhaseIconData
    {
        public OperatorGrowthSystem.ElitePhase phase;
        public Sprite icon; 
    }
}