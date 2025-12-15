using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class StageDatabase : MonoBehaviour
{
    [SerializeField] private List<StageData> stageDatas = new List<StageData>();

    public IReadOnlyCollection<StageData> StageDatas => stageDatas; 

    // stageId로 stageData를 얻음
    public StageData GetDataById(string stageId)
    {
        return stageDatas.FirstOrDefault(stageData => stageData.stageId == stageId);
    }
}
