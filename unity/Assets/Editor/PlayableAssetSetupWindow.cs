using UnityEditor;
using UnityEngine;

public class PlayableAssetSetupWindow : EditorWindow
{
    private const string ProfilePath =
        "Assets/Playable/DefaultAssetProfile.asset";

    private PlayableAssetProfile profile;
    private SerializedObject serializedProfile;

    [MenuItem("Tools/Playable/Asset Setup")]
    public static void Open()
    {
        GetWindow<PlayableAssetSetupWindow>("Asset Setup");
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
                "Close and reopen this window to load the profile.",
                MessageType.Warning
            );
            return;
        }

        GUILayout.Label("Assign Your Sprites", EditorStyles.boldLabel);

        serializedProfile.Update();

        DrawField("player");
        DrawField("ground");
        DrawField("collectible");
        DrawField("spikes");
        DrawField("exit");

        serializedProfile.ApplyModifiedProperties();

        EditorGUILayout.Space();

        if (GUILayout.Button("Save Setup"))
        {
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("Asset setup saved!"));
        }
    }

    private void DrawField(string fieldName)
    {
        var property = serializedProfile.FindProperty(fieldName);

        if (property != null)
        {
            EditorGUILayout.PropertyField(property);
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"PlayableAssetProfile is missing the '{fieldName}' field.",
                MessageType.Error
            );
        }
    }
}