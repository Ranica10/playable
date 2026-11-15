using UnityEditor;
using UnityEngine;

public class AssetSetupWindow : EditorWindow
{
    private const string ProfilePath =
        "Assets/Playable/DefaultAssetProfile.asset";

    private PlayableAssetProfile profile;
    private SerializedObject serializedProfile;

    [MenuItem("Tools/Playable/Asset Setup")]
    public static void Open()
    {
        GetWindow<AssetSetupWindow>("Playable Assets");
    }

    private void OnEnable()
    {
        profile = AssetDatabase.LoadAssetAtPath<PlayableAssetProfile>(
            ProfilePath
        );

        if (profile == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Playable"))
            {
                AssetDatabase.CreateFolder("Assets", "Playable");
            }

            profile = CreateInstance<PlayableAssetProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            AssetDatabase.SaveAssets();
        }

        serializedProfile = new SerializedObject(profile);
    }

    private void OnGUI()
    {
        if (profile == null || serializedProfile == null)
        {
            EditorGUILayout.HelpBox(
                "Reopen this window to load the asset profile.",
                MessageType.Warning
            );
            return;
        }

        GUILayout.Label(
            "Assign Your Sprites",
            EditorStyles.boldLabel
        );

        serializedProfile.Update();

        EditorGUILayout.PropertyField(
            serializedProfile.FindProperty("player"));
        EditorGUILayout.PropertyField(
            serializedProfile.FindProperty("ground"));
        EditorGUILayout.PropertyField(
            serializedProfile.FindProperty("coin"));
        EditorGUILayout.PropertyField(
            serializedProfile.FindProperty("spikes"));
        EditorGUILayout.PropertyField(
            serializedProfile.FindProperty("exit"));

        serializedProfile.ApplyModifiedProperties();

        EditorGUILayout.Space();

        if (GUILayout.Button("Save Setup"))
        {
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            ShowNotification(new GUIContent("Asset setup saved!"));
        }
    }
}