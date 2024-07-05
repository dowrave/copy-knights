using UnityEngine;

public enum TileType
{
    Ground,
    Hill,
    Start,
    End,
    Empty
}


public class Tile : MonoBehaviour
{
    public TileType Type { get ; private set; }
    public Vector2Int GridPosition { get; private set; }

    public bool IsWalkable => Type != TileType.Hill && Type != TileType.Empty; // &&�� ����. 

    public bool IsOccupied { get; private set; } // �ش� Ÿ�Ͽ� �̹� ��ġ�� �Ǿ��°�

    private Renderer tileRenderer;
    //private Material originalMaterial;
    private Color originalColor;
    private static readonly int ColorProperty = Shader.PropertyToID("_Color");

    // ��ã�� �˰����� ���� �Ӽ���
    public int GCost { get; set; }
    public int HCost { get; set; }
    public int FCost => GCost + HCost;
    public Tile Parent { get; set; }

    private void Awake()
    {
        // �ڽ� ������Ʈ Cube�� Renderer�� �����´�.
        tileRenderer = GetComponentInChildren<Renderer>();
        if (tileRenderer != null)
        {
            originalColor = tileRenderer.material.GetColor(ColorProperty);
        }
    }

    public void Initialize(TileType type, Vector2Int gridPosition, float height)
    {
        Type = type;
        GridPosition = gridPosition;
        SetHeight(height);
        UpdateVisuals();
    }

    public void SetHeight(float height)
    {
        transform.localScale = new Vector3(transform.localScale.x, height, transform.localScale.z);
        transform.localPosition = new Vector3(transform.localPosition.x, height / 2f, transform.localPosition.z);
    }

    private void UpdateVisuals()
    {
        if (tileRenderer == null) return;

        SetTileColor(GetColorForTileType());
    }

    private void SetTileColor(Color color)
    {
        if (tileRenderer != null && tileRenderer.material != null) 
        {
            tileRenderer.material.SetColor(ColorProperty, color);
        }
    }

    private Color GetColorForTileType()
    {
        return Type switch
        {
            TileType.Ground => Color.gray,
            TileType.Hill => Color.gray,
            TileType.Start => Color.red,
            TileType.End => Color.blue,
            _ => originalColor
        };
    }

    public bool CanPlaceOperator()
    {
        return (IsOccupied == false) && (Type == TileType.Ground || Type == TileType.Hill);
    }

    public void SetOccupied(bool occupied)
    {
        IsOccupied = occupied;
    }

    public void Highlight(Color color)
    {
        if (tileRenderer != null)
        {
            // ���� ��Ƽ������ �� ����
            tileRenderer.material.color = color;
        } 
    }

    public void ResetHighlight()
    {
        if (tileRenderer != null)
        {
            Debug.Log("Tile.cs : ResetHighlight �۵�");
            SetTileColor(GetColorForTileType());
        }
    }
}
