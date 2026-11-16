using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class LevelGenerator
{
    [MenuItem("Tools/Playable/Generate From JSON")]
    public static void Generate()
    {
        string path = EditorUtility.OpenFilePanel("Choose level JSON", "", "json");
        if (string.IsNullOrEmpty(path)) return;
        try { GenerateFromFile(path); }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorUtility.DisplayDialog("Generation failed", error.Message, "OK");
        }
    }

    // Call this method from a CLI adapter when you wire up your command.
    // Set promptToSave=false for unattended operation in a dedicated generator
    // project only: that setting skips prompts and replaces the active scene.
    public static string GenerateFromFile(string jsonPath, bool promptToSave = true)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play mode first.");
        var level = JsonUtility.FromJson<LevelData>(File.ReadAllText(jsonPath));
        var profile = AssetDatabase.LoadAssetAtPath<PlayableAssetProfile>(
            "Assets/Playable/DefaultAssetProfile.asset");
        Validate(level, profile);

        bool hasTerrain = level.terrain != null && level.terrain.Length > 0;
        bool hasObjects = level.objects != null && level.objects.Length > 0;
        Material material = null;
        if (hasTerrain || hasObjects)
        {
            material = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            if (material == null)
                throw new InvalidOperationException("URP Sprite-Unlit-Default material not found. This generator targets Universal 2D.");
        }
        if (promptToSave && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return null;

        if (!AssetDatabase.IsValidFolder("Assets/GeneratedLevels"))
            AssetDatabase.CreateFolder("Assets", "GeneratedLevels");
        string folder = AssetDatabase.GenerateUniqueAssetPath(
            "Assets/GeneratedLevels/" + SafeName(level.levelName));
        AssetDatabase.CreateFolder("Assets/GeneratedLevels", Path.GetFileName(folder));
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        if (hasTerrain)
            TerrainGenerator.Generate(level.terrain, profile.ground, material, folder);
        if (hasObjects)
        {
            var parent = new GameObject("Level Objects").transform;
            foreach (var item in level.objects)
                ObjectGenerator.Generate(item, profile, material, parent);
        }
        CreateCamera(level);
        AssetDatabase.SaveAssets();
        string scenePath = folder + "/Level.unity";
        if (!EditorSceneManager.SaveScene(scene, scenePath))
            throw new IOException("Could not save scene at " + scenePath);
        Debug.Log("Generated level: " + scenePath);
        return scenePath;
    }

    private static void Validate(LevelData level, PlayableAssetProfile profile)
    {
        if (level == null || level.schemaVersion != 1)
            throw new ArgumentException("Expected schemaVersion: 1.");
        int terrainCount = level.terrain?.Length ?? 0;
        int objectCount = level.objects?.Length ?? 0;
        if ((long)terrainCount + objectCount > 10000)
            throw new ArgumentException("Starter limit: 10,000 cells/objects.");
        // A deliberately empty layout is valid and needs no asset profile.
        if (terrainCount + objectCount == 0) return;
        if (profile == null)
            throw new InvalidOperationException("Save your Playable asset profile first.");
        if (terrainCount > 0)
        {
            CheckSprite(profile.ground, "ground");
            foreach (var cell in level.terrain)
                if (cell == null || Math.Abs((long)cell.x) > 10000 || Math.Abs((long)cell.y) > 10000)
                    throw new ArgumentException("Invalid terrain cell (coordinate limit: 10,000).");
        }
        int players = 0;
        if (level.objects == null) return;
        foreach (var item in level.objects)
        {
            if (item == null) throw new ArgumentException("Null object in layout.");
            CheckSprite(ObjectGenerator.RequiredSprite(item.type, profile), item.type);
            if (float.IsNaN(item.x) || float.IsInfinity(item.x) ||
                float.IsNaN(item.y) || float.IsInfinity(item.y) ||
                Math.Abs(item.x) > 10000 || Math.Abs(item.y) > 10000)
                throw new ArgumentException("Invalid object coordinates.");
            if (ObjectGenerator.NormalizeType(item.type) == "player" && ++players > 1)
                throw new ArgumentException("This starter supports one player per level.");
        }
    }

    private static void CheckSprite(Sprite sprite, string role)
    {
        if (sprite == null || sprite.bounds.size.x <= 0 || sprite.bounds.size.y <= 0)
            throw new InvalidOperationException("Assign a valid " + role + " sprite in Asset Setup and Save Setup.");
    }

    private static string SafeName(string name)
    {
        string result = "";
        foreach (char c in name ?? "GeneratedLevel")
            result += char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_';
        if (string.IsNullOrWhiteSpace(result)) result = "GeneratedLevel";
        return result.Substring(0, Math.Min(64, result.Length));
    }

    private static void CreateCamera(LevelData level)
    {
        Bounds bounds = new Bounds();
        bool hasBounds = false;
        Action<Vector2> include = point =>
        {
            var b = new Bounds(new Vector3(point.x, point.y, 0), new Vector3(2, 2, 0));
            if (!hasBounds) { bounds = b; hasBounds = true; }
            else bounds.Encapsulate(b);
        };
        if (level.terrain != null)
            foreach (var cell in level.terrain) include(new Vector2(cell.x, cell.y));
        if (level.objects != null)
            foreach (var item in level.objects) include(new Vector2(item.x, item.y));
        var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(3, bounds.extents.y + 1,
            (bounds.extents.x + 1) / Mathf.Max(0.1f, camera.aspect));
        camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.15f, 0.2f, 0.3f);
    }
}
