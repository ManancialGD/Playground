#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using Debug = UnityEngine.Debug;

namespace EditorTools
{
    public class BuildTools
    {
        private static readonly string buildsPath = "Builds";
        private static readonly string windowsBuildPath = Path.Combine(buildsPath, "Windows");
        private static readonly string gameName = "Playground";
        private static readonly string exeName = gameName + ".exe";
        private static readonly string windowsGamePath = Path.Combine(windowsBuildPath, exeName);

        private static void Run(string path, string args)
        {
            // Start a new process​
            Process process = new Process();

            // Configure the process using the StartInfo properties​
            process.StartInfo.FileName = path;
            process.StartInfo.Arguments = args;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal; // Choose the window style: Hidden, Minimized, Maximized, Normal​
            process.StartInfo.RedirectStandardOutput = false; // Set to true to redirect the output (so you can read it in Unity)​
            process.StartInfo.UseShellExecute = true; // Set to false if you want to redirect the output​

            // Run the process​
            process.Start();
        }

        [MenuItem("Build Tools/Build Windows (x64) _F10", priority = 0)]
        public static bool BuildGameWindows()
        {
            BuildPlayerOptions buildPlayerOptions = new()
            {
                scenes = EditorBuildSettings.scenes
                    .Where(s => s.enabled)
                    .Select(s => s.path)
                    .ToArray(),

                locationPathName = windowsGamePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            // Perform the build
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);

            // Output the result of the build​
            Debug.Log($"Build ended with status: {report.summary.result}");

            // Additional log on the build, looking at report.summary​
            return report.summary.result == BuildResult.Succeeded;
        }

        [MenuItem("Build Tools/Build and Launch) _F11", priority = 10)]
        public static void BuildAndLaunch1()
        {
            CloseAll();
            if (BuildGameWindows())
            {
                Launch();
            }
        }

        [MenuItem("Build Tools/Launch _F12", priority = 30)]
        public static void Launch()
        {
            Run(windowsGamePath, "");
        }

        [MenuItem("Build Tools/Close All", priority = 100)]
        public static void CloseAll()
        {
            // Get all processes with the specified name​
            Process[] processes = Process.GetProcessesByName(gameName);
            foreach (var process in processes)
            {
                try
                {
                    // Close the process​
                    process.Kill();
                    // Wait for the process to exit​
                    process.WaitForExit();
                }
                catch (Exception ex)
                {
                    // Handle exceptions, if any​
                    // This could occur if the process has already exited or you don't have permission to kill it​
                    Debug.LogWarning($"Error trying to kill process {process.ProcessName}: {ex.Message}");
                }
            }
        }

    }
}
#endif