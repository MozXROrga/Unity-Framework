using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mox
{
#if UNITY_EDITOR
    namespace Editor
    {
        public class TrackingControlls : EditorWindow
        {
            private Process _processPharusUnicastFloor = null;
            private Process _processPharusUnicastWall = null;

            private Process _processPharusMulticastFloor = null;
            private Process _processPharusMulticastWall = null;


            private Process _processTracksimUnicastFloor = null;
            private Process _processTracksimUnicastWall = null;

            private Process _processTracksimMulticastFloor = null;
            private Process _processTracksimMulticastWall = null;



            private Button _buttonPharusRecUniAll = null;
            private Button _buttonPharusRecUniFloor = null;
            private Button _buttonPharusRecUniWall = null;

            private Button _buttonPharusRecMultiAll = null;
            private Button _buttonPharusRecMultiFloor = null;
            private Button _buttonPharusRecMultiWall = null;


            private Button _buttonPharusSimUniAll = null;
            private Button _buttonPharusSimUniFloor = null;
            private Button _buttonPharusSimUniWall = null;

            private Button _buttonPharusSimMultiAll = null;
            private Button _buttonPharusSimMultiFloor = null;
            private Button _buttonPharusSimMultiWall = null;


            // private string _pharusBaseDirectory = "D:\\DeepSpace\\Misc\\Pharus\\";
            // string _pharusRecordingName = "limelight1.rec";

            // [MenuItem("Tools/MozXR/Tracking", false, 2)]
            public static void ShowExample()
            {
                TrackingControlls wnd = GetWindow<TrackingControlls>();
                wnd.titleContent = new GUIContent("Tracking Simulators");
            }

            public void CreateGUI()
            {
                // _pharusBaseDirectory = MoxEditorConfig.PharusBaseDirectory;

                // Each editor window contains a root VisualElement object
                VisualElement root = rootVisualElement;

                VisualElement pharusRoot = new VisualElement();
                root.Add(pharusRoot);

                // VisualElements objects can contain other VisualElement following a tree hierarchy.
                Label labelPharusRecordingHeader = new Label("<b>Pharus (Recording)</b>");
                pharusRoot.Add(labelPharusRecordingHeader);

                Label labelPharusUnicast = new Label("Unicast");
                pharusRoot.Add(labelPharusUnicast);

                _buttonPharusRecUniAll = new Button();
                _buttonPharusRecUniAll.text = "Start All";
                _buttonPharusRecUniAll.clicked += OnStartPharusUnicastAll;
                pharusRoot.Add(_buttonPharusRecUniAll);

                _buttonPharusRecUniFloor = new Button();
                _buttonPharusRecUniFloor.text = "Start Floor";
                _buttonPharusRecUniFloor.clicked += OnStartPharusUnicastFloor;
                pharusRoot.Add(_buttonPharusRecUniFloor);

                _buttonPharusRecUniWall = new Button();
                _buttonPharusRecUniWall.text = "Start Wall";
                _buttonPharusRecUniWall.clicked += OnStartPharusUnicastWall;
                pharusRoot.Add(_buttonPharusRecUniWall);



                Label labelPharusMulticast = new Label("Multicast");
                pharusRoot.Add(labelPharusMulticast);

                _buttonPharusRecMultiAll = new Button();
                _buttonPharusRecMultiAll.text = "Start All";
                _buttonPharusRecMultiAll.clicked += OnStartPharusMulticastAll;
                pharusRoot.Add(_buttonPharusRecMultiAll);

                _buttonPharusRecMultiFloor = new Button();
                _buttonPharusRecMultiFloor.text = "Start Floor";
                _buttonPharusRecMultiFloor.clicked += OnStartPharusMulticastFloor;
                pharusRoot.Add(_buttonPharusRecMultiFloor);

                _buttonPharusRecMultiWall = new Button();
                _buttonPharusRecMultiWall.text = "Start Wall";
                _buttonPharusRecMultiWall.clicked += OnStartPharusMulticastWall;
                pharusRoot.Add(_buttonPharusRecMultiWall);


                Label labelPharusSimulatorHeader = new Label("<b>Pharus (Simulator)</b>");
                pharusRoot.Add(labelPharusSimulatorHeader);

                Label labelPharusSimulatorUnicast = new Label("Unicast");
                pharusRoot.Add(labelPharusSimulatorUnicast);

                _buttonPharusSimUniAll = new Button();
                _buttonPharusSimUniAll.text = "Start All";
                _buttonPharusSimUniAll.clicked += OnStartTracksimUnicastAll;
                pharusRoot.Add(_buttonPharusSimUniAll);

                _buttonPharusSimUniFloor = new Button();
                _buttonPharusSimUniFloor.text = "Start Floor";
                _buttonPharusSimUniFloor.clicked += OnStartTracksimUnicastFloor;
                pharusRoot.Add(_buttonPharusSimUniFloor);

                _buttonPharusSimUniWall = new Button();
                _buttonPharusSimUniWall.text = "Start Floor";
                _buttonPharusSimUniWall.clicked += OnStartTracksimUnicastWall;
                pharusRoot.Add(_buttonPharusSimUniWall);


                Label labelPharusSimulatorMulticast = new Label("Multicast");
                pharusRoot.Add(labelPharusSimulatorMulticast);

                _buttonPharusSimMultiAll = new Button();
                _buttonPharusSimMultiAll.text = "Start All";
                _buttonPharusSimMultiAll.clicked += OnStartTracksimMulticastAll;
                pharusRoot.Add(_buttonPharusSimMultiAll);

                _buttonPharusSimMultiFloor = new Button();
                _buttonPharusSimMultiFloor.text = "Start Floor";
                _buttonPharusSimMultiFloor.clicked += OnStartTracksimMulticastFloor;
                pharusRoot.Add(_buttonPharusSimMultiFloor);

                _buttonPharusSimMultiWall = new Button();
                _buttonPharusSimMultiWall.text = "Start Floor";
                _buttonPharusSimMultiWall.clicked += OnStartTracksimMulticastWall;
                pharusRoot.Add(_buttonPharusSimMultiWall);

                // tab = GUILayout.Toolbar(tab, new string[] { "Object", "Bake", "Layers" });
            }

            public void Update()
            {
                UpdatePharusRecUnicastButtons();
                UpdatePharusRecMulticastButtons();

                UpdatePharusSimUnicastButtons();
                UpdatePharusSimMulticastButtons();
            }

            private void OnStartPharusUnicastAll()
            {
                if ((_processPharusUnicastFloor == null || _processPharusUnicastFloor.HasExited)
                    || (_processPharusUnicastWall == null || _processPharusUnicastWall.HasExited))
                {
                    StartPharusUnicastFloor();
                    StartPharusUnicastWall();
                }
                else if (_processPharusUnicastFloor != null
                    && _processPharusUnicastWall != null)
                {
                    _processPharusUnicastFloor.Kill();
                    _processPharusUnicastWall.Kill();
                }
            }

            private void OnStartPharusUnicastFloor()
            {
                if (_processPharusUnicastFloor == null || _processPharusUnicastFloor.HasExited)
                {
                    StartPharusUnicastFloor();
                }
                else if (_processPharusUnicastFloor != null)
                {
                    _processPharusUnicastFloor.Kill();
                }
            }

            private void OnStartPharusUnicastWall()
            {
                if (_processPharusUnicastWall == null || _processPharusUnicastWall.HasExited)
                {
                    StartPharusUnicastWall();
                }
                else if (_processPharusUnicastWall != null)
                {
                    _processPharusUnicastWall.Kill();
                }
            }

            private void OnStartPharusMulticastAll()
            {
                if ((_processPharusMulticastFloor == null || _processPharusMulticastFloor.HasExited)
                    || (_processPharusMulticastWall == null || _processPharusMulticastWall.HasExited))
                {
                    StartPharusMulticastFloor();
                    StartPharusMulticastWall();
                }
                else if (_processPharusMulticastFloor != null
                    && _processPharusMulticastWall != null)
                {
                    _processPharusMulticastFloor.Kill();
                    _processPharusMulticastWall.Kill();
                }
            }

            private void OnStartPharusMulticastFloor()
            {
                if (_processPharusMulticastFloor == null || _processPharusMulticastFloor.HasExited)
                {
                    StartPharusMulticastFloor();
                }
                else if (_processPharusMulticastFloor != null)
                {
                    _processPharusMulticastFloor.Kill();
                }
            }

            private void OnStartPharusMulticastWall()
            {
                if (_processPharusMulticastWall == null || _processPharusMulticastWall.HasExited)
                {
                    StartPharusMulticastWall();
                }
                else if (_processPharusMulticastWall != null)
                {
                    _processPharusMulticastWall.Kill();
                }
            }


            private void OnStartTracksimUnicastAll()
            {
                if ((_processTracksimUnicastFloor == null || _processTracksimUnicastFloor.HasExited)
                    || (_processTracksimUnicastWall == null || _processTracksimUnicastWall.HasExited))
                {
                    OnStartTracksimUnicastFloor();
                    OnStartTracksimUnicastWall();
                }
                else if (_processTracksimUnicastFloor != null
                    && _processTracksimUnicastWall != null)
                {
                    _processTracksimUnicastFloor.Kill();
                    _processTracksimUnicastWall.Kill();
                }
            }

            private void OnStartTracksimUnicastFloor()
            {
                if (_processTracksimUnicastFloor == null || _processTracksimUnicastFloor.HasExited)
                {
                    StartPharusTracklinkSimUnicastFloor();
                }
                else if (_processTracksimUnicastFloor != null)
                {
                    _processTracksimUnicastFloor.Kill();
                }
            }

            private void OnStartTracksimUnicastWall()
            {
                if (_processTracksimUnicastWall == null || _processTracksimUnicastWall.HasExited)
                {
                    StartPharusTracklinkSimUnicastWall();
                }
                else if (_processTracksimUnicastWall != null)
                {
                    _processTracksimUnicastWall.Kill();
                }
            }

            private void OnStartTracksimMulticastAll()
            {
                if ((_processTracksimMulticastFloor == null || _processTracksimMulticastFloor.HasExited)
                    || (_processTracksimMulticastWall == null || _processTracksimMulticastWall.HasExited))
                {
                    OnStartTracksimMulticastFloor();
                    OnStartTracksimMulticastWall();
                }
                else if (_processTracksimMulticastFloor != null
                    && _processTracksimMulticastWall != null)
                {
                    _processTracksimMulticastFloor.Kill();
                    _processTracksimMulticastWall.Kill();
                }
            }

            private void OnStartTracksimMulticastFloor()
            {
                if (_processTracksimMulticastFloor == null || _processTracksimMulticastFloor.HasExited)
                {
                    StartPharusTracklinkSimMulticastFloor();
                }
                else if (_processTracksimMulticastFloor != null)
                {
                    _processTracksimMulticastFloor.Kill();
                }
            }

            private void OnStartTracksimMulticastWall()
            {
                if (_processTracksimMulticastWall == null || _processTracksimMulticastWall.HasExited)
                {
                    StartPharusTracklinkSimMulticastWall();
                }
                else if (_processTracksimMulticastWall != null)
                {
                    _processTracksimMulticastWall.Kill();
                }
            }


            private void StartPharusUnicastFloor()
            {
                _processPharusUnicastFloor = StartPharusRecording("pharus-rec-sim-v2.4.0-s0-release_UniCast_Floor\\bin", EditorConfig.PharusRecording);
            }

            private void StartPharusUnicastWall()
            {
                _processPharusUnicastWall = StartPharusRecording("pharus-rec-sim-v2.4.0-s0-release_UniCast_Wall\\bin", EditorConfig.PharusRecording);
            }

            private void StartPharusMulticastFloor()
            {
                _processPharusMulticastFloor = StartPharusRecording("pharus-rec-sim-v2.4.0-s0-release_MultiCast_Floor\\bin", EditorConfig.PharusRecording);
            }

            private void StartPharusMulticastWall()
            {
                _processPharusMulticastWall = StartPharusRecording("pharus-rec-sim-v2.4.0-s0-release_MultiCast_Wall\\bin", EditorConfig.PharusRecording);
            }

            private void StartPharusTracklinkSimUnicastFloor()
            {
                _processTracksimUnicastFloor = StartTracklinkSimulator("TrackLinkSimulator_UniCast_Floor\\bin");
            }

            private void StartPharusTracklinkSimUnicastWall()
            {
                _processTracksimUnicastWall = StartTracklinkSimulator("TrackLinkSimulator_UniCast_Wall\\bin");
            }

            private void StartPharusTracklinkSimMulticastFloor()
            {
                _processTracksimMulticastFloor = StartTracklinkSimulator("TrackLinkSimulator_MulrtiCast_Floor\\bin");
            }

            private void StartPharusTracklinkSimMulticastWall()
            {
                _processTracksimMulticastWall = StartTracklinkSimulator("TrackLinkSimulator_MultiCast_Wall\\bin");
            }

            private Process StartPharusRecording(string pharusDirectory, string recordingName)
            {
                return StartPharus(pharusDirectory, "pharus.exe", recordingName);

                //Process result = null;

                //ProcessStartInfo startInfo = new ProcessStartInfo();
                //startInfo.FileName = "pharus.exe";
                //startInfo.WorkingDirectory = MoxEditorConfig.PharusBaseDirectory + pharusDirectory;
                //startInfo.Arguments = recordingName;
                //result = Process.Start(startInfo);

                //return result;
            }

            private Process StartTracklinkSimulator(string pharusDirectory)
            {
                return StartPharus(pharusDirectory, "tlsim.exe", "");

                //Process result = null;

                //ProcessStartInfo startInfo = new ProcessStartInfo();
                //startInfo.FileName = "tlsim.exe";
                //startInfo.WorkingDirectory = MoxEditorConfig.PharusBaseDirectory + pharusDirectory;
                //result = Process.Start(startInfo);

                //return result;
            }

            private Process StartPharus(string directory, string exeName, string recordingName)
            {
                UnityEngine.Debug.Log("StartPharus");

                Process result = null;

                string baseDirectory = EditorConfig.PharusBaseDirectory;
                baseDirectory = baseDirectory.Replace("\\", "/");

                if (File.Exists(EditorConfig.PharusBaseDirectory + "/" + directory + "/" + exeName))
                {
                    result = EditorProcessUtility.LaunchProcess(EditorConfig.PharusBaseDirectory + "/" + directory, exeName, recordingName);


                    if(result != null)
                    {
                        System.IntPtr consoleHandle = Utility.ProcessUtility.GetProcessConsoleWindowHandle(result);

                        UnityEngine.Debug.Log("pharus process console handle: " + consoleHandle);

                        // int foo = 0;
                    }
                }
                else
                {
                    string fullPath = EditorConfig.PharusBaseDirectory + "/" + directory + "/" + exeName;

                    UnityEngine.Debug.LogError("Pharus executable not found at: " + fullPath);

                    EditorUtility.DisplayDialog("Pharus Executable not found.", "The Pharus executable could not be found at the specified location: " + fullPath + "\nCheck that you have the Pharus binaries downloaded and the MozXR configuration is correct.", "Ok");
                }

                return result;
            }

            private void UpdatePharusRecUnicastButtons()
            {
                if ((_processPharusUnicastFloor == null || _processPharusUnicastFloor.HasExited)
                    || (_processPharusUnicastWall == null || _processPharusUnicastWall.HasExited))
                {
                    _buttonPharusRecUniAll.text = "Start All";
                }
                else
                {
                    _buttonPharusRecUniAll.text = "Stop All";
                }

                if (_processPharusUnicastFloor == null || _processPharusUnicastFloor.HasExited)
                {
                    _buttonPharusRecUniFloor.text = "Start Floor";
                }
                else
                {
                    _buttonPharusRecUniFloor.text = "Stop Floor";
                }

                if (_processPharusUnicastWall == null || _processPharusUnicastWall.HasExited)
                {
                    _buttonPharusRecUniWall.text = "Start Wall";
                }
                else
                {
                    _buttonPharusRecUniWall.text = "Stop Wall";
                }
            }

            private void UpdatePharusRecMulticastButtons()
            {
                if ((_processPharusMulticastFloor == null || _processPharusMulticastFloor.HasExited)
                    || (_processPharusMulticastWall == null || _processPharusMulticastWall.HasExited))
                {
                    _buttonPharusRecMultiAll.text = "Start All";
                }
                else
                {
                    _buttonPharusRecMultiAll.text = "Stop All";
                }

                if (_processPharusMulticastFloor == null || _processPharusMulticastFloor.HasExited)
                {
                    _buttonPharusRecMultiFloor.text = "Start Floor";
                }
                else
                {
                    _buttonPharusRecMultiFloor.text = "Stop Floor";
                }

                if (_processPharusMulticastWall == null || _processPharusMulticastWall.HasExited)
                {
                    _buttonPharusRecMultiWall.text = "Start Wall";
                }
                else
                {
                    _buttonPharusRecMultiWall.text = "Stop Wall";
                }
            }

            private void UpdatePharusSimUnicastButtons()
            {
                if ((_processTracksimUnicastFloor == null || _processTracksimUnicastFloor.HasExited)
                    || (_processTracksimUnicastWall == null || _processTracksimUnicastWall.HasExited))
                {
                    _buttonPharusSimUniAll.text = "Start All";
                }
                else
                {
                    _buttonPharusSimUniAll.text = "Stop All";
                }

                if (_processTracksimUnicastFloor == null || _processTracksimUnicastFloor.HasExited)
                {
                    _buttonPharusSimUniFloor.text = "Start Floor";
                }
                else
                {
                    _buttonPharusSimUniFloor.text = "Stop Floor";
                }

                if (_processTracksimUnicastWall == null || _processTracksimUnicastWall.HasExited)
                {
                    _buttonPharusSimUniWall.text = "Start Wall";
                }
                else
                {
                    _buttonPharusSimUniWall.text = "Stop Wall";
                }
            }

            private void UpdatePharusSimMulticastButtons()
            {
                if ((_processTracksimMulticastFloor == null || _processTracksimMulticastFloor.HasExited)
                    || (_processTracksimMulticastWall == null || _processTracksimMulticastWall.HasExited))
                {
                    _buttonPharusSimMultiAll.text = "Start All";
                }
                else
                {
                    _buttonPharusSimMultiAll.text = "Stop All";
                }

                if (_processTracksimMulticastFloor == null || _processTracksimMulticastFloor.HasExited)
                {
                    _buttonPharusSimMultiFloor.text = "Start Floor";
                }
                else
                {
                    _buttonPharusSimMultiFloor.text = "Stop Floor";
                }

                if (_processTracksimMulticastWall == null || _processTracksimMulticastWall.HasExited)
                {
                    _buttonPharusSimMultiWall.text = "Start Wall";
                }
                else
                {
                    _buttonPharusSimMultiWall.text = "Stop Wall";
                }
            }
        }

    }
#endif
}

