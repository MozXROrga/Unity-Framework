using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

namespace Mox
{
#if UNITY_EDITOR
    namespace Editor
    {
        public class DeploymentControlls : EditorWindow
        {
            [System.Serializable]
            private struct ProjectConfig
            {
                public string startMap;
                public string version;
                public string type;
                public string executable;
                public string nDisplay;
            }

            [System.Serializable]
            private struct UnityConfig
            {
                public string executablePath;
            }

            private static string _projectConfigRelativePath = "/project.json";
            private static string _unityConfigRelativePath = "/Build/Windows/MoxUnityEndpoint/Content/UnityConfig/unity.json";

            private static string _unityExeRelativeDirectory = "../../../../../Unity/";


            // [MenuItem("Tools/MozXR/Deployment/Build", false, 3)]
            public static void Deploy()
            {
                string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");

                if (System.IO.Directory.Exists(path) == false)
                {
                    UnityEngine.Debug.LogError("Build failed, target directory does not exists.");
                    EditorUtility.DisplayDialog("Build Failed", "Target directory does not exists.", "Ok");

                    return;
                }

                string buildDirectoryName = PlayerSettings.productName + "_" + System.DateTime.Now.ToString("yyMMdd_HHmmss");
                string buildDirectoryPath = System.IO.Path.Combine(path, buildDirectoryName);
                System.IO.Directory.CreateDirectory(buildDirectoryPath);

                if (CopyEndpoint(buildDirectoryPath))
                {
                    if (Build(buildDirectoryPath + "/Unity/") == true)
                    {
                        EditorUtility.RevealInFinder(buildDirectoryPath);
                    }
                }
            }

            private static bool Build(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    UnityEngine.Debug.LogError("No build path given");
                    return false;
                }

                BuildTarget target = BuildTarget.StandaloneWindows64;
                BuildTargetGroup targetGroup = BuildTargetGroup.Standalone;

                if (EditorUserBuildSettings.activeBuildTarget != target)
                {
                    bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);

                    if (switched == false)
                    {
                        UnityEngine.Debug.LogError("Failed to switch active build target to StandaloneWindows64.");
                        return false;
                    }
                }

                EnforceMonoBackend();

                EditorBuildSettingsScene[] allScenes = EditorBuildSettings.scenes;
                List<string> enabledScenes = new List<string>();

                for (int i = 0; i < allScenes.Length; i++)
                {
                    if (allScenes[i].enabled)
                    {
                        enabledScenes.Add(allScenes[i].path);
                    }
                }

                if (enabledScenes.Count == 0)
                {
                    UnityEngine.Debug.LogError("No enabled scenes found in Build Settings.");
                    EditorUtility.DisplayDialog("Build Failed", "No enabled scenes found in Build Settings.", "Ok");
                    return false;
                }

                BuildPlayerOptions options = new BuildPlayerOptions();
                options.scenes = enabledScenes.ToArray();
                options.locationPathName = System.IO.Path.Combine(path, GetProjectExeName());
                options.target = target;
                options.options = BuildOptions.None;

                BuildReport report = BuildPipeline.BuildPlayer(options);

                if (report.summary.result == BuildResult.Succeeded)
                {
                    UnityEngine.Debug.Log("Build succeeded: " + options.locationPathName);

                    return true;
                }
                else
                {
                    UnityEngine.Debug.LogError("Build failed with result: " + report.summary.result);
                    EditorUtility.DisplayDialog("Build Failed", "Build Failed. Check console log for details.", "Ok");

                    return false;
                }
            }

