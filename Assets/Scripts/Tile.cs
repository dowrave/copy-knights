using UnityEngine;

[ExecuteAlways] // ������, ��Ÿ�� ��ο��� ��ũ��Ʈ ����
public class Tile : MonoBehaviour
{
    public TileData data;
    public Vector2Int GridPosition { get; private set; }
    public bool IsOccupied { get; private set; }

    [SerializeField] private Material baseTileMaterial; // Inspector���� �Ҵ���
    private Renderer tileRenderer;
    private MaterialPropertyBlock propBlock; // ��Ƽ���� �Ӽ��� �������̵��ϴ� �淮 ��ü. ��� Ÿ���� ������ ��Ƽ������ �����ϵ� ���� ���������� ������ �� �ִ�.

    // ��ã�� �˰����� ���� �Ӽ���
    public int GCost { get; set; }
    public int HCost { get; set; }
    public int FCost => GCost + HCost;
    public Tile Parent { get; set; }

    // ������Ʈ Ȱ��ȭ���� ȣ��
    private void OnEnable()
    {
        Initialize();
    }

    // �ν����Ϳ��� ������Ʈ ���� ����� ������ ȣ�� - �����Ϳ��� �ٷ� Ȯ�� ����
    private void OnValidate()
    {
        Initialize();
    }

    private void Initialize()
    {
        // �ڽ� ������Ʈ Cube�� Renderer�� �����´�.
        if (tileRenderer == null)
        {
            tileRenderer = GetComponentInChildren<Renderer>();
        }

        else
        {
            tileRenderer.sharedMaterial = baseTileMaterial;
        }

        propBlock = new MaterialPropertyBlock();
        UpdateVisuals();
    }

    public void SetTileData(TileData tileData, Vector2Int gridPosition)
    {
        data = tileData;
        GridPosition = gridPosition;
        SetScale();
        UpdateVisuals();
    }

    public void SetScale()
    {
        if (data == null) return;
        float height = (data.terrain == TileData.TerrainType.Hill) ? 0.5f : 0.1f;
        transform.localScale = new Vector3(transform.localScale.x * 0.98f, height, transform.localScale.z * 0.98f);
        transform.localPosition = new Vector3(transform.localPosition.x, height / 2f, transform.localPosition.z);
    }

    public float GetHeight()
    {
        return transform.localScale.y;
    }

    private void UpdateVisuals()
    {
        if (tileRenderer == null || data == null) return;

        tileRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", data.tileColor);
        tileRenderer.SetPropertyBlock(propBlock);
        
    }


    public bool CanPlaceOperator()
    {
        return (IsOccupied == false) && (data.canPlaceOperator);
    }

    public void SetOccupied(bool occupied)
    {
        IsOccupied = occupied;
    }

    public void Highlight(Color color)
    {
        if (tileRenderer != null)
        {
            tileRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor("_Color", color);
            tileRenderer.SetPropertyBlock(propBlock);
        } 
    }

    public void ResetHighlight()
    {
        if (tileRenderer != null)
        {
            UpdateVisuals();
        }
    }
}
