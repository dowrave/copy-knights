using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ���� �гο��� ���������� ���Ǵ� ��ɵ��� ��Ƽ� �����صӴϴ�
/// </summary>
public class UIHelper : MonoBehaviour
{
    //[Header("Container Reference")]
    //[SerializeField] private RectTransform attackRangeContainer;

    [Header("Attack Range Tile Prefabs")]
    [SerializeField] private Image filledTilePrefab;
    [SerializeField] private Image outlineTilePrefab;
    [SerializeField] private Image highlightTilePrefab;


    // �̱��� ����
    public static UIHelper Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public class AttackRangeHelper
    {
        private List<Image> rangeTiles = new List<Image>();
        private RectTransform containerRect;
        private float centerOffset;
        private float tileSize;

        // �����ڿ��� ������ ������ �����ϰ� �����̳ʿ� �����¸� �޵��� ����
        public AttackRangeHelper(RectTransform container, float offset)
        {
            containerRect = container;
            centerOffset = offset;
            // Ÿ�� ũ��� Instance�� �����տ��� ������
            tileSize = Instance.filledTilePrefab.rectTransform.rect.width;
        }

        private Image CreateTile(Vector2Int gridPos, bool isCenter, bool isHighlight)
        {
            // �������� ���� Instance���� ������
            Image tilePrefab = isCenter ? Instance.filledTilePrefab :
                             isHighlight ? Instance.highlightTilePrefab :
                             Instance.outlineTilePrefab;

            Image tile = GameObject.Instantiate(tilePrefab, containerRect);

            float interval = tileSize / 4f;
            float gridX = gridPos.x * (tileSize + interval);
            float gridY = gridPos.y * (tileSize + interval);

            tile.rectTransform.anchoredPosition = new Vector2(
                gridX - centerOffset,
                gridY
            );

            rangeTiles.Add(tile);
            return tile;
        }

        // �⺻ ���� ������ ǥ��
        public void ShowBasicRange(List<Vector2Int> attackableTiles)
        {
            ClearTiles();
            CreateCenterTile();

            foreach (Vector2Int pos in attackableTiles)
            {
                // ���� ���� - ������ ������ ���� ���� ������ ���� ������ ����
                Vector2Int convertedPos = new Vector2Int(-pos.x, -pos.y);
                if (convertedPos != Vector2Int.zero)
                {
                    CreateTile(convertedPos, false, false);
                }
            }
        }
        private void CreateCenterTile()
        {
            CreateTile(Vector2Int.zero, true, false);
        }

        private void ClearTiles()
        {
            foreach (var tile in rangeTiles)
            {
                GameObject.Destroy(tile.gameObject);
            }
            rangeTiles.Clear();
        }

        // �⺻ ������ �ر� ������ �Բ� ǥ��
        public void ShowRangeWithUnlocks(List<Vector2Int> baseTiles, List<Vector2Int> additionalTiles)
        {
            ShowBasicRange(baseTiles);

            if (additionalTiles != null)
            {
                foreach (Vector2Int pos in additionalTiles)
                {
                    Vector2Int convertedPos = new Vector2Int(-pos.x, -pos.y);
                    CreateTile(convertedPos, false, true);
                }
            }
        }
    }
    public AttackRangeHelper CreateAttackRangeHelper(RectTransform container, float offset)
    {
        return new AttackRangeHelper(container, offset);
    }
}