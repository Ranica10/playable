using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class LevelGenerator
{
    [MenuItem("Tools/Playable/Generate Level")]
    public static void Generate()
    {
        // Don't allow generation while in play mode or about to enter it.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Exit Play mode before generating a level.");
            return;
        }

        // get the saved asset profile, which contains the sprites to use for generation.
        var profile = AssetDatabase.LoadAssetAtPath<PlayableAssetProfile>(
            "Assets/Playable/DefaultAssetProfile.asset"
        );

        // Check that the profile and required sprites are assigned.
        if (profile == null || profile.player == null ||
            profile.ground == null)
        {
            EditorUtility.DisplayDialog(
                "Missing Assets",
                "Assign and save a player and ground sprite first.",
                "OK"
            );
            return;
        }

        // Ask about unsaved work before opening a new scene.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Single
        );

        // Create the camera.
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";

        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5;
        camera.transform.position = new Vector3(0, 3, -10);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.15f, 0.2f, 0.3f);

        // Floor: a row of one-unit tiles.
        // Create a grid and its terrain Tilemap.
        var gridObject = new GameObject("Level Grid", typeof(Grid));

        // Offset cells so their centres match the old platform positions.
        gridObject.transform.position = new Vector3(-0.5f, -0.5f, 0);

        var terrainObject = new GameObject(
            "Terrain",
            typeof(Tilemap),
            typeof(TilemapRenderer)
        );

        terrainObject.transform.SetParent(gridObject.transform, false);

        var tilemap = terrainObject.GetComponent<Tilemap>();

        // Create or update a persistent Tile asset.
        const string tileFolder = "Assets/Playable/Tiles";
        const string tilePath = tileFolder + "/Ground.asset";

        if (!AssetDatabase.IsValidFolder(tileFolder))
        {
            AssetDatabase.CreateFolder("Assets/Playable", "Tiles");
        }

        var groundTile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);

        if (groundTile == null)
        {
            groundTile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(groundTile, tilePath);
        }

        groundTile.sprite = profile.ground;
        groundTile.colliderType = Tile.ColliderType.Grid;

        // Fit the sprite rectangle to a 1-by-1 grid cell.
        Vector2 spriteSize = profile.ground.bounds.size;
        Vector2 spriteCentre = profile.ground.bounds.center;

        Vector3 scale = new Vector3(
            1f / spriteSize.x,
            1f / spriteSize.y,
            1f
        );

        groundTile.transform = Matrix4x4.TRS(
            new Vector3(
                -spriteCentre.x * scale.x,
                -spriteCentre.y * scale.y,
                0
            ),
            Quaternion.identity,
            scale
        );

        EditorUtility.SetDirty(groundTile);
        AssetDatabase.SaveAssets();

        // Floor.
        for (int x = -8; x <= 8; x++)
        {
            tilemap.SetTile(new Vector3Int(x, 0, 0), groundTile);
        }

        // First raised platform.
        for (int x = -3; x <= -1; x++)
        {
            tilemap.SetTile(new Vector3Int(x, 2, 0), groundTile);
        }

        // Second raised platform: grid positions use whole numbers.
        for (int x = 2; x <= 4; x++)
        {
            tilemap.SetTile(new Vector3Int(x, 4, 0), groundTile);
        }

        // Generate terrain collision.
        terrainObject.AddComponent<TilemapCollider2D>();

        // Player.
        var player = CreateSprite(
            "Player", profile.player,
            new Vector2(-6, 2), new Vector2(0.8f, 1.2f)
        );

        player.AddComponent<BoxCollider2D>();

        var body = player.AddComponent<Rigidbody2D>();
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Optional objects: visual placement only for now.
        if (profile.coin != null)
        {
            CreateSprite("Coin", profile.coin,
                new Vector2(-2, 3), Vector2.one * 0.5f);
        }

        if (profile.spikes != null)
        {
            CreateSprite("Spikes", profile.spikes,
                new Vector2(1, 0.8f), new Vector2(1, 0.6f));
        }

        if (profile.exit != null)
        {
            CreateSprite("Exit", profile.exit,
                new Vector2(7, 1.25f), new Vector2(1, 1.5f));
        }

        if (!AssetDatabase.IsValidFolder("Assets/GeneratedLevels"))
        {
            AssetDatabase.CreateFolder("Assets", "GeneratedLevels");
        }

        // Use a unique name so previous levels aren't overwritten.
        string path = AssetDatabase.GenerateUniqueAssetPath(
            "Assets/GeneratedLevels/TestLevel.unity"
        );

        if (EditorSceneManager.SaveScene(scene, path))
            Debug.Log($"Generated level saved to {path}");
        else
            Debug.LogError("Could not save the generated scene.");
    }

    private static void CreatePlatform(Sprite sprite, float x, float y)
    {
        var platform = CreateSprite(
            "Ground", sprite,
            new Vector2(x, y), Vector2.one
        );

        platform.AddComponent<BoxCollider2D>();
    }

    private static GameObject CreateSprite(
        string name, Sprite sprite, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name);
        obj.transform.position = position;

        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        // Normalize size regardless of the image's pixel dimensions.
        Vector2 originalSize = sprite.bounds.size;
        obj.transform.localScale = new Vector3(
            size.x / originalSize.x,
            size.y / originalSize.y,
            1
        );

        // Keep test sprites visible without requiring scene lights.
        renderer.sharedMaterial =
            AssetDatabase.GetBuiltinExtraResource<Material>(
                "Sprites-Default.mat"
            );

        return obj;
    }
}