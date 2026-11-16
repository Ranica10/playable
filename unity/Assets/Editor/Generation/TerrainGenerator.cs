using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TerrainGenerator
{
    public static void Generate(TerrainCellData[] cells, Sprite ground,
        Material material, string outputFolder)
    {
        if (cells == null || cells.Length == 0) return;

        var grid = new GameObject("Level Grid", typeof(Grid));
        // Centre cell (0,0) on world (0,0).
        grid.transform.position = new Vector3(-0.5f, -0.5f, 0);
        var terrain = new GameObject("Terrain", typeof(Tilemap), typeof(TilemapRenderer));
        terrain.transform.SetParent(grid.transform, false);
        var tilemap = terrain.GetComponent<Tilemap>();
        terrain.GetComponent<TilemapRenderer>().sharedMaterial = material;

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = ground;
        tile.colliderType = Tile.ColliderType.Sprite;
        Vector2 size = ground.bounds.size;
        Vector2 centre = ground.bounds.center;
        Vector3 scale = new Vector3(1f / size.x, 1f / size.y, 1);
        tile.transform = Matrix4x4.TRS(
            new Vector3(-centre.x * scale.x, -centre.y * scale.y, 0),
            Quaternion.identity, scale);
        // Save the Tile so the scene remains editable after closing/reopening.
        AssetDatabase.CreateAsset(tile, outputFolder + "/Ground.asset");

        foreach (var cell in cells)
            tilemap.SetTile(new Vector3Int(cell.x, cell.y, 0), tile);
        terrain.AddComponent<TilemapCollider2D>();
    }
}
