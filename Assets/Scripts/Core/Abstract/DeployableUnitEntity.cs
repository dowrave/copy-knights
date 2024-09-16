#nullable enable
using UnityEngine;

public class DeployableUnitEntity: UnitEntity, IDeployable
{
    [SerializeField]
    private DeployableUnitData deployableUnitData;
    public new DeployableUnitData Data { get => deployableUnitData; private set => deployableUnitData = value; }

    public DeployableUnitStats currentStats;

    // IDeployable �������̽� ����
    public bool IsDeployed { get; protected set; }
    public int InitialDeploymentCost { get; protected set; } // ���� ��ġ �ڽ�Ʈ - DeploymentCost�� ���� �� ������ �� ����

    public Sprite? Icon => Data.Icon;

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


    public void Initialize(DeployableUnitData deployableUnitData)
    {
        //InitializeDeployableData(deployableUnitData);
        base.InitializeUnitProperties(); 
        InitializeDeployableProperties(); 
        
    }

    private void InitializeDeployableData(DeployableUnitData deployableData)
    {
        this.deployableUnitData = deployableData;
        currentStats = deployableUnitData.stats;
    }

    protected void InitializeDeployableProperties()
    {
        modelObject = transform.Find("Model").gameObject;
        modelRenderer = modelObject.GetComponent<Renderer>();

        if (ValidateModelStructure() == false)
        {
            Debug.LogError("DeployableUnitEntity�� ��Ƽ���� ������ �̻��ؿ�");
            return;
        }
        
        IsDeployed = false; // ��ġ ��Ȱ��ȭ
        IsPreviewMode = true; // �̸����� Ȱ��ȭ
        InitialDeploymentCost = currentStats.DeploymentCost; // �ʱ� ��ġ �ڽ�Ʈ ����
        SetupPreviewMaterial(); // �̸����� ��Ƽ���� �غ�
    }

    /// <summary>
    /// �����տ��� �ʵ���� ������ �ʱ�ȭ. ���� �ʱ�ȭ�� Initialize���� ������ �̷�����.
    /// </summary>
    public virtual void InitializeFromPrefab()
    {
        if (modelObject == null)
        {
            modelObject = transform.Find("Model").gameObject;
        }
        if (modelObject != null)
        {
            modelRenderer = modelObject.GetComponent<Renderer>();
        }
        // DeployableUnitData �ʱ�ȭ (���� SerializeField�� �����Ǿ� �ִٸ� �̹� �Ҵ�Ǿ� ����)
        if (deployableUnitData != null)
        {
            currentStats = deployableUnitData.stats;
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

        if (tile.data.terrain == TileData.TerrainType.Ground && Data.canDeployOnGround) return true;
        if (tile.data.terrain == TileData.TerrainType.Hill && Data.canDeployOnHill) return true;

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
        if (modelObject == null || modelRenderer == null)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// ������ȭ�� �̸����� ��Ƽ������ �غ�
    /// </summary>
    protected void SetupPreviewMaterial()
    {
        originalMaterial = modelRenderer.sharedMaterial; // ������, ���¿� ���� ������ ���� sharedMaterial�� ���

        // �̰� �� �� null�� ����
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

    protected override void InitializeUnitProperties()
    {
        MaxHealth = currentStats.Health;
        CurrentHealth = MaxHealth;
        UpdateCurrentTile();
        Prefab = Data.prefab; // �̰� Ÿ�� ������ �������̵���
    }
}
#nullable restore
