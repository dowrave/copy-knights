using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(PathData))]
public class PathDataEditor : Editor
{
    private PathData? pathData;
    private bool isEditing = false;
    //private int controlID; // �̺�Ʈ ó�� �����ϴ� ��Ʈ�� ID
    private Tool lastTool; // ���� ��� ���� ���� �� ����


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
            EditorGUILayout.HelpBox("�� ���� ��带 Ŭ���ؼ� ��带 �߰��մϴ�. shift Ű�� ��带 �����մϴ�.", MessageType.Info);
        }
    }

    private void CustomSceneGUI(SceneView sceneView)
    {
        if (!isEditing) return;

        // �̺�Ʈ ó��
        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {

            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // ������
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

    // �θ� ������Ʈ���� Tile ������Ʈ�� ã��
    private Tile? FindTileComponent(GameObject hitObject)
    {
        // ���� ��Ʈ�� ������Ʈ���� Tile ������Ʈ ã��
        Tile tile = hitObject.GetComponent<Tile>();

        // ���ٸ� �θ𿡼� ã��
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
