using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(PathData))]
public class PathDataEditor : Editor
{
    private PathData? pathData;
    private bool isEditing = false;
    //private int controlID; // 이벤트 처리 관리하는 컨트롤 ID
    private Tool lastTool; // 편집 모드 진입 전의 툴 상태


    private void OnEnable()
    {
        pathData = (PathData)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();

        if (GUILayout.Button(isEditing ? "Stop Editing" : "Start Editing"))
        {
            isEditing = !isEditing;
            if (isEditing)
            {
                lastTool = Tools.current;
                Tools.current = Tool.None;
                SceneView.duringSceneGui += CustomSceneGUI;
            }
            else
            {
                Tools.current = lastTool;
                SceneView.duringSceneGui -= CustomSceneGUI;
            }
            SceneView.RepaintAll();
        }

        if (isEditing)
        {
            EditorGUILayout.HelpBox("씬 뷰의 노드를 클릭해서 노드를 추가합니다. shift 키로 노드를 제거합니다.", MessageType.Info);
        }
    }

    private void CustomSceneGUI(SceneView sceneView)
    {
        if (!isEditing) return;

        // 이벤트 처리
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // 디버깅용
                Debug.Log($"Hit object: {hit.collider.gameObject.name}");
                Debug.Log($"Hit point: {hit.point}");
                Debug.Log($"Hit object layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");

                Tile? clickedTile = FindTileComponent(hit.collider.gameObject);
                if (clickedTile != null)
                {
                    Debug.Log($"Found Tile: {clickedTile.name}, Grid Position: {clickedTile.GridPosition}");
                    if (e.shift)
                    {
                        RemoveNearestNode(clickedTile.GridPosition);
                    }
                    else
                    {
                        AddNode(clickedTile);
                    }
                }
                e.Use();
            }
        }

        DrawPath();
    }

    // 부모 오브젝트에서 Tile 컴포넌트를 찾음
    private Tile? FindTileComponent(GameObject hitObject)
    {
        // 먼저 히트된 오브젝트에서 Tile 컴포넌트 찾기
        Tile tile = hitObject.GetComponent<Tile>();

        // 없다면 부모에서 찾기
        if (tile == null && hitObject.transform.parent != null)
        {
            tile = hitObject.transform.parent.GetComponent<Tile>();
        }

        return tile;
    }

    private void AddNode(Tile tile)
    {
        if (pathData == null) return; // Add null check

        Undo.RecordObject(pathData, "Add Path Node");

        Debug.Log($"{tile.gameObject.name}");
        Debug.Log($"{tile.GridPosition}");

        pathData.nodes.Add(new PathNode
        {
            tileName = tile.name,
            gridPosition = tile.GridPosition,
            waitTime = 0f
        }
        );

        EditorUtility.SetDirty(pathData);
    }

    private void RemoveNearestNode(Vector2Int position)
    {
        InstanceValidator.ValidateInstance(pathData);

        if (pathData!.nodes.Count == 0) return;

        int nearestIndex = pathData.nodes
            .Select((node, index) => new { Node = node, Index = index })
            .OrderBy(x => Vector2Int.Distance(x.Node.gridPosition, position))
            .First().Index;

        Undo.RecordObject(pathData, "Remove Path Node");
        pathData.nodes.RemoveAt(nearestIndex);
        EditorUtility.SetDirty(pathData);
    }

    private void DrawPath()
    {
        InstanceValidator.ValidateInstance(pathData);

        for (int i = 0; i < pathData!.nodes.Count; i++)
        {
            PathNode node = pathData!.nodes[i];
            Vector3 nodePosition = MapManager.Instance!.ConvertToWorldPosition(node.gridPosition);
            Handles.color = Color.yellow;
            Handles.SphereHandleCap(0, nodePosition, Quaternion.identity, 0.2f, EventType.Repaint);
        
            if (i < pathData.nodes.Count - 1)
            {
                Vector3 nextPosition = MapManager.Instance.ConvertToWorldPosition(pathData!.nodes[i + 1].gridPosition);
                Handles.DrawLine(nodePosition, nextPosition);
            }

            Handles.Label(nodePosition + Vector3.up, $"$Node {i}: Wait {pathData!.nodes[i].waitTime}s");
        }
    }

    private void OnDisable()
    {
        if (isEditing)
        {
            SceneView.duringSceneGui -= CustomSceneGUI;
            Tools.current = lastTool;
        }
    }

}
