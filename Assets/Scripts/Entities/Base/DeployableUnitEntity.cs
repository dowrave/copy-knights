#nullable enable
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

public class DeployableUnitEntity: UnitEntity, IDeployable
{
    public new DeployableUnitData BaseData { get; private set; }

    [HideInInspector]
    public DeployableUnitStats currentStats;

    // IDeployable �������̽� ����
    public bool IsDeployed { get; protected set; }
    public int InitialDeploymentCost { get; protected set; } // ���� ��ġ �ڽ�Ʈ - DeploymentCost�� ���� �� ������ �� ����

    private DeployableManager.DeployableInfo? deployableInfo;

    public Sprite? Icon => BaseData.Icon;

    // �̸����� ����
    protected bool isPreviewMode = false;
    public bool IsPreviewMode
    {
        get { return isPreviewMode; }
        protected set
        {
            isPreviewMode = value;
            UpdateVisuals();
        }
    }
    protected GameObject modelObject;
    protected Renderer modelRenderer;
    protected Material originalMaterial;
    protected Material previewMaterial;

    public virtual bool CanDeployGround { get; set; }
    public virtual bool CanDeployHill { get; set; }

    // ��ġ �Ϸ� �� Ŀ���� �� ��ġ�� ���۷����� ��ġ�� �� ActionUI�� ��Ÿ���� �����ϱ� ���� ������
    private float preventInteractingTime = 0.1f; 
    private float lastDeployTime;

    protected virtual void Awake()
    {
        InitializeVisual();
        Faction = Faction.Ally; // ��ġ ������ ��Ҵ� ��� �Ʊ����� ����
    }

    public void Initialize(DeployableUnitData deployableUnitData)
    {
        InitializeDeployableData(deployableUnitData);
        InitializeUnitProperties();
        InitializeDeployableProperties(); 
    }

    private void InitializeDeployableData(DeployableUnitData deployableData)
    {
        BaseData = deployableData;
        currentStats = BaseData.stats;
        deployableInfo = DeployableManager.Instance.GetDeployableInfoByName(BaseData.entityName);
    }

    /// <summary>
    /// �ڽ� ������Ʈ�� �ð�ȭ�� ����ϴ� Model�� �ִٴ� ����
    /// </summary>
    protected virtual void InitializeDeployableProperties()
    {
        IsDeployed = false; // ��ġ ��Ȱ��ȭ
        IsPreviewMode = true; // �̸����� Ȱ��ȭ

        // A ?? B : A�� null�� ��� B�� ���
        CanDeployGround = BaseData?.canDeployOnGround ?? false; 
        CanDeployHill = BaseData?.canDeployOnHill ?? false;

        InitialDeploymentCost = currentStats.DeploymentCost; // �ʱ� ��ġ �ڽ�Ʈ ����
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
        if (BaseData != null)
        {
            currentStats = BaseData.stats;
        }
    }

    public virtual void Deploy(Vector3 position)
    {
        if (!IsDeployed)
        {
            IsDeployed = true;
            IsPreviewMode = false;
            base.UpdateCurrentTile();
            CurrentTile.SetOccupied(this);
            transform.position = SetPosition(position);
            InitializeHP();
            lastDeployTime = Time.time;
        }
    }

    /// <summary>
    /// Ÿ�� �������� ���� ��ġ ��ġ ����
    /// </summary>
    protected Vector3 SetPosition(Vector3 worldPosition)
    {
        if (this is Barricade)
        {
            return worldPosition + Vector3.up * 0.1f;
        }
        else
        {
            return worldPosition + Vector3.up * 0.5f;
        }
    }

    public virtual void Retreat()
    {
        Die();
    }

    protected override void Die()
    {
        if (IsDeployed)
        {
            IsDeployed = false;
            DeployableManager.Instance.OnDeployableRemoved(this);
            if (CurrentTile != null)
            {
                CurrentTile.ClearOccupied(); // Ÿ�Ͽ� ��ġ�� ��� ����
            }

            base.Die();
        }
    }

