using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mox
{
#if UNITY_EDITOR
    namespace Editor
    {
        public class EndpointControlls : EditorWindow
        {
            private static string _exeDirectory = "/Build/Windows/";
            private static string _exeName = "MoxUnityEndpoint";
            private static string _exeExtension = ".exe";

            private static Process _endpointProcess = null;

            // [MenuItem("Tools/MozXR/Start Endpoint", false, 0)]
            public static void StartEndpoint()
            {
                StartEndpointIfNotRunning();
            }

            internal static bool IsEndpointRunning()
            {
                RefreshEndpointProcess();
                return _endpointProcess != null;
            }

            internal static void StartEndpointIfNotRunning()
            {
                // return;

                RefreshEndpointProcess();

                if (_endpointProcess == null)
                {
                    string endpointDirectory = EditorConfig.EndpointDirectory + _exeDirectory;

                    string exeName = _exeName + _exeExtension;

                    if (File.Exists(endpointDirectory + exeName))
                    {
                        string moxConfigPath = ResolveAbsolutePath(EditorConfig.MoxConfig);

                        string arguments = "";

                        if (File.Exists(moxConfigPath) == true)
                        {
                            arguments += "moxConfig=\"" + moxConfigPath + "\"";
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning("Mox Config file not found at: '" + moxConfigPath + "'. Starting Endpoint without moxConfig argument.");
                        }

                        _endpointProcess = EditorProcessUtility.LaunchProcess(endpointDirectory, exeName, arguments);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("Endpoint executable not found at: " + endpointDirectory + exeName);
                        EditorUtility.DisplayDialog("Endpoint Executable not found.", "The endpoint executable could not be found at the specified location: " + EditorConfig.EndpointDirectory + "\nCheck that you have the Endpoint binaries downloaded and the MozXR configuration is correct.", "Ok");
                    }
                }
                else
                {
                    UnityEngine.Debug.Log("Endpoint is already running.");
                }
            }

            private static void RefreshEndpointProcess()
            {
                UnityEngine.Debug.Log("Refresh Endpoint Process");

                // if(_endpointProcess == null)
                {
                    _endpointProcess = FindRunningEndpointProcess();
                }

                if (_endpointProcess != null)
                {
                    _endpointProcess.Refresh();
                    if (_endpointProcess.HasExited)
                    {
                        _endpointProcess = null;
                    }
                }
            }

            private static Process FindRunningEndpointProcess()
            {
                UnityEngine.Debug.Log("Find Running Endpoint Process    ");

                Process result = null;

                Process[] processes = Process.GetProcessesByName(_exeName);
                if (processes.Length > 0)
                {
                    result = processes[0];
                }

                return result;
            }

            private static string ResolveAbsolutePath(string configuredPath)
            {
                if (string.IsNullOrEmpty(configuredPath))
                {
                    return "";
                }

                if (Path.IsPathRooted(configuredPath))
                {
                    return Path.GetFullPath(configuredPath);
                }

                string projectDirectory = Path.GetDirectoryName(Application.dataPath);

                return Path.GetFullPath(Path.Combine(projectDirectory, configuredPath));
            }
        }
    }
#endif
}


