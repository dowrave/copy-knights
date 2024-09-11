using UnityEngine;

public class DeployableUnitEntity : UnitEntity, IDeployable
{
    private DeployableUnitData data;
    private DeployableUnitStats currentStats;

    // IDeployable �������̽� ����
    public bool IsDeployed { get; protected set; }
    public int DeploymentCost { get => currentStats.deploymentCost; set => currentStats.deploymentCost = value; }
    public float RedeployTime { get => currentStats.redeployTime; set => currentStats.redeployTime = value; }
    public int InitialDeploymentCost { get; protected set; } // ���� ��ġ �ڽ�Ʈ - DeploymentCost�� ���� �� ������ �� ����

    public Sprite Icon => data.icon;

    // �̸����� ����
    private bool isPreviewMode = false;
    public bool IsPreviewMode
    {
        get { return isPreviewMode; }
        set
        {
            isPreviewMode = value;
            UpdateVisuals();
        }
    }
    protected GameObject modelObject;
    protected Renderer modelRenderer;
    protected Material originalMaterial;
    protected Material previewMaterial;

    public override void Initialize(UnitData unitData)
    {
        base.Initialize(unitData); // �� Ŭ������ InitializeData�� ȣ���
        InitializeDeployableProperties();
    }

    protected override void InitializeData(UnitData unitData)
    {
        if (unitData is DeployableUnitData deployableUnitData)
        {
            data = deployableUnitData;
            currentStats = data.stats;
        }
        else
        {
            Debug.LogError("���� �����Ͱ� deployableUnitData�� �ƴ�!");
        }
    }


    /// <summary>
    /// DeployableUnitEntity ���� �ʱ�ȭ
    /// </summary>
    protected virtual void InitializeDeployableProperties()
    {
        IsDeployed = false;
        IsPreviewMode = true;
        InitialDeploymentCost = DeploymentCost;


        SetupPreviewMaterial(); // �̸����� ��Ƽ���� �غ�
        InitializeMaxHealth(); // �ִ� ü�� ����

        if (!ValidateModelStructure()) // �ڽ� ������Ʈ�� Model ������Ʈ�� �ִ��� üũ
        {
            Debug.LogError($"DeployableData�� �ƴ�, �ʱ�ȭ �ߴ�");
            return;
        }

    }

    public virtual void Deploy(Vector3 position)
    {
        if (!IsDeployed)
        {
            IsDeployed = true;
            IsPreviewMode = false;

            transform.position = position;
            UpdateCurrentTile();
            UpdateVisuals();
        }
    }

    public virtual void Retreat()
    {
        if (IsDeployed)
        {
            IsDeployed = false;
            DeployableManager.Instance.OnDeployableRemoved(this);
            Destroy(gameObject);
        }
    }

    public virtual bool CanDeployOnTile(Tile tile)
    {
        if (IsInvalidTile(tile)) return false;

        if (tile.data.terrain == TileData.TerrainType.Ground && data.canDeployOnGround) return true;
        if (tile.data.terrain == TileData.TerrainType.Hill && data.canDeployOnHill) return true;

        return false;
    }

    private bool IsInvalidTile(Tile tile)
    {
        return tile == null || tile.IsOccupied || tile.data.isStartPoint || tile.data.isEndPoint;
    }

    public virtual void EnablePreviewMode()
    {
        IsPreviewMode = true;
    }

    public virtual void DisablePreviewMode()
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

    /// <summary>
    /// �ڽ� ������Ʈ�� Model�� �ִ��� ���θ� üũ
    /// </summary>
    private bool ValidateModelStructure()
    {
        modelObject = transform.Find("Model")?.gameObject;
        if (modelObject == null)
        {
            return false;
        }

        modelRenderer = modelObject.GetComponent<Renderer>();
        return true;
    }

    /// <summary>
    /// ������ȭ�� �̸����� ��Ƽ������ �غ�
    /// </summary>
    protected void SetupPreviewMaterial()
    {
        originalMaterial = modelRenderer.material;
        previewMaterial = new Material(originalMaterial);
        previewMaterial.SetFloat("_Mode", 3); // TransParent ���� ����
        Color previewColor = previewMaterial.color;
        previewColor.a = 0.5f;
        previewMaterial.color = previewColor;
    }

    protected virtual void ShowDeploymentUI()
    {

    }

    protected virtual void HideDeploymentUI()
    {

    }

    /// <summary>
    /// ��ġ�� ���� Ŭ�� ���� ����
    /// </summary>
    public virtual void OnClick()
    {
        // ��ġ�� ���۷����� & �̸����� ���۷����͸� Ŭ���� �� �ƴ� ��
        if (IsDeployed && !IsPreviewMode && StageManager.Instance.currentState == GameState.Battle)
        {
            DeployableManager.Instance.CancelPlacement();

            // �̸����� ���¿��� �����ϸ� �ȵ�
            if (IsPreviewMode == false)
            {
                UIManager.Instance.ShowDeployableInfo(this);
            }

            ShowActionUI();
        }
    }

    protected virtual void ShowActionUI()
    {
        DeployableManager.Instance.ShowActionUI(this);
        UIManager.Instance.ShowDeployableInfo(this);
    }

    private void UpdateVisuals()
    {
        if (IsDeployed && !IsPreviewMode && StageManager.Instance.currentState == GameState.Battle)
        {
            // ������ ����� ���� �ð� ����
            modelRenderer.material = previewMaterial;
        }

        else
        {
            // ���� ��ġ ����� ���� �ð� ����
            modelRenderer.material = originalMaterial;
        }
    }

    public void SetPreviewTransparency(float alpha)
    {
        if (previewMaterial != null)
        { 
            Color color = previewMaterial.color;
            color.a = alpha;
            previewMaterial.color = color;
        }
    }
}
