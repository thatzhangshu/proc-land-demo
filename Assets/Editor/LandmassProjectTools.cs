using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch entry: -batchmode -projectPath <project> -executeMethod LandmassProjectTools.RunSmokeTest
// Do not add -quit: this check exits after the asynchronous terrain has loaded.
[InitializeOnLoad]
public static class LandmassProjectTools
{
    const string ScenePath = "Assets/Scenes/Scene.unity";
    const string RunningKey = "Landmass.SmokeTest";
    static double started;
    static int errors;
    static string failure;

    static LandmassProjectTools()
    {
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool(RunningKey, false) && EditorApplication.isPlaying)
                BeginMonitoring();
        };
    }

    [MenuItem("Tools/Landmass/Open Demo Scene")]
    public static void OpenDemo()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("Tools/Landmass/Run Smoke Test")]
    public static void RunSmokeTest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before starting the smoke test.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        EditorSceneManager.OpenScene(ScenePath);
        var terrain = UnityEngine.Object.FindObjectOfType<TerrainGenerator>();
        if (terrain == null || terrain.viewer == null || terrain.meshSettings == null ||
            terrain.heightMapSettings == null || terrain.textureSettings == null || terrain.mapMaterial == null ||
            UnityEngine.Object.FindObjectOfType<ThreadedDataRequester>() == null)
            throw new InvalidOperationException("Demo scene has missing terrain references.");
        if (terrain.detailLevels == null || terrain.detailLevels.Length == 0 ||
            terrain.colliderLODIndex < 0 || terrain.colliderLODIndex >= terrain.detailLevels.Length)
            throw new InvalidOperationException("Invalid terrain LOD configuration.");
        SessionState.SetBool(RunningKey, true);
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.isPlaying = true;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(RunningKey, false))
            BeginMonitoring();
    }

    static void BeginMonitoring()
    {
        started = EditorApplication.timeSinceStartup;
        errors = 0;
        failure = null;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            errors++;
            failure = message;
        }
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying) { Finish(false, "Play Mode stopped before verification."); return; }
        double elapsed = EditorApplication.timeSinceStartup - started;
        if (errors > 0) { Finish(false, failure); return; }
        if (elapsed < 15) return;
        var terrain = UnityEngine.Object.FindObjectOfType<TerrainGenerator>();
        var meshes = UnityEngine.Object.FindObjectsOfType<MeshFilter>()
            .Count(filter => filter.name == "Terrain Chunk" && filter.sharedMesh != null);
        var colliders = UnityEngine.Object.FindObjectsOfType<MeshCollider>()
            .Count(collider => collider.name == "Terrain Chunk" && collider.sharedMesh != null);
        if (terrain != null && meshes > 0 && colliders > 0 && Camera.main != null)
        {
            var position = terrain.viewer.position;
            if (Physics.Raycast(new Vector3(position.x, 10000, position.z), Vector3.down, out var hit, 20000) &&
                hit.collider.name == "Terrain Chunk" && position.y >= hit.point.y - 2)
            {
                Finish(true, "Terrain meshes=" + meshes + ", colliders=" + colliders + ", viewer=" + position);
                return;
            }
        }
        if (elapsed > 60) Finish(false, "Timed out waiting for terrain meshes, collision and a viewer above ground.");
    }

    static void Finish(bool success, string message)
    {
        SessionState.SetBool(RunningKey, false);
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        Application.logMessageReceived -= OnLog;
        if (success) Debug.Log("LANDMASS_SMOKE_PASS: " + message);
        else Debug.LogError("LANDMASS_SMOKE_FAIL: " + message);
        EditorApplication.isPlaying = false;
        if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
    }
}
