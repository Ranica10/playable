using UnityEditor;
using UnityEngine;

public static class AssetScanner
{
    [MenuItem("Tools/Playable/Scan Assets")]
    public static void ScanAssets()
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:Prefab",
            new[] { "Assets" }
        );

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log(path);
        }

        Debug.Log($"Found {guids.Length} prefabs.");
    }
}