    public virtual bool CanDeployOnTile(Tile tile)
    {
        if (IsInvalidTile(tile)) return false;

        if (tile.data.terrain == TileData.TerrainType.Ground && BaseData.canDeployOnGround) return true;
        if (tile.data.terrain == TileData.TerrainType.Hill && BaseData.canDeployOnHill) return true;

        return false;
    }

    private bool IsInvalidTile(Tile tile)
    {
        return tile == null || tile.IsOccupied || tile.data.isStartPoint || tile.data.isEndPoint;
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
        originalMaterial = new Material(modelRenderer.sharedMaterial); // �����ؼ� ���� (������ ���� ���� ���ɼ� ������)
        modelRenderer.material = originalMaterial;

        // ������ ��Ƽ���� ����
        previewMaterial = new Material(originalMaterial);
        previewMaterial.SetFloat("_Mode", 3); // TransParent ���� ����
        previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); // �ҽ� ���� ��� ����. ���İ��� ����� ����
        previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); // ��� ���� ��� ����. (1 - �ҽ� ���İ�)���� ����
        previewMaterial.SetInt("_ZWrite", 0); // Z���� ��Ȱ��ȭ. ���� ��ü�� ������� �ʴ´ٰ� ��
        previewMaterial.DisableKeyword("_ALPHATEST_ON"); // ���� �׽�Ʈ ��� ��Ȱ��ȭ
        previewMaterial.EnableKeyword("_ALPHABLEND_ON"); // ���� ���� ��� Ȱ��ȭ
        previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON"); //���� ������Ƽ�ö��� ��� ��Ȱ��ȭ
        previewMaterial.renderQueue = 3000; // ���� ť ����. ������ ��ü�� �ڿ� �׷����� ��

        Color previewColor = previewMaterial.color;
        previewColor.a = 0.8f;
        previewMaterial.color = previewColor;
    }

    /// <summary>
    /// ��ġ�� ���� Ŭ�� ���� ����
    /// </summary>
    public virtual void OnClick()
    {
        // ��ġ ���� Ŭ�� ����
        if (Time.time - lastDeployTime < preventInteractingTime)
        {
            DeployableManager.Instance.CancelPlacement();
            return;
        }

        // ��ġ�� ���۷����� Ŭ�� ����
        if (IsDeployed && !IsPreviewMode && StageManager.Instance.currentState == GameState.Battle)
        {
            DeployableManager.Instance.CancelPlacement();

            // �̸����� ���¿��� ���� X
            if (IsPreviewMode == false)
            {
                UIManager.Instance.ShowDeployedInfo(this);
            }

            ShowActionUI();
        }
    }

    protected virtual void ShowActionUI()
    {
        DeployableManager.Instance.ShowActionUI(this);
        UIManager.Instance.ShowDeployedInfo(this);
    }

    private void UpdateVisuals()
    {
        if (IsPreviewMode)
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

    /// <summary>
    /// BaseData, Stat�� ��ƼƼ���� �ٸ��� ������ �ڽ� �޼��忡�� �����ǰ� �׻� �ʿ�
    /// </summary>
    protected override void InitializeUnitProperties()
    {
        // ���� ü��, �ִ� ü�� ���� - Deploy �޼��� ����

        // ���� ��ġ�� ������� �� Ÿ�� ����
        base.UpdateCurrentTile();
        Prefab = BaseData.prefab;
    }

    /// <summary>
    /// �ð�ȭ ��ҵ��� �ʱ�ȭ
    /// </summary>
    protected virtual void InitializeVisual()
    {
        modelObject = transform.Find("Model").gameObject;
        modelRenderer = modelObject.GetComponent<Renderer>();
        SetupPreviewMaterial();
    }

    protected override void InitializeHP()
    {
        MaxHealth = currentStats.Health;
        CurrentHealth = MaxHealth;
    }
}

#nullable restore
