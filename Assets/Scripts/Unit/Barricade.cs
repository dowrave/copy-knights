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
    public static event Action<Barricade> OnBarricadeRetreated;


    public void Deploy(Vector3 position)
    {
        transform.position = position;
        isDeployed = true;
        gameObject.SetActive(true);
    }

    public void Retreat()
    {
        isDeployed = false;
        gameObject.SetActive(false);
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
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            originalMaterial = renderer.material;
            previewMaterial = new Material(originalMaterial);
            previewMaterial.color = new Color(originalMaterial.color.r, originalMaterial.color.g, originalMaterial.color.b, 0.5f);
        }
    }

    private void UpdateVisuals()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = IsPreviewMode ? previewMaterial : originalMaterial;
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
}
