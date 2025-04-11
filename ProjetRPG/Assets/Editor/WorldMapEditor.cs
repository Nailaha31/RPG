using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class WorldMapEditor : EditorWindow
{
    private WorldMap worldMap;
    private Vector2 scroll;
    private Vector2Int center = Vector2Int.zero;
    private int visibleHalfSize = 2;
    private Vector2Int? pendingNewMapCoord = null;
    private SceneAsset sceneToFocus = null;

    [MenuItem("Tools/World Map Editor")]
    public static void OpenWindow()
    {
        GetWindow<WorldMapEditor>("🗺️ World Map Editor");
    }

    void OnGUI()
    {
        GUILayout.Label("World Map Editor", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);

        worldMap = (WorldMap)EditorGUILayout.ObjectField("World Map Asset", worldMap, typeof(WorldMap), false);

        if (worldMap == null)
        {
            EditorGUILayout.EndScrollView();
            return;
        }

        // --- Centrage manuel par scène ---
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Centrer la grille sur une scène :", EditorStyles.boldLabel);
        sceneToFocus = (SceneAsset)EditorGUILayout.ObjectField("Scène cible", sceneToFocus, typeof(SceneAsset), false);

        if (sceneToFocus != null)
        {
            string path = AssetDatabase.GetAssetPath(sceneToFocus);
            string sceneName = Path.GetFileNameWithoutExtension(path);
            MapData map = worldMap.maps.Find(m => m.sceneName == sceneName);
            if (map != null)
            {
                center = map.coords;
                sceneToFocus = null;
            }
            else
            {
                EditorGUILayout.HelpBox("❌ Map non trouvée dans le WorldMap.", MessageType.Warning);
            }
        }

        // --- Centrage manuel par coordonnées ---
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Centrer manuellement :", EditorStyles.boldLabel);
        center.x = EditorGUILayout.IntField("Coordonnée X", center.x);
        center.y = EditorGUILayout.IntField("Coordonnée Y", center.y);

        // --- Boutons utilitaires ---
        EditorGUILayout.Space();
        if (GUILayout.Button("🔁 Recharger les maps"))
        {
            CleanMissingScenes(worldMap);
            DetectAndAddMissingScenes(worldMap);
        }

        if (GUILayout.Button("➕ Ajouter toutes les maps au Build Settings"))
        {
            AddAllScenesToBuildSettings(worldMap);
        }

        EditorGUILayout.Space();
        DrawMapGrid(worldMap, visibleHalfSize);
        EditorGUILayout.EndScrollView();

        if (pendingNewMapCoord.HasValue)
        {
            Vector2Int coordToCreate = pendingNewMapCoord.Value;
            pendingNewMapCoord = null;

            EditorApplication.delayCall += () =>
            {
                if (worldMap != null)
                {
                    CreateNewSceneAt(coordToCreate, worldMap);
                    center = coordToCreate;
                }
            };
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(worldMap);
        }
    }

    void DrawMapGrid(WorldMap map, int halfSize)
    {
        GUILayout.Label($"Grille centrée sur ({center.x},{center.y}) - Taille : {halfSize * 2 + 1}", EditorStyles.boldLabel);

        for (int y = center.y + halfSize; y >= center.y - halfSize; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = center.x - halfSize; x <= center.x + halfSize; x++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                MapData existing = map.maps.Find(m => m.coords == coord);

#if UNITY_EDITOR
                if (existing != null && existing.sceneAsset != null)
                {
                    string path = AssetDatabase.GetAssetPath(existing.sceneAsset);
                    existing.sceneName = Path.GetFileNameWithoutExtension(path);

                    Regex coordPattern = new Regex(@"([-+]?\d+)_([-+]?\d+)");
                    Match match = coordPattern.Match(existing.sceneName);
                    if (match.Success)
                    {
                        int newX = int.Parse(match.Groups[1].Value);
                        int newY = int.Parse(match.Groups[2].Value);
                        existing.coords = new Vector2Int(newX, newY);
                    }
                }
#endif

                GUI.backgroundColor = existing != null ? Color.green : Color.gray;
                GUILayout.BeginVertical("box");

                string label = existing != null ? $"Map ({x},{y})" : $"+ ({x},{y})";
                string tooltip = existing != null
                    ? $"{existing.sceneName} ({coord.x},{coord.y})"
                    : $"Nouvelle map à créer ({coord.x},{coord.y})";

                bool missingInBuild = existing != null && !SceneIsInBuildSettings(existing.sceneName);
                if (missingInBuild) label = "⚠️ " + label;

                GUIContent buttonContent = new GUIContent(label, tooltip);

                if (GUILayout.Button(buttonContent, GUILayout.Width(90), GUILayout.Height(40)))
                {
                    if (existing != null)
                    {
                        OpenScene(existing.sceneName);
                        center = existing.coords;
                    }
                    else
                    {
                        pendingNewMapCoord = coord;
                    }
                }

                if (existing != null)
                {
                    existing.includeInBuild = EditorGUILayout.ToggleLeft("Inclure dans build", existing.includeInBuild, GUILayout.Width(120));
                    existing.description = EditorGUILayout.TextField("Description", existing.description);

                    string newName = EditorGUILayout.TextField("Renommer", existing.sceneName);
                    if (newName != existing.sceneName && !string.IsNullOrEmpty(newName))
                    {
                        string oldPath = AssetDatabase.GetAssetPath(existing.sceneAsset);
                        string newPath = Path.GetDirectoryName(oldPath) + "/" + newName + ".unity";
                        AssetDatabase.RenameAsset(oldPath, newName);
                        existing.sceneName = newName;
                        existing.sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(newPath);
                    }
                }

                GUILayout.EndVertical();
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndHorizontal();
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
    }

    void OpenScene(string sceneName)
    {
        string[] guids = AssetDatabase.FindAssets(sceneName + " t:Scene");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == sceneName)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(path);
                }
                return;
            }
        }

        Debug.LogWarning("⚠️ Scène introuvable : " + sceneName);
    }

    void CreateNewSceneAt(Vector2Int coords, WorldMap map)
    {
        string sceneName = $"Map_{coords.x}_{coords.y}";
        string folder = "Assets/Scenes/Maps";
        string scenePath = $"{folder}/{sceneName}.unity";

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new GameObject("MapRoot");
        EditorSceneManager.SaveScene(newScene, scenePath);

        MapData newMap = new MapData
        {
            coords = coords,
            sceneName = sceneName,
            includeInBuild = true,
#if UNITY_EDITOR
            sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath)
#endif
        };

        map.maps.Add(newMap);
        EditorUtility.SetDirty(map);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ Nouvelle scène créée : {scenePath}");
    }

    void CleanMissingScenes(WorldMap map)
    {
        for (int i = map.maps.Count - 1; i >= 0; i--)
        {
            var data = map.maps[i];
            string[] guids = AssetDatabase.FindAssets(data.sceneName + " t:Scene");
            bool found = false;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == data.sceneName)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogWarning($"🧹 Scène supprimée détectée : {data.sceneName} — nettoyage.");
                map.maps.RemoveAt(i);
            }
        }

        EditorUtility.SetDirty(map);
    }

    void DetectAndAddMissingScenes(WorldMap map)
    {
        string[] allScenePaths = Directory.GetFiles("Assets/Scenes/Maps", "*.unity", SearchOption.AllDirectories);
        Regex coordPattern = new Regex(@"([-+]?\d+)_([-+]?\d+)", RegexOptions.Compiled);

        foreach (string path in allScenePaths)
        {
            string sceneName = Path.GetFileNameWithoutExtension(path);

            if (map.maps.Exists(m => m.sceneName == sceneName))
                continue;

            Match match = coordPattern.Match(sceneName);
            if (match.Success)
            {
                int x = int.Parse(match.Groups[1].Value);
                int y = int.Parse(match.Groups[2].Value);

                MapData newMap = new MapData
                {
                    coords = new Vector2Int(x, y),
                    sceneName = sceneName,
                    includeInBuild = true,
#if UNITY_EDITOR
                    sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path)
#endif
                };

                map.maps.Add(newMap);
                Debug.Log($"📥 Map importée automatiquement : {sceneName} à ({x},{y})");
            }
        }

        EditorUtility.SetDirty(map);
        AssetDatabase.SaveAssets();
    }
}
