using UnityEngine;

public interface IDeployable
{
    bool IsDeployed { get;  } // 배치 여부
    int DeploymentCost { get; } // 배치 코스트
    Sprite Icon { get; } // BottomPanel에 표시되는 아이콘
    Vector3 Direction { get; } // 방향
    bool IsPreviewMode { get; set; } // 미리 보기 여부
    Transform Transform { get; } 
    bool CanDeployGround { get; }
    bool CanDeployHill { get; }
    Renderer Renderer { get; }
    GameObject OriginalPrefab { get; } // 원본 프리팹

    void Initialize(GameObject prefab); // 프리팹 추가 땜에 넣음
    void Deploy(Vector3 position); //실제 배치 동작
    void Retreat(); // 퇴각 동작
    void EnablePreviewMode(); // 미리 보기 활성화
    void DisablePreviewMode(); // 미리 보기 비활성화
    void UpdatePreviewPosition(Vector3 position); // 미리보기 위치 업데이트
    void SetDirection(Vector3 direction); // 방향 설정
    void HighlightAttackRange(); // 공격 범위 설정
    void SetPreviewTransparency(float alpha); // 미리 보기 투명도
    void OnClick();



}