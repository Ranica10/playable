using UnityEditor;
using UnityEngine;

public class AssetSetupWindow : EditorWindow
{
    private Sprite player;
    private Sprite ground;
    private Sprite coin;
    private Sprite spikes;
    private Sprite exit;

    [MenuItem("Tools/Playable/Asset Setup")]
    public static void Open()
    {
        GetWindow<AssetSetupWindow>("Playable Assets");
    }

    private void OnGUI()
    {
        GUILayout.Label("Assign Your Sprites", EditorStyles.boldLabel);

        player = SpriteField("Player", player);
        ground = SpriteField("Ground", ground);
        coin = SpriteField("Coin", coin);
        spikes = SpriteField("Spikes", spikes);
        exit = SpriteField("Exit", exit);

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            player != null && ground != null
                ? "Player and ground assigned."
                : "Assign at least a player and ground sprite.",
            MessageType.Info
        );
    }

    private static Sprite SpriteField(string label, Sprite value)
    {
        return (Sprite)EditorGUILayout.ObjectField(
            label,
            value,
            typeof(Sprite),
            false
        );
    }
}