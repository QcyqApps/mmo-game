using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

namespace MmoGame.Editor
{
    public static class BuildScripts
    {
        const string ServerOutputDir = "Build/Server-Linux";
        const string ServerExecutable = "MmoGameServer.x86_64";
        const string AndroidOutputDir = "Build/Client-Android";
        const string AndroidApk = "MmoGame.apk";

        [MenuItem("MmoGame/Build/Linux Server")]
        public static void BuildLinuxServer()
        {
            EnsureDirectory(ServerOutputDir);

            var opts = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                locationPathName = Path.Combine(ServerOutputDir, ServerExecutable),
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opts);
            ReportAndExit(report, "Linux server");
        }

        [MenuItem("MmoGame/Build/Android Client")]
        public static void BuildAndroidClient()
        {
            EnsureDirectory(AndroidOutputDir);

            var opts = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                target = BuildTarget.Android,
                locationPathName = Path.Combine(AndroidOutputDir, AndroidApk),
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(opts);
            ReportAndExit(report, "Android client");
        }

        static string[] GetEnabledScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            var paths = new System.Collections.Generic.List<string>();
            foreach (var s in scenes)
                if (s.enabled) paths.Add(s.path);

            if (paths.Count == 0)
            {
                Debug.LogWarning("[BuildScripts] No scenes in Build Settings. Falling back to SampleScene.");
                paths.Add("Assets/Scenes/SampleScene.unity");
            }
            return paths.ToArray();
        }

        static void EnsureDirectory(string relPath)
        {
            var full = Path.Combine(Directory.GetCurrentDirectory(), relPath);
            if (!Directory.Exists(full)) Directory.CreateDirectory(full);
        }

        static void ReportAndExit(BuildReport report, string label)
        {
            var summary = report.summary;
            Debug.Log($"[BuildScripts] {label}: {summary.result} | size={summary.totalSize}B | duration={summary.totalTime}");
            if (summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
            EditorApplication.Exit(0);
        }
    }
}
