using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class WorldMapReport : EditorWindow
{
    private WorldMap worldMap;
    private Vector2 scroll;

    private bool showMissingSceneAsset = true;
    private bool showNotInBuild = true;
    private bool showDuplicates = true;
    private bool showAll = true;

    private List<string> orphans = new List<string>();
    private List<Vector2Int> duplicateCoords = new List<Vector2Int>();

    [MenuItem("Tools/World Map Report")]
    public static void OpenWindow()
    {
        GetWindow<WorldMapReport>("📋 Rapport WorldMap");
    }

    void OnGUI()
    {
        GUILayout.Label("📋 Rapport d'intégrité du WorldMap", EditorStyles.boldLabel);

        worldMap = (WorldMap)EditorGUILayout.ObjectField("World Map Asset", worldMap, typeof(WorldMap), false);

        if (worldMap == null)
        {
            EditorGUILayout.HelpBox("Aucun WorldMap sélectionné", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🔍 Filtres d'affichage", EditorStyles.boldLabel);
        showAll = EditorGUILayout.ToggleLeft("Afficher tout", showAll);
        showMissingSceneAsset = EditorGUILayout.ToggleLeft("❌ SceneAsset manquant", showMissingSceneAsset);
        showNotInBuild = EditorGUILayout.ToggleLeft("⚠️ Hors Build Settings", showNotInBuild);
        showDuplicates = EditorGUILayout.ToggleLeft("🔁 Coordonnées en double", showDuplicates);

        EditorGUILayout.Space();
        if (GUILayout.Button("🔄 Actualiser le rapport"))
        {
            RefreshReport(worldMap);
        }

        if (GUILayout.Button("➕ Ajouter les scènes manquantes au Build Settings"))
        {
            AddAllScenesToBuildSettings(worldMap);
        }

        EditorGUILayout.Space();
        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var map in worldMap.maps)
        {
            bool isMissingAsset = map.sceneAsset == null;
            bool isNotInBuild = !SceneIsInBuildSettings(map.sceneName) && map.includeInBuild;
            bool isDuplicate = duplicateCoords.Contains(map.coords);

            if (!showAll)
            {
                if (isMissingAsset && !showMissingSceneAsset) continue;
                if (isNotInBuild && !showNotInBuild) continue;
                if (isDuplicate && !showDuplicates) continue;
                if (!isMissingAsset && !isNotInBuild && !isDuplicate) continue;
            }

            GUILayout.BeginVertical("box");
            GUILayout.Label($"📄 {map.sceneName} — ({map.coords.x}, {map.coords.y})");

            if (isMissingAsset)
                EditorGUILayout.HelpBox("❌ SceneAsset non assigné", MessageType.Error);

            if (isNotInBuild)
                EditorGUILayout.HelpBox("⚠️ Pas dans Build Settings (mais incluse)", MessageType.Warning);

            if (!map.includeInBuild)
                EditorGUILayout.HelpBox("ℹ️ Exclue volontairement du Build Settings", MessageType.Info);

            if (isDuplicate)
                EditorGUILayout.HelpBox("🔁 Coordonnées en double", MessageType.Warning);

            GUILayout.EndVertical();
        }

        if (orphans.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🚫 Scènes .unity non assignées dans WorldMap", EditorStyles.boldLabel);
            foreach (var path in orphans)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(Path.GetFileName(path));
                if (GUILayout.Button("Ajouter à WorldMap", GUILayout.Width(150)))
                {
                    TryAddOrphanToWorldMap(path);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    void RefreshReport(WorldMap map)
    {
        CleanDeletedScenes(map); // ✅ Nettoyage automatique
        duplicateCoords = map.maps
            .GroupBy(m => m.coords)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        DetectOrphanScenes(map);
    }

    void CleanDeletedScenes(WorldMap map)
    {
        for (int i = map.maps.Count - 1; i >= 0; i--)
        {
            var m = map.maps[i];

            if (m.sceneAsset != null)
                continue;

            // Vérifie si un fichier existe encore avec ce nom
            string[] guids = AssetDatabase.FindAssets(m.sceneName + " t:Scene");
            bool found = guids.Any(guid => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)) == m.sceneName);

            if (!found)
            {
                Debug.Log($"🧹 Supprimé du WorldMap : {m.sceneName}");
                map.maps.RemoveAt(i);
            }
        }

        EditorUtility.SetDirty(map);
        AssetDatabase.SaveAssets();
    }

    void TryAddOrphanToWorldMap(string path)
    {
        string sceneName = Path.GetFileNameWithoutExtension(path);
        var match = System.Text.RegularExpressions.Regex.Match(sceneName, @"([-+]?\d+)_([-+]?\d+)");
        if (match.Success && worldMap != null)
        {
            int x = int.Parse(match.Groups[1].Value);
            int y = int.Parse(match.Groups[2].Value);
            var newMap = new MapData
            {
                coords = new Vector2Int(x, y),
                sceneName = sceneName,
#if UNITY_EDITOR
                sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path)
#endif
            };
            worldMap.maps.Add(newMap);
            EditorUtility.SetDirty(worldMap);
            AssetDatabase.SaveAssets();
            Debug.Log($"✅ Ajouté au WorldMap : {sceneName}");
            RefreshReport(worldMap);
        }
    }

    void DetectOrphanScenes(WorldMap map)
    {
        orphans.Clear();
        var allScenePaths = Directory.GetFiles("Assets/Scenes/Maps", "*.unity", SearchOption.AllDirectories);
        foreach (var path in allScenePaths)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (!map.maps.Exists(m => m.sceneName == name))
            {
                orphans.Add(path);
            }
        }
    }

    bool SceneIsInBuildSettings(string sceneName)
    {
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (Path.GetFileNameWithoutExtension(scene.path) == sceneName)
                return true;
        }
        return false;
    }

    void AddAllScenesToBuildSettings(WorldMap map)
    {
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        foreach (var mapData in map.maps)
        {
            if (!mapData.includeInBuild || mapData.sceneAsset == null)
                continue;

            string path = AssetDatabase.GetAssetPath(mapData.sceneAsset);
            if (!buildScenes.Exists(s => s.path == path))
            {
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
                Debug.Log($"✅ Ajoutée au Build Settings : {mapData.sceneName}");
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log("✅ Build Settings mis à jour.");
    }
}
