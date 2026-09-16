#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

/// <summary>
/// Meldet Aktivitaet im Unity-Editor an WakaTime.
/// Ruft dazu die offizielle wakatime-cli auf, die ihren API-Key aus
/// ~/.wakatime.cfg liest - es wird also kein zweiter Key im Projekt gespeichert.
/// Offline-Queue und Git-Branch-Erkennung uebernimmt damit ebenfalls die CLI.
/// </summary>
[InitializeOnLoad]
public static class WakaTimeUnity
{
    private const string EnabledPref = "WakaTime/Unity/Enabled";
    private const string DebugPref = "WakaTime/Unity/Debug";
    private const string CliPathPref = "WakaTime/Unity/CliPath";

    private const string PluginVersion = "1.0.0";

    /// <summary>Mindestabstand zwischen zwei Heartbeats (WakaTime-Standard).</summary>
    private const double CooldownSeconds = 120.0;

    private static DateTime lastHeartbeatUtc = DateTime.MinValue;
    private static string lastEntity = string.Empty;

    static WakaTimeUnity()
    {
        // Verzoegert, damit nach einem Domain-Reload erst der Editor fertig laedt.
        EditorApplication.delayCall += Initialize;
    }

    private static bool Enabled
    {
        get { return EditorPrefs.GetBool(EnabledPref, true); }
        set { EditorPrefs.SetBool(EnabledPref, value); }
    }

    private static bool DebugLogs
    {
        get { return EditorPrefs.GetBool(DebugPref, false); }
        set { EditorPrefs.SetBool(DebugPref, value); }
    }

    private static void Initialize()
    {
        UnlinkCallbacks();

        if (!Enabled)
        {
            return;
        }

        if (FindCli() == null)
        {
            Debug.LogWarning(
                "<WakaTime> wakatime-cli nicht gefunden. Einmal die WakaTime-Extension in VS Code " +
                "oder Rider starten (die laedt die CLI nach ~/.wakatime/), oder den Pfad unter " +
                "Tools > WakaTime > CLI-Pfad setzen... eintragen.");
            return;
        }

        LinkCallbacks();
        SendHeartbeat();
    }

    // --- Events -----------------------------------------------------------

