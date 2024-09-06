using UnityEngine;

public interface IDeployable
{
    bool IsDeployed { get;  } // ��ġ ����
    int DeploymentCost { get; } // ��ġ �ڽ�Ʈ
    Sprite Icon { get; } // BottomPanel�� ǥ�õǴ� ������
    Vector3 Direction { get; } // ����
    bool IsPreviewMode { get; set; } // �̸� ���� ����
    Transform Transform { get; } 
    bool CanDeployGround { get; }
    bool CanDeployHill { get; }
    Renderer Renderer { get; }
    GameObject OriginalPrefab { get; } // ���� ������

    void Initialize(GameObject prefab); // ������ �߰� ���� ����
    void Deploy(Vector3 position); //���� ��ġ ����
    void Retreat(); // �� ����
    void EnablePreviewMode(); // �̸� ���� Ȱ��ȭ
    void DisablePreviewMode(); // �̸� ���� ��Ȱ��ȭ
    void UpdatePreviewPosition(Vector3 position); // �̸����� ��ġ ������Ʈ
    void SetDirection(Vector3 direction); // ���� ����
    void HighlightAttackRange(); // ���� ���� ����
    void SetPreviewTransparency(float alpha); // �̸� ���� ����
    void OnClick();



}