using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationTable", menuName = "Localization/Localization Table")]
public class LocalizationTable: ScriptableObject
{
    public List<LocalizationItem> items;
}