    private static void LinkCallbacks()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.hierarchyChanged += OnEditorActivity;
        EditorApplication.projectChanged += OnEditorActivity;
        Selection.selectionChanged += OnEditorActivity;
        EditorSceneManager.sceneSaved += OnSceneSaved;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorSceneManager.newSceneCreated += OnSceneCreated;
    }

    private static void UnlinkCallbacks()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.hierarchyChanged -= OnEditorActivity;
        EditorApplication.projectChanged -= OnEditorActivity;
        Selection.selectionChanged -= OnEditorActivity;
        EditorSceneManager.sceneSaved -= OnSceneSaved;
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.newSceneCreated -= OnSceneCreated;
    }

    private static void OnEditorActivity()
    {
        SendHeartbeat();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        SendHeartbeat();
    }

    private static void OnSceneSaved(Scene scene)
    {
        // Speichern ist ein Write-Heartbeat und umgeht den Cooldown.
        SendHeartbeat(true);
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        SendHeartbeat();
    }

    private static void OnSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
    {
        SendHeartbeat();
    }

    // --- Heartbeat --------------------------------------------------------

    private static void SendHeartbeat(bool isWrite = false)
    {
        if (!Enabled)
        {
            return;
        }

        string entity = GetCurrentEntity();
        DateTime now = DateTime.UtcNow;

        bool cooling = (now - lastHeartbeatUtc).TotalSeconds < CooldownSeconds;
        if (cooling && !isWrite && entity == lastEntity)
        {
            return;
        }

        string cli = FindCli();
        if (cli == null)
        {
            return;
        }

        lastHeartbeatUtc = now;
        lastEntity = entity;

        // --alternate-project greift nur, wenn die CLI kein Git-Repo findet.
        string args =
            "--entity " + Quote(entity) +
            " --entity-type file" +
            " --plugin " + Quote("unity/" + Application.unityVersion + " unity-wakatime/" + PluginVersion) +
            " --alternate-project " + Quote(Application.productName) +
            // .unity/.prefab erkennt die CLI nicht selbst; "Unity3D Asset" ist
            // der Name, den WakaTime dafuer kennt ("Unity" wird abgelehnt).
            " --alternate-language " + Quote("Unity3D Asset") +
            " --category " + (EditorApplication.isPlaying ? "debugging" : "coding") +
            (isWrite ? " --write" : string.Empty);

        Run(cli, args);
    }

    /// <summary>
    /// Die Datei, an der gerade gearbeitet wird: offener Prefab-Modus vor
    /// aktiver Szene, sonst der Projektordner als Rueckfallebene.
    /// </summary>
    private static string GetCurrentEntity()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;

        var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null && !string.IsNullOrEmpty(prefabStage.assetPath))
        {
            return Path.GetFullPath(Path.Combine(projectRoot, prefabStage.assetPath));
        }

        string scenePath = EditorSceneManager.GetActiveScene().path;
        if (!string.IsNullOrEmpty(scenePath))
        {
            return Path.GetFullPath(Path.Combine(projectRoot, scenePath));
        }

        return projectRoot;
    }

    private static void Run(string cli, string args)
    {
        try
        {
            var info = new ProcessStartInfo(cli, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = new Process { StartInfo = info, EnableRaisingEvents = true };
            process.Exited += (sender, e) =>
            {
                var finished = (Process)sender;
                if (DebugLogs)
                {
                    string stderr = finished.StandardError.ReadToEnd();
                    Debug.Log("<WakaTime> exit " + finished.ExitCode +
                              (string.IsNullOrEmpty(stderr) ? string.Empty : "\n" + stderr));
                }
                finished.Dispose();
            };

            process.Start();

            if (DebugLogs)
            {
                Debug.Log("<WakaTime> " + cli + " " + args);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("<WakaTime> Heartbeat fehlgeschlagen: " + e.Message);
        }
    }

    // --- CLI finden -------------------------------------------------------

    private static string FindCli()
    {
        string custom = EditorPrefs.GetString(CliPathPref, string.Empty);
        if (!string.IsNullOrEmpty(custom) && File.Exists(custom))
        {
            return custom;
        }

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home))
        {
            return null;
        }

        string dir = Path.Combine(home, ".wakatime");
        if (!Directory.Exists(dir))
        {
            return null;
        }

        // Die CLI heisst je nach Plattform und Architektur anders, z. B.
        // wakatime-cli-windows-amd64.exe oder wakatime-cli-darwin-arm64.
        foreach (string candidate in Directory.GetFiles(dir, "wakatime-cli*"))
        {
            string name = Path.GetFileName(candidate);
            if (name.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".bdb", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    private static string Quote(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    // --- Menue ------------------------------------------------------------

    private const string EnabledMenu = "Tools/WakaTime/Tracking aktiviert";
    private const string DebugMenu = "Tools/WakaTime/Debug-Logs";

    [MenuItem(EnabledMenu, false, 0)]
    private static void ToggleEnabled()
    {
        Enabled = !Enabled;
        Initialize();
    }

    [MenuItem(EnabledMenu, true)]
    private static bool ToggleEnabledValidate()
    {
        Menu.SetChecked(EnabledMenu, Enabled);
        return true;
    }

    [MenuItem(DebugMenu, false, 1)]
    private static void ToggleDebug()
    {
        DebugLogs = !DebugLogs;
    }

    [MenuItem(DebugMenu, true)]
    private static bool ToggleDebugValidate()
    {
        Menu.SetChecked(DebugMenu, DebugLogs);
        return true;
    }

    [MenuItem("Tools/WakaTime/Status anzeigen", false, 20)]
    private static void ShowStatus()
    {
        string cli = FindCli();
        Debug.Log(
            "<WakaTime> Tracking: " + (Enabled ? "an" : "aus") +
            "\nCLI: " + (cli ?? "nicht gefunden") +
            "\nEntity: " + GetCurrentEntity() +
            "\nLetzter Heartbeat: " +
            (lastHeartbeatUtc == DateTime.MinValue
                ? "noch keiner"
                : lastHeartbeatUtc.ToLocalTime().ToString("HH:mm:ss")));
    }

    [MenuItem("Tools/WakaTime/CLI-Pfad setzen...", false, 21)]
    private static void SetCliPath()
    {
        string picked = EditorUtility.OpenFilePanel("wakatime-cli auswaehlen", "", "");
        if (!string.IsNullOrEmpty(picked))
        {
            EditorPrefs.SetString(CliPathPref, picked);
            Initialize();
        }
    }
}
#endif
