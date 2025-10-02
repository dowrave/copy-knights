using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// [ExecuteAlways] // ������, ��Ÿ�� ��ο��� ��ũ��Ʈ ����
public class Tile : MonoBehaviour
{
    [Header("Highlight References")]
    [SerializeField] private MeshRenderer attackRangeIndicator = default!;

    public TileData data = default!;
    public DeployableUnitEntity? OccupyingDeployable { get; private set; }

    // �� Ÿ���� ���� ������ ��� ���۷����� ���
    private readonly List<Operator> listeningOperators = new List<Operator>();

    public bool IsWalkable { get; private set; }
    private float tileScale = 0.98f;
    public Vector2 size2D;

    // Ÿ�� ���� ������ �����ϴ� �ؽ���.
    private HashSet<Enemy> enemiesOnTile = new HashSet<Enemy>();
    public IReadOnlyCollection<Enemy> EnemiesOnTile => enemiesOnTile;

    /* 
     * �߿�! ������Ƽ�� �����ϸ� ���� ������ �Ұ����ϴ�
     * �ʵ�� ������Ƽ�� �Բ� �����ϰ�, �ʵ带 �����ؾ� ������ ���ο� �׸��� ��ǥ�� ������ �� �ְ� �ȴ�.
     * ��, �Ʒ�ó�� �����ϴ� �� �� Ÿ���� ������ gridPosition ������ ���� �ϱ� �����̴�.
     * public Vector2Int GridPosition {get; set;} �� �����ϸ�, �������� �����ߴٰ� �ҷ��� �� �� Ÿ���� �׸��� ��ǥ�� ���ư���.
    */
    [HideInInspector][SerializeField] private Vector2Int gridPosition;
    public Vector2Int GridPosition
    {
        get { return gridPosition; }
        private set { gridPosition = value; }
    }

    // ���� ������ ���� MaterialPropertyBlock
    private MeshRenderer meshRenderer = default!;
    private MaterialPropertyBlock propBlock = default!; // ��Ƽ���� �Ӽ��� �������̵��ϴ� �淮 ��ü. ��� Ÿ���� ������ ��Ƽ������ �����ϵ� ���� ���������� ������ �� �ִ�.
    private MaterialPropertyBlock indicatorPropBlock = default!;

    // Ÿ�Ͽ� ��Ÿ���� UI �����
    private Color tileHighlightColor = new Color(0f, 0.25f, 0f);
    private Color defaultIndicatorColor = new Color(0.94f, 0.56f, 0.12f);
    private Color medicIndicatorColor = new Color(0.12f, 0.65f, 0.95f);

    // ��ã�� �˰����� ���� �Ӽ���
    public int GCost { get; set; }
    public int HCost { get; set; }
    public int FCost => GCost + HCost;
    public Tile Parent { get; set; } = default!;

    private void Awake()
    {
        PrepareHighlight();
        InitializeGridPosition();
        size2D = new Vector2(tileScale, tileScale);

        Enemy.OnEnemyDespawned += HandleEnemyDespawn;
    }

    private void PrepareHighlight()
    {
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        propBlock = new MaterialPropertyBlock();
        indicatorPropBlock = new MaterialPropertyBlock(); // �ʱ�ȭ

        ResetHighlight();
    }

    private void OnValidate()
    {
        Initialize();
        InitializeGridPosition();
    }

    // ������Ʈ Ȱ��ȭ���� ȣ��
    private void OnEnable()
    {
        Initialize();
        InitializeGridPosition();

    }

    private void Initialize()
    {
        InitializeVisuals();
        InitializeIndicatorPosition();
    }

    private void InitializeIndicatorPosition()
    {
        // Ÿ�� ���� ��ġ�� �������� y���� Ÿ���� y�������� ���� + 0.01
        Vector3 tilePosition = transform.position;
        float indicatorY = transform.localScale.y / 2f + 0.01f;
        attackRangeIndicator.gameObject.transform.position = new Vector3(tilePosition.x, indicatorY, tilePosition.z);

        // ���ʿ��� ��Ȱ��ȭ
        attackRangeIndicator.gameObject.SetActive(false);
    }

    public void SetTileData(TileData tileData, Vector2Int gridPosition)
    {
        data = tileData;
        GridPosition = gridPosition;
        IsWalkable = data.isWalkable;

        AdjustScale();
        InitializeVisuals();
    }

    public void AdjustScale()
    {
        Vector3 targetScale = new Vector3(tileScale, GetHeightScale(), tileScale);
        transform.localScale = targetScale;
    }

    // ��ġ�� ��Ҵ� �� ���� ���ݺ��� ���� ���� ��
    public float GetHeightScale()
    {
        return (data != null && data.terrain == TileData.TerrainType.Hill) ? 0.5f : 0.1f;
    }

    private void InitializeVisuals()
    {
        if (meshRenderer == null || data == null) return;

        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_BaseColor", data.tileColor);
        meshRenderer.SetPropertyBlock(propBlock);
    }

    public bool CanPlaceDeployable()
    {
        return
            !data.isStartPoint && // ������ �ƴ�
            !data.isEndPoint && // ���� �ƴ�
            (OccupyingDeployable == null) &&  // �����ϰ� �ִ� ��ü ����
            (data.isDeployable); // �� Ÿ���� ��ġ ��������
    }

    public void SetOccupied(DeployableUnitEntity deployable)
    {
        OccupyingDeployable = deployable;
    }


    public void ClearOccupied()
    {
        OccupyingDeployable = null;
    }

