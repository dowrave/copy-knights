using System.Collections.Generic;

[System.Serializable]
public class StageResultData
{
    [System.Serializable]
    public class StageResultInfo
    {
        public string stageId;
        public int stars;
        public bool IsPerfectClear => stars == 3;

        public StageResultInfo(string id, int stars)
        {
            stageId = id;
            this.stars = stars; 
        }
    }

    public List<StageResultInfo> clearedStages = new List<StageResultInfo>();
}

