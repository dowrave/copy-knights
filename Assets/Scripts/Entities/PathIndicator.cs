using System.Collections;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;

public class PathIndicator : MonoBehaviour
{
    private float speed = 10f;
    private float height = 0.3f; // 월드 좌표 기준 +y 위치

    private List<Vector3> currentPath = new List<Vector3>();

    private PathData pathData;
    private PathNode nextNode;
    private int nextNodeIndex = 0; // 시작하자마자 1이 됨
    private Vector3 nextPosition;
    private bool isWaiting = false;
    private Vector3 destinationPosition; // 목적지

    public void Initialize(PathData pathData)
    {
        this.pathData = pathData;

        // Enemy에 작성된 순서를 따라감
        SetupInitialPosition();
        UpdateNextNode();
        InitializeCurrentPath();
    }

    private void InitializeCurrentPath()
    {
        foreach (var node in pathData.nodes)
        {
            currentPath.Add(MapManager.Instance.ConvertToWorldPosition(node.gridPosition) + Vector3.up * height);
        }

        destinationPosition = currentPath[currentPath.Count - 1]; // 목적지 설정
    }

    private void SetupInitialPosition()
    {
        if (pathData != null && pathData.nodes.Count > 0)
        {
            transform.position = MapManager.Instance.ConvertToWorldPosition(pathData.nodes[0].gridPosition) +
                Vector3.up * height;
        }
    }

    private void Update()
    {
        if (StageManager.Instance.currentState == GameState.Battle)
        {
            MoveAlongPath();
        }
    }

    private void MoveAlongPath()
    {
        if (CheckIfReachedDestination())
        {
            ReachDestination();
            return;
        }

        Move(nextPosition);
        //RotateModelTowardsMovementDirection();

        // 노드 도달 확인
        if (Vector3.Distance(transform.position, nextPosition) < 0.05f)
        {
            // 목적지 도달
            if (Vector3.Distance(transform.position, destinationPosition) < 0.05f)
            {
                ReachDestination();
            }
            // 기다려야 하는 경우
            else if (nextNode.waitTime > 0)
            {
                StartCoroutine(WaitAtNode(nextNode.waitTime));
            }
            // 노드 업데이트
            else
            {
                UpdateNextNode();
            }
        }
    }

    // 마지막 타일의 월드 좌표 기준
    private bool CheckIfReachedDestination()
    {
        if (pathData.nodes.Count == 0) return false;

        Vector2Int lastNodeGridPos = pathData.nodes[pathData.nodes.Count - 1].gridPosition;
        Vector3 lastNodePosition = MapManager.Instance.ConvertToWorldPosition(lastNodeGridPos) + Vector3.up * height;

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
        if (nextNodeIndex < pathData.nodes.Count)
        {
            nextNode = pathData.nodes[nextNodeIndex];
            nextPosition = MapManager.Instance.ConvertToWorldPosition(nextNode.gridPosition) +
                Vector3.up * height;
        }
    }

    // 대기 중일 때 실행
    private IEnumerator WaitAtNode(float waitTime)
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;

        UpdateNextNode();
    }
}