    public void ShowAttackRange(bool isMedic)
    {
        Color targetColor = isMedic ? medicIndicatorColor : defaultIndicatorColor;

        attackRangeIndicator.GetPropertyBlock(indicatorPropBlock);
        indicatorPropBlock.SetColor("_Color", targetColor); // URP Lit�� �޸�, attackRange��� ������ ���̴��� �ְ� Color��� ������Ƽ�� ����
        attackRangeIndicator.SetPropertyBlock(indicatorPropBlock);

        attackRangeIndicator.gameObject.SetActive(true);
    }

    public void HideAttackRange()
    {
        attackRangeIndicator.gameObject.SetActive(false);
    }

    public void Highlight()
    {
        // materialInstance.EnableKeyword("_EMISSION");

        meshRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_EmissionColor", tileHighlightColor);
        meshRenderer.SetPropertyBlock(propBlock);
    }

    public void ResetHighlight()
    {
        // materialInstance.DisableKeyword("_EMISSION");

        meshRenderer.GetPropertyBlock(propBlock);
        // _EmissionColor �Ӽ��� ���������� �����Ͽ� ȿ���� ���ϴ�.
        propBlock.SetColor("_EmissionColor", Color.black);
        meshRenderer.SetPropertyBlock(propBlock);
    }

    public EnemySpawner GetSpawner()
    {
        return GetComponentInChildren<EnemySpawner>();
    }

    public bool HasSpawner()
    {
        return GetSpawner() != null;
    }

    public void SetGridPosition(Vector2Int gridPos)
    {
        GridPosition = gridPos;
    }

    public Vector3 GetWorldPosition()
    {
        Map map = GetComponentInParent<Map>();
        return new Vector3(GridPosition.x, 0, map.Height - 1 - GridPosition.y);
    }

    // �� Ÿ��
    private void InitializeGridPosition()
    {
        GridPosition = ExtractGridPositionFromName(name);
    }

    private Vector2Int ExtractGridPositionFromName(string tileName)
    {
        string[] parts = tileName.Split('_');
        if (parts.Length >= 3 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int y))
        {
            return new Vector2Int(x, y);
        }
        Debug.LogWarning($"Ÿ�� �̸����� �׸��� ��ǥ ���� ����: {tileName}");
        return Vector2Int.zero;
    }

    // Ÿ�Ͽ� �ö� �� �����ϴ� �޼���� -------
    public bool IsEnemyOnTile(Enemy enemy)
    {
        Vector3 enemyPosition = enemy.transform.position;
        Vector3 tilePosition = transform.position;

        // 3D -> 2D ��ǥ�� ��ȯ
        Vector2 enemyPosition2D = new Vector2(enemyPosition.x, enemyPosition.z);
        Vector2 tilePosition2D = new Vector2(tilePosition.x, tilePosition.z);

        // Ÿ�� ��� ���
        float tolerance = 0f; // Ÿ�� ���� ������ ������ ����
        float minX = tilePosition2D.x - size2D.x / 2 - tolerance;
        float maxX = tilePosition2D.x + size2D.x / 2 + tolerance;
        float minY = tilePosition2D.y - size2D.y / 2 - tolerance;
        float maxY = tilePosition2D.y + size2D.y / 2 + tolerance;

        // ���� x, z ��ǥ�� Ÿ�� ��� ���� �ִ��� Ȯ��
        return enemyPosition2D.x >= minX && enemyPosition2D.x <= maxX && enemyPosition2D.y >= minY && enemyPosition2D.y <= maxY;
    }

    // ���� Ÿ�Ͽ� ����
    public void EnemyEntered(Enemy enemy)
    {
        if (!enemiesOnTile.Contains(enemy))
        {
            enemiesOnTile.Add(enemy);

            // �� Ÿ���� ���ݹ����� �ϴ� ���۷����Ϳ��� �˸�
            foreach (var op in listeningOperators)
            {
                op.OnEnemyEnteredAttackRange(enemy);
            }
        }
    }

    // ���� Ÿ�Ͽ��� ����
    public void EnemyExited(Enemy enemy)
    {
        enemiesOnTile.Remove(enemy);

        // �� Ÿ���� ���ݹ����� �ϴ� ���۷����Ϳ��� �˸�
        foreach (var op in listeningOperators)
        {
            op.OnEnemyExitedAttackRange(enemy);
        }
    }

    // Ÿ�Ͽ� �ö� �� �����ϴ� �޼���� �� -------
    public void ToggleWalkable(bool isWalkable)
    {
        IsWalkable = isWalkable;
    }

    // �ٸ����̵尡 �ִ��� ���� ����
    public bool HasBarricade()
    {
        if (OccupyingDeployable is Barricade)
        {
            return true;
        }
        return false;
    }

    // ���۷����Ͱ� Ÿ���� ���� ������ ���
    public void RegisterOperator(Operator op)
    {
        if (!listeningOperators.Contains(op))
        {
            listeningOperators.Add(op);
        }
    }

    // ���۷����Ͱ� Ÿ���� ���� �������� ����
    public void UnregisterOperator(Operator op)
    {
        if (listeningOperators.Contains(op))
        {
            listeningOperators.Remove(op);
        }

    }

    // �� ��� �̺�Ʈ�� �޾� ����
    private void HandleEnemyDespawn(Enemy enemy, DespawnReason reason)
    {
        // Ÿ�� ���� ���� �ִٸ� ����
        if (enemiesOnTile.Contains(enemy))
        {
            EnemyExited(enemy);
        }
    }

    void OnDestroy()
    {
        Enemy.OnEnemyDespawned -= HandleEnemyDespawn;
    }

}
