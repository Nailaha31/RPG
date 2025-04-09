using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MapData
{
    public string sceneName;
    public Vector2Int coords;

#if UNITY_EDITOR
    public UnityEditor.SceneAsset sceneAsset;
#endif

    public string up;
    public string down;
    public string left;
    public string right;
    public string description = "";

    public bool includeInBuild = true; // ✅ Nouveau champ pour contrôle du Build Settings
}

[CreateAssetMenu(fileName = "WorldMap", menuName = "World Editor/World Map", order = 1)]
public class WorldMap : ScriptableObject
{
    public List<MapData> maps = new List<MapData>();
}
