using System.Collections;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;

public class PathIndicator : MonoBehaviour
{
    private float speed = 10f;
    private float height = 0.3f; // 월드 좌표 기준 +y 위치

    private List<Vector3> currentPath = new List<Vector3>();

    private PathData? pathData;
    private PathNode? nextNode;
    private int nextNodeIndex = 0; // 시작하자마자 1이 됨
    private Vector3 nextPosition;
    private Vector3 destinationPosition; // 목적지
    private bool isWaiting = false; // 쓰고 있음

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
        InstanceValidator.ValidateInstance(pathData);

        foreach (var node in pathData!.nodes)
        {
            currentPath.Add(MapManager.Instance!.ConvertToWorldPosition(node.gridPosition) + Vector3.up * height);
        }

        destinationPosition = currentPath[currentPath.Count - 1]; // 목적지 설정
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

    // 경고 발생 이유:
    // isWaiting 필드는 WaitAtNode() 코루틴에서 값이 할당되지만, 실제로 읽히지 않아 경고 CS0414가 발생합니다.
    // 해결 방법은 두 가지입니다.

    // 1. 대기 상태를 참조하도록 코드를 수정하여 isWaiting을 사용하게 할 수 있습니다.
    // 예를 들어, MoveAlongPath()에서 isWaiting 상태일 때 이동 로직을 건너뛰도록 처리해보세요.
    private void MoveAlongPath()
    {
        // 대기 중일 때는 이동하지 않음
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

    // 마지막 타일의 월드 좌표 기준
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

    // 대기 중일 때 실행
    private IEnumerator WaitAtNode(float waitTime)
    {
        isWaiting = true;
        yield return new WaitForSeconds(waitTime);
        isWaiting = false;

        UpdateNextNode();
    }
}
