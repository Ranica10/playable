using System;
using UnityEngine;
using Playable.Gameplay;

public static class ObjectGenerator
{
    public static string NormalizeType(string type)
    {
        string value = (type ?? "").Trim().ToLowerInvariant();
        return value == "spawn" ? "player" : value;
    }

    public static Sprite RequiredSprite(string type, PlayableAssetProfile profile)
    {
        switch (NormalizeType(type))
        {
            case "player": return profile.player;
            case "collectible": return profile.collectible;
            case "spikes": return profile.spikes;
            case "exit": return profile.exit;
            default: throw new ArgumentException("Unknown object type: " + type);
        }
    }

    public static void Generate(LevelObjectData item, PlayableAssetProfile profile,
        Material material, Transform parent)
    {
        switch (NormalizeType(item.type))
        {
            case "player": CreatePlayer(item, profile.player, material, parent); break;
            case "collectible": CreateCollectible(item, profile.collectible, material, parent); break;
            case "spikes": CreateSpikes(item, profile.spikes, material, parent); break;
            case "exit": CreateExit(item, profile.exit, material, parent); break;
            default: throw new ArgumentException("Unknown object type: " + item.type);
        }
    }

    private static void CreatePlayer(LevelObjectData item, Sprite sprite,
        Material material, Transform parent)
    {
        var size = new Vector2(0.8f, 1.2f);
        var obj = CreateSprite("Player", item, sprite, size, material, parent);
        obj.AddComponent<BoxCollider2D>().size = size;
        var body = obj.AddComponent<Rigidbody2D>();
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        obj.AddComponent<PlayerController>();
    }

    private static void CreateCollectible(LevelObjectData item, Sprite sprite,
        Material material, Transform parent)
    {
        var size = new Vector2(0.5f, 0.5f);
        var obj = CreateSprite("Collectible", item, sprite, size, material, parent);
        AddTrigger(obj, size);
        obj.AddComponent<Collectible>();
    }

    private static void CreateSpikes(LevelObjectData item, Sprite sprite,
        Material material, Transform parent)
    {
        var size = new Vector2(1, 0.6f);
        var obj = CreateSprite("Spikes", item, sprite, size, material, parent);
        AddTrigger(obj, size);
        obj.AddComponent<Hazard>();
    }

    private static void CreateExit(LevelObjectData item, Sprite sprite,
        Material material, Transform parent)
    {
        var size = new Vector2(1, 1.5f);
        var obj = CreateSprite("Exit", item, sprite, size, material, parent);
        AddTrigger(obj, size);
        obj.AddComponent<LevelExit>();
    }

    private static void AddTrigger(GameObject obj, Vector2 size)
    {
        var collider = obj.AddComponent<BoxCollider2D>();
        collider.size = size;
        collider.isTrigger = true;
    }

    private static GameObject CreateSprite(string name, LevelObjectData item,
        Sprite sprite, Vector2 size, Material material, Transform parent)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(item.x, item.y, 0);
        // Keep physics root at scale 1. Fit only the visual child to world size.
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = material;
        Vector2 bounds = sprite.bounds.size;
        Vector2 centre = sprite.bounds.center;
        Vector3 scale = new Vector3(size.x / bounds.x, size.y / bounds.y, 1);
        visual.transform.localScale = scale;
        visual.transform.localPosition = new Vector3(
            -centre.x * scale.x, -centre.y * scale.y, 0);
        return root;
    }
}