            private static void EnforceMonoBackend()
            {
                NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));

                ScriptingImplementation currentBackend = PlayerSettings.GetScriptingBackend(namedBuildTarget);

                if (currentBackend != ScriptingImplementation.Mono2x)
                {
                    PlayerSettings.SetScriptingBackend(namedBuildTarget, ScriptingImplementation.Mono2x);

                    UnityEngine.Debug.Log("IL2CPP is not supported by MozXR Framework. Scripting Backend was changed to Mono.");

                    EditorUtility.DisplayDialog("Enforcing Mono Backend", "IL2CPP is not supported by MozXR Framework. Scripting Backend was changed to Mono.", "Ok");
                }
            }

            private static string GetProjectExeName()
            {
                string projectName = PlayerSettings.productName;

                if (string.IsNullOrEmpty(projectName))
                {
                    projectName = "MozXRBuild";
                }

                char[] invalidChars = System.IO.Path.GetInvalidFileNameChars();

                if (invalidChars.Length > 0)
                {
                    UnityEngine.Debug.Log("Invalid characters in output file name, replacing with '_'");

                    for (int i = 0; i < invalidChars.Length; i++)
                    {
                        projectName = projectName.Replace(invalidChars[i], '_');
                    }
                }

                return projectName + ".exe";
            }

            private static bool CopyEndpoint(string buildPath)
            {
                string endpointDirectoryConfig = EditorConfig.EndpointDirectory;
                string projectDirectory = System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath);

                string endpointSourcePath = "";

                if (System.IO.Path.IsPathRooted(endpointDirectoryConfig))
                {
                    endpointSourcePath = endpointDirectoryConfig;
                }
                else
                {
                    endpointSourcePath = System.IO.Path.Combine(projectDirectory, endpointDirectoryConfig);
                }

                endpointSourcePath = System.IO.Path.GetFullPath(endpointSourcePath);

                string endpointDestPath = System.IO.Path.Combine(buildPath);

                if (System.IO.Directory.Exists(endpointSourcePath) == false)
                {
                    UnityEngine.Debug.LogError("Endpoint directory not found at: " + endpointSourcePath);
                    EditorUtility.DisplayDialog("Copy Endpoint Failed", "Endpoint directory not found at:\n" + endpointSourcePath, "Ok");

                    return false;
                }

                try
                {
                    if (System.IO.Directory.Exists(endpointDestPath))
                    {
                        System.IO.Directory.Delete(endpointDestPath, true);
                    }

                    CopyDirectory(endpointSourcePath, endpointDestPath);

                    UnityEngine.Debug.Log("Endpoint copied to: " + endpointDestPath);
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError("Failed to copy Endpoint: " + e.Message);
                    EditorUtility.DisplayDialog("Copy Endpoint Failed", "Error: " + e.Message, "Ok");

                    return false;
                }

                UpdateProjectConfig(buildPath + _projectConfigRelativePath);

                UpdateUnityConfig(buildPath + _unityConfigRelativePath);

                return true;
            }

            private static void CopyDirectory(string sourceDir, string destDir)
            {
                System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(sourceDir);

                if (dir.Exists == false)
                {
                    throw new System.IO.DirectoryNotFoundException("Source directory not found: " + sourceDir);
                }

                System.IO.DirectoryInfo[] subDirs = dir.GetDirectories();

                System.IO.Directory.CreateDirectory(destDir);

                System.IO.FileInfo[] files = dir.GetFiles();
                for (int i = 0; i < files.Length; i++)
                {
                    string targetFilePath = System.IO.Path.Combine(destDir, files[i].Name);
                    files[i].CopyTo(targetFilePath, true);
                }

                for (int i = 0; i < subDirs.Length; i++)
                {
                    string targetSubDir = System.IO.Path.Combine(destDir, subDirs[i].Name);
                    CopyDirectory(subDirs[i].FullName, targetSubDir);
                }
            }

            private static bool UpdateProjectConfig(string configPath)
            {
                ProjectConfig config;

                if (LoadJsonConfig<ProjectConfig>(configPath, out config) == false)
                {
                    return false;
                }

                config.type = "unity";



                config.nDisplay = EditorConfig.NDisplayConfig;

                try
                {
                    string updatedJson = UnityEngine.JsonUtility.ToJson(config, true);
                    System.IO.File.WriteAllText(configPath, updatedJson);
                    UnityEngine.Debug.Log("Project config updated.");
                    return true;
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError("UpdateProjectConfig failed to write: " + e.Message);
                    return false;
                }
            }

            private static bool UpdateUnityConfig(string configPath)
            {
                UnityConfig config;

                if (LoadJsonConfig<UnityConfig>(configPath, out config) == false)
                {
                    return false;
                }

                // Update config fields here
                config.executablePath = _unityExeRelativeDirectory + GetProjectExeName();

                try
                {
                    string updatedJson = UnityEngine.JsonUtility.ToJson(config, true);
                    System.IO.File.WriteAllText(configPath, updatedJson);
                    UnityEngine.Debug.Log("Unity config updated.");
                    return true;
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError("UpdateUnityConfig failed to write: " + e.Message);
                    return false;
                }
            }

            private static bool LoadJsonConfig<T>(string configPath, out T config) where T : struct
            {
                config = default(T);

                if (string.IsNullOrEmpty(configPath))
                {
                    UnityEngine.Debug.LogError("LoadJsonConfig: config path is null or empty.");
                    return false;
                }

                if (System.IO.File.Exists(configPath) == false)
                {
                    UnityEngine.Debug.LogError("LoadJsonConfig: config file not found at: " + configPath);
                    return false;
                }

                try
                {
                    string configJson = System.IO.File.ReadAllText(configPath);
                    config = UnityEngine.JsonUtility.FromJson<T>(configJson);
                    return true;
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError("LoadJsonConfig failed: " + e.Message);
                    return false;
                }
            }
        }
    }
#endif
}
