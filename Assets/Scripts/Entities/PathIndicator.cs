using System.Collections;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;

public class PathIndicator : MonoBehaviour
{
    private float speed = 10f;
    private float height = 0.3f; // ���� ��ǥ ���� +y ��ġ

    private List<Vector3> currentPath = new List<Vector3>();

    private PathData? pathData;
    private PathNode? nextNode;
    private int nextNodeIndex = 0; // �������ڸ��� 1�� ��
    private Vector3 nextPosition;
    private Vector3 destinationPosition; // ������
    private bool isWaiting = false; // ���� ����

    public void Initialize(PathData pathData)
    {
        this.pathData = pathData;

        // Enemy�� �ۼ��� ������ ����
        SetupInitialPosition();
        UpdateNextNode();
        InitializeCurrentPath();
    }

    private void InitializeCurrentPath()
    {
        InstanceValidator.ValidateInstance(pathData);

        foreach (var node in pathData!.nodes)
        {
            currentPath.Add(MapManager.Instance!.ConvertToWorldPosition(node.gridPosition) + Vector3.up * height);
        }

        destinationPosition = currentPath[currentPath.Count - 1]; // ������ ����
    }

    private void SetupInitialPosition()
    {
        InstanceValidator.ValidateInstance(pathData);

        if (pathData!.nodes.Count > 0)
        {
            transform.position = MapManager.Instance!.ConvertToWorldPosition(pathData!.nodes[0].gridPosition) +
                Vector3.up * height;
        }
    }

    private void Update()
    {
        if (StageManager.Instance!.currentState == GameState.Battle)
        {
            MoveAlongPath();
        }
    }

    // ��� �߻� ����:
    // isWaiting �ʵ�� WaitAtNode() �ڷ�ƾ���� ���� �Ҵ������, ������ ������ �ʾ� ��� CS0414�� �߻��մϴ�.
    // �ذ� ����� �� �����Դϴ�.

    // 1. ��� ���¸� �����ϵ��� �ڵ带 �����Ͽ� isWaiting�� ����ϰ� �� �� �ֽ��ϴ�.
    // ���� ���, MoveAlongPath()���� isWaiting ������ �� �̵� ������ �ǳʶٵ��� ó���غ�����.
    private void MoveAlongPath()
    {
        // ��� ���� ���� �̵����� ����
        if (isWaiting)
        {
            return;
        }

        if (CheckIfReachedDestination())
        {
            ReachDestination();
            return;
        }

        Move(nextPosition);

        if (Vector3.Distance(transform.position, nextPosition) < 0.05f)
        {
            if (Vector3.Distance(transform.position, destinationPosition) < 0.05f)
            {
                ReachDestination();
            }
            else if (nextNode != null && nextNode.waitTime > 0)
            {
                StartCoroutine(WaitAtNode(nextNode.waitTime));
            }
            else
            {
                UpdateNextNode();
            }
        }
    }

    // ������ Ÿ���� ���� ��ǥ ����
    private bool CheckIfReachedDestination()
    {
        InstanceValidator.ValidateInstance(pathData);

        if (pathData!.nodes.Count == 0) return false;

        Vector2Int lastNodeGridPos = pathData!.nodes[pathData!.nodes.Count - 1].gridPosition;
        Vector3 lastNodePosition = MapManager.Instance!.ConvertToWorldPosition(lastNodeGridPos) + Vector3.up * height;

        return Vector3.Distance(transform.position, lastNodePosition) < 0.05f;
    }

    private void ReachDestination()
    {
        Destroy(gameObject, 0.5f);
    }

    public void Move(Vector3 destination)
    {
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
    }

    private void UpdateNextNode()
    {
        nextNodeIndex++;
        if (pathData != null && nextNodeIndex < pathData.nodes.Count)
        {
            nextNode = pathData.nodes[nextNodeIndex];
            nextPosition = MapManager.Instance!.ConvertToWorldPosition(nextNode.gridPosition) +
                Vector3.up * height;
        }
    }

    // ��� ���� �� ����
    private IEnumerator WaitAtNode(float waitTime)
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;

        UpdateNextNode();
    }
}
