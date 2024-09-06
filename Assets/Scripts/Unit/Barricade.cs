using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Barricade : MonoBehaviour, IDeployable
{
    public int health = 1;
    public int deploymentCost = 5;
    public Sprite icon;

    private bool isDeployed = false;
    private bool isPreviewMode = false;
    private Material originalMaterial;
    private Material previewMaterial;

    public Transform Transform => transform;
    public bool IsDeployed => isDeployed;
    public int DeploymentCost => deploymentCost;
    public Sprite Icon => icon;
    public bool IsPreviewMode
    {
        get => isPreviewMode;
        set
        {
            isPreviewMode = value;
            UpdateVisuals();
        }
    }

    private Vector3 facingDirection = Vector3.left;
    public Vector3 Direction
    {
        get => facingDirection;
        private set
        {
            facingDirection = value.normalized;
            transform.forward = facingDirection;
        }
    }

    private Renderer _renderer;
    public Renderer Renderer
    {
        get
        {
            if (_renderer == null)
            {
                _renderer = GetComponentInChildren<Renderer>();
            }
            return _renderer;
        }
    }

    private bool canDeployGround = true;
    private bool canDeployHill = false;

    public bool CanDeployGround => canDeployGround;
    public bool CanDeployHill => canDeployHill;

    public static event Action<Barricade> OnBarricadeDeployed;
    public static event Action<Barricade> OnBarricadeRemoved;
    public GameObject OriginalPrefab { get; private set; }

    public void Initialize(GameObject prefab)
    {
        OriginalPrefab = prefab;
        // ���� �ʱ�ȭ �ڵ尡 �ִٸ� ���⿡ �߰�...
    }

    private void Awake()
    {
        PreparePreviewMaterials();
    }

    public void Deploy(Vector3 position)
    {
        transform.position = new Vector3(position.x, 0.1f, position.z);
        isDeployed = true;
        IsPreviewMode = false;
        gameObject.SetActive(true);
        OnBarricadeDeployed?.Invoke(this);
    }

    public void Retreat()
    {
        isDeployed = false;
        gameObject.SetActive(false);
        OnBarricadeRemoved?.Invoke(this);
        DeployableManager.Instance.OnDeployableRemoved(this);
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }

    public void EnablePreviewMode()
    {
        IsPreviewMode = true;
    }

    public void DisablePreviewMode()
    {
        IsPreviewMode = false;
    }

    public void UpdatePreviewPosition(Vector3 position)
    {
        if (IsPreviewMode)
        {
            transform.position = position;
        }
    }

    private void PreparePreviewMaterials()
    {

        if (Renderer != null)
        {
            originalMaterial = Renderer.material;
            previewMaterial = new Material(originalMaterial);
            previewMaterial.SetFloat("_Mode", 3); // Transparent mode
            Color previewColor = previewMaterial.color;
            previewColor.a = 0.5f;
            previewMaterial.color = previewColor;
        }
    }

    private void UpdateVisuals()
    {
        if (Renderer != null)
        {
            Renderer.material = IsPreviewMode ? previewMaterial : originalMaterial;
        }
    }

    public void SetDirection(Vector3 direction)
    {
        Direction = direction;
    }

    public void HighlightAttackRange()
    {
        return;
    }

    public void SetPreviewTransparency(float alpha)
    {
        if (Renderer != null)
        {
            Material mat = Renderer.material;
            Color color = mat.color;
            color.a = alpha;
            mat.color = color;
        }
    }

    public void OnClick()
    {
        if (IsDeployed && !IsPreviewMode && StageManager.Instance.currentState == GameState.Battle)
        {
            DeployableManager.Instance.CancelPlacement(); // �̹� ���� ���� ��ġ ������ ��ҵǾ�� ��

            // �̸����� ���¿��� �����ϸ� �ȵ�
            if (IsPreviewMode == false)
            {
                UIManager.Instance.ShowDeployableInfo(this);
            }

            HighlightAttackRange();
            ShowActionUI();
        }
    }
    public void ShowActionUI()
    {
        DeployableManager.Instance.ShowActionUI(this);
        UIManager.Instance.ShowDeployableInfo(this);
    }
}
