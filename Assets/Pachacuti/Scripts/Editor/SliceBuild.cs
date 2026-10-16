using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Pachacuti.EditorTools
{
    /// <summary>
    /// Builds the slice. The browser build is the pitch deliverable ("browser-playable from a
    /// link", §2); the Windows build is the same project on the PC path.
    ///
    /// Command line (CI, or a machine without the Editor UI open):
    ///   Unity -batchmode -quit -projectPath . -executeMethod Pachacuti.EditorTools.SliceBuild.WebGL [-buildPath Builds/WebGL]
    ///   Unity -batchmode -quit -projectPath . -executeMethod Pachacuti.EditorTools.SliceBuild.Windows
    /// </summary>
    public static class SliceBuild
    {
        /// <summary>Pitch slice §2 hard constraint: total download under 60 MB.</summary>
        public const long DownloadBudgetBytes = 60L * 1024 * 1024;

        [MenuItem("PACHACUTI/Build/WebGL (browser link)", priority = 20)]
        public static void WebGL()
        {
            string path = ArgOr("-buildPath", "Builds/WebGL");
            // Brotli is smallest; the decompression fallback makes it work on hosts that do not send
            // Content-Encoding headers (GitHub Pages, plain static hosting). itch.io and Netlify work either way.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
#if UNITY_2022_1_OR_NEWER // absent from the 2021 reference assemblies the compile check uses
            // A shader variant missing from the build falls back to the nearest one instead of not drawing:
            // the first web build drew every lit surface black for want of one URP Lit variant.
            PlayerSettings.strictShaderVariantMatching = false;
#endif
            Build(BuildTarget.WebGL, BuildTargetGroup.WebGL, path);
        }

        [MenuItem("PACHACUTI/Build/Windows (PC)", priority = 21)]
        public static void Windows()
        {
            string path = ArgOr("-buildPath", "Builds/Windows");
            Build(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, Path.Combine(path, "PACHACUTI.exe"));
        }

        static void Build(BuildTarget target, BuildTargetGroup group, string location)
        {
            ProjectSetup.Validate();
            if (EditorUserBuildSettings.activeBuildTarget != target)
                EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectSetup.BootScene },
                locationPathName = location,
                target = target,
                targetGroup = group,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            string folder = target == BuildTarget.WebGL ? location : Path.GetDirectoryName(location);
            long bytes = FolderSize(folder);
            string verdict = target == BuildTarget.WebGL
                ? (bytes <= DownloadBudgetBytes ? "within" : "OVER") + $" the 60 MB download budget"
                : "PC build";
            Debug.Log($"[PACHACUTI] Build {summary.result} for {target}: {bytes / (1024f * 1024f):0.0} MB at {folder} ({verdict}). " +
                      $"{summary.totalErrors} error(s), {summary.totalWarnings} warning(s), {summary.totalTime.TotalSeconds:0}s.");
            if (Application.isBatchMode && summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        static long FolderSize(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return 0;
            long total = 0;
            foreach (var f in Directory.GetFiles(folder, "*", SearchOption.AllDirectories)) total += new FileInfo(f).Length;
            return total;
        }

        static string ArgOr(string name, string fallback)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return fallback;
        }
    }
}
