using System;

[Serializable]
public class LevelData
{
    public int schemaVersion = 1;
    public string levelName = "GeneratedLevel";
    public TerrainCellData[] terrain;
    public LevelObjectData[] objects;
}

[Serializable]
public class TerrainCellData
{
    public int x;
    public int y;
}

[Serializable]
public class LevelObjectData
{
    public string type;
    public float x;
    public float y;
}
