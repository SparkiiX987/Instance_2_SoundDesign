using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Called by the Dockerfile / GitHub Actions via:
///   -executeMethod BuildScript.PerformBuild
/// Environment variables (set by docker --build-arg or CI):
///   CUSTOM_BUILD_PATH   – output directory  (default: /build)
///   CUSTOM_BUILD_TARGET – platform string   (default: StandaloneLinux64)
/// </summary>
public static class BuildScript
{
    public static void PerformBuild()
    {
        // ── Resolve target platform ──────────────────────────────────────
        string targetEnv = GetArg("-customBuildTarget")
                           ?? Environment.GetEnvironmentVariable("BUILD_TARGET")
                           ?? "StandaloneLinux64";

        if (!Enum.TryParse(targetEnv, out BuildTarget target))
        {
            Debug.LogError($"[BuildScript] Unknown build target: {targetEnv}");
            EditorApplication.Exit(1);
            return;
        }

        // ── Resolve output path ──────────────────────────────────────────
        string buildPath = GetArg("-customBuildPath")
                           ?? Environment.GetEnvironmentVariable("BUILD_PATH")
                           ?? "/build";

        string executableName = target == BuildTarget.StandaloneWindows64
            ? "Instance_2_SoundDesign.exe"
            : "Instance_2_SoundDesign";

        string outputPath = $"{buildPath}/{executableName}";

        // ── Collect scenes from EditorBuildSettings ──────────────────────
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[BuildScript] No enabled scenes found in Build Settings.");
            EditorApplication.Exit(1);
            return;
        }

        Debug.Log($"[BuildScript] Building {target} → {outputPath}");
        Debug.Log($"[BuildScript] Scenes: {string.Join(", ", scenes)}");

        // ── Run the build ────────────────────────────────────────────────
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes           = scenes,
            locationPathName = outputPath,
            target           = target,
            options          = BuildOptions.None,
        });

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] Build succeeded. Size: {report.summary.totalSize} bytes");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"[BuildScript] Build FAILED: {report.summary.result}");
            EditorApplication.Exit(1);
        }
    }

    // ── Helper: read a named argument from the command line ──────────────
    private static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }
}

