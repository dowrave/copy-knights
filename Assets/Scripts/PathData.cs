using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathNode
{
    public Vector3 position;
    public float waitTime;
}

[CreateAssetMenu(fileName = "New Path Data", menuName = "Game/PathData")]
public class PathData: ScriptableObject
{
    public List<PathNode> nodes = new List<PathNode>();
    public List<PathData> alternativePaths = new List<PathData>();
}
