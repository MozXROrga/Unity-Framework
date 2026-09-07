using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_EDITOR
namespace Mox.Editor
{
    public class Interface : EditorWindow
    {
        #region PharusVariables
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
        #endregion

        #region ConfigVariables
        private TextField _textFieldPharusBase = null;
        private TextField _textFieldPharusRecording = null;

        private TextField _textFieldEndpoint = null;
        private TextField _textFieldNDisplayConfig = null;

        private TextField _textFieldMoxConfig = null;
        #endregion

        #region SimulatorVariables
        private Button _buttonSimulatorStartEndpoint = null;
        private Button _buttonSimulatorStartSingle = null;
        private Button _buttonSimulatorStartCluster = null;
        #endregion

        #region DeploymentVariables
        private Button _buttonDeploymentDeploy = null;
        #endregion

        [MenuItem("Tools/MozXR/Control Panel", false, 2)]
        public static void ShowExample()
        {
            Interface wnd = GetWindow<Interface>();
            wnd.titleContent = new GUIContent("MozXR");
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();

            ScrollView scrollView = new ScrollView();
            scrollView.style.flexGrow = 1f;
            rootVisualElement.Add(scrollView);

            CreateConfigGUI(scrollView);
            CreateSimulatorGUI(scrollView);
            CreateTrackingGUI(scrollView);
            CreateDeploymentGUI(scrollView);
        }

        private Foldout CreateFoldout(string text, bool isExpanded)
        {
            Foldout foldout = new Foldout();
            foldout.text = text;
            foldout.value = isExpanded;
            return foldout;
        }

        private Button CreateButton(string text, System.Action callback)
        {
            Button button = new Button();
            button.text = text;
            button.clicked += callback;
            return button;
        }

        private static VisualElement CreateHorizontalLayoutGroup(VisualElement parent)
        {
            VisualElement group = new VisualElement();
            group.style.flexDirection = FlexDirection.Row;
            group.style.alignItems = Align.Center;
            group.style.flexGrow = 1f;

            parent.Add(group);

            return group;
        }

        public void CreateConfigGUI(VisualElement parent)
        {
            Foldout configFoldout = CreateFoldout("Config", false);
            parent.Add(configFoldout);

            VisualElement root = configFoldout;

            // Endpoint
            {
                VisualElement endpointRoot = new VisualElement();
                root.Add(endpointRoot);

                VisualElement labelEndpoint = new Label("<b>Endpoint</b>");
                endpointRoot.Add(labelEndpoint);

                VisualElement endpointRow = CreateHorizontalLayoutGroup(endpointRoot);

                _textFieldEndpoint = new TextField("Endpoint Directory");
                _textFieldEndpoint.value = EditorConfig.EndpointDirectory;
                _textFieldEndpoint.tooltip = "Root-directory of the Endpoint Unreal application. Either absolute or relative to Unity project directory.";
                _textFieldEndpoint.style.flexGrow = 1f;
                endpointRow.Add(_textFieldEndpoint);

                UnityEngine.UIElements.Button endpointBrowseButton = new UnityEngine.UIElements.Button();
                endpointBrowseButton.text = "...";
                endpointBrowseButton.clicked += OnEndpointBaseDirectoryPickerClicked;
                endpointRow.Add(endpointBrowseButton);
            }

            VisualElement space = new VisualElement();
            space.style.height = 10f;
            root.Add(space);

            // Pharus
            {
                VisualElement pharusRoot = new VisualElement();
                root.Add(pharusRoot);

                VisualElement labelPharus = new Label("<b>Pharus</b>");
                pharusRoot.Add(labelPharus);

                VisualElement pharusBaseRow = CreateHorizontalLayoutGroup(pharusRoot);

                _textFieldPharusBase = new TextField("Base Directory");
                _textFieldPharusBase.value = EditorConfig.PharusBaseDirectory;
                _textFieldPharusBase.tooltip = "Directory that contains the 4 pharus-rec and 4 TrackLinkSimulator folders. Either absolute or relative to Unity project directory.";
                _textFieldPharusBase.style.flexGrow = 1f;
                pharusBaseRow.Add(_textFieldPharusBase);

                UnityEngine.UIElements.Button pharusBaseBrowseButton = new UnityEngine.UIElements.Button();
                pharusBaseBrowseButton.text = "...";
                pharusBaseBrowseButton.clicked += OnPharusBaseDirectoryPickerClicked;
                pharusBaseRow.Add(pharusBaseBrowseButton);

                VisualElement pharusRecordingRow = CreateHorizontalLayoutGroup(pharusRoot);

                _textFieldPharusRecording = new TextField("Recording");
                _textFieldPharusRecording.value = EditorConfig.PharusRecording;
                _textFieldPharusRecording.tooltip = "Can be either an absolute path or the same relative path to each of the 4 pharus-rec binaries (like the included default-recording)";
                _textFieldPharusRecording.style.flexGrow = 1f;
                pharusRecordingRow.Add(_textFieldPharusRecording);

                UnityEngine.UIElements.Button pharusRecordingBrowseButton = new UnityEngine.UIElements.Button();
                pharusRecordingBrowseButton.text = "...";
                pharusRecordingBrowseButton.clicked += OnPharusRecordingPickerClicked;
                pharusRecordingRow.Add(pharusRecordingBrowseButton);
            }

            space = new VisualElement();
            space.style.height = 10f;
            root.Add(space);

            // NDisplay
            {
                VisualElement ndisplayRoot = new VisualElement();
                root.Add(ndisplayRoot);

                VisualElement labelNDisplay = new Label("<b>NDisplay</b>");
                ndisplayRoot.Add(labelNDisplay);

                VisualElement ndisplayRow = CreateHorizontalLayoutGroup(ndisplayRoot);

                _textFieldNDisplayConfig = new TextField("NDisplay Config");
                _textFieldNDisplayConfig.value = EditorConfig.NDisplayConfig;
                _textFieldNDisplayConfig.tooltip = "Path to the NDisplay configuration used during cluster simulation. Either absolute or relative to Unity project directory.";
                _textFieldNDisplayConfig.style.flexGrow = 1f;
                ndisplayRow.Add(_textFieldNDisplayConfig);

                UnityEngine.UIElements.Button ndisplayBrowseButton = new UnityEngine.UIElements.Button();
                ndisplayBrowseButton.text = "...";
                ndisplayBrowseButton.clicked += OnNDisplayConfigPickerClicked;
                ndisplayRow.Add(ndisplayBrowseButton);
            }

            space = new VisualElement();
            space.style.height = 10f;
            root.Add(space);

            // MoxConfig
            {
                VisualElement moxConfigRoot = new VisualElement();
                root.Add(moxConfigRoot);

                VisualElement labelMoxConfig = new Label("<b>MozConfig</b>");
                moxConfigRoot.Add(labelMoxConfig);

                VisualElement moxConfigRow = CreateHorizontalLayoutGroup(moxConfigRoot);

                _textFieldMoxConfig = new TextField("Moz Config");
                _textFieldMoxConfig.value = EditorConfig.MoxConfig;
                _textFieldMoxConfig.tooltip = "Path to MozConfig.ini. Either absolute or relative to Unity project directory.";
                _textFieldMoxConfig.style.flexGrow = 1f;
                moxConfigRow.Add(_textFieldMoxConfig);

                UnityEngine.UIElements.Button moxConfigBrowseButton = new UnityEngine.UIElements.Button();
                moxConfigBrowseButton.text = "...";
                moxConfigBrowseButton.clicked += OnMoxConfigPickerClicked;
                moxConfigRow.Add(moxConfigBrowseButton);

                Label moxConfigInfo = new Label("Info: The Endpoint Application needs to be restarted if you select a different configuration .ini file.");
                moxConfigInfo.style.marginTop = 4.0f;
                moxConfigInfo.style.whiteSpace = WhiteSpace.Normal;
                moxConfigInfo.style.unityFontStyleAndWeight = FontStyle.Italic;
                moxConfigRoot.Add(moxConfigInfo);
            }

            space = new VisualElement();
            space.style.height = 20f;
            root.Add(space);

            UnityEngine.UIElements.Button resetButton = new UnityEngine.UIElements.Button();
            resetButton.text = "Reset to Default";
            resetButton.clicked += OnResetClicked;
            root.Add(resetButton);

            space = new VisualElement();
            space.style.height = 30f;
            root.Add(space);
        }

        public void CreateSimulatorGUI(VisualElement parent)
        {
            VisualElement root = parent;

            Foldout foldout = CreateFoldout("Simulator", true);
            root.Add(foldout);

            _buttonSimulatorStartEndpoint = CreateButton("Start Endpoint", OnStartEndpoint);
            foldout.Add(_buttonSimulatorStartEndpoint);

            _buttonSimulatorStartSingle = CreateButton("Start Single", OnStartSingle);
            foldout.Add(_buttonSimulatorStartSingle);

            _buttonSimulatorStartCluster = CreateButton("Start Cluster", OnStartCluster);
            foldout.Add(_buttonSimulatorStartCluster);

            VisualElement space = new VisualElement();
            space.style.height = 30f;
            foldout.Add(space);
        }

        public void CreateTrackingGUI(VisualElement parent)
        {
            VisualElement root = parent;

            Foldout trackingFoldout = CreateFoldout("Tracking", false);
            root.Add(trackingFoldout);

            Foldout pharusFoldout = CreateFoldout("Pharus", true);
            trackingFoldout.Add(pharusFoldout);

            Foldout pharusRecordingFoldout = CreateFoldout("Pharus (Recording)", true);
            pharusFoldout.Add(pharusRecordingFoldout);

            Label labelPharusUnicast = new Label("Unicast");
            pharusRecordingFoldout.Add(labelPharusUnicast);

            _buttonPharusRecUniAll = CreateButton("Start All", OnStartPharusUnicastAll);
            pharusRecordingFoldout.Add(_buttonPharusRecUniAll);

            _buttonPharusRecUniFloor = CreateButton("Start Floor", OnStartPharusUnicastFloor);
            pharusRecordingFoldout.Add(_buttonPharusRecUniFloor);

            _buttonPharusRecUniWall = CreateButton("Start Wall", OnStartPharusUnicastWall);
            pharusRecordingFoldout.Add(_buttonPharusRecUniWall);

            Label labelPharusMulticast = new Label("Multicast");
            pharusRecordingFoldout.Add(labelPharusMulticast);

            _buttonPharusRecMultiAll = CreateButton("Start All", OnStartPharusMulticastAll);
            pharusRecordingFoldout.Add(_buttonPharusRecMultiAll);

            _buttonPharusRecMultiFloor = CreateButton("Start Floor", OnStartPharusMulticastFloor);
            pharusRecordingFoldout.Add(_buttonPharusRecMultiFloor);

            _buttonPharusRecMultiWall = CreateButton("Start Wall", OnStartPharusMulticastWall);
            pharusRecordingFoldout.Add(_buttonPharusRecMultiWall);

            Foldout pharusSimulatorFoldout = CreateFoldout("Pharus (Simulator)", true);
            pharusFoldout.Add(pharusSimulatorFoldout);

            Label labelPharusSimulatorUnicast = new Label("Unicast");
            pharusSimulatorFoldout.Add(labelPharusSimulatorUnicast);

            _buttonPharusSimUniAll = CreateButton("Start All", OnStartTracksimUnicastAll);
            pharusSimulatorFoldout.Add(_buttonPharusSimUniAll);

            _buttonPharusSimUniFloor = CreateButton("Start Floor", OnStartTracksimUnicastFloor);
            pharusSimulatorFoldout.Add(_buttonPharusSimUniFloor);

            _buttonPharusSimUniWall = CreateButton("Start Wall", OnStartTracksimUnicastWall);
            pharusSimulatorFoldout.Add(_buttonPharusSimUniWall);

            Label labelPharusSimulatorMulticast = new Label("Multicast");
            pharusSimulatorFoldout.Add(labelPharusSimulatorMulticast);

            _buttonPharusSimMultiAll = CreateButton("Start All", OnStartTracksimMulticastAll);
            pharusSimulatorFoldout.Add(_buttonPharusSimMultiAll);

            _buttonPharusSimMultiFloor = CreateButton("Start Floor", OnStartTracksimMulticastFloor);
            pharusSimulatorFoldout.Add(_buttonPharusSimMultiFloor);

            _buttonPharusSimMultiWall = CreateButton("Start Wall", OnStartTracksimMulticastWall);
            pharusSimulatorFoldout.Add(_buttonPharusSimMultiWall);

            //Button buttonRemoveConsole = CreateButton("Remove Console", OnConsoleRemoveTest);
            //pharusSimulatorFoldout.Add(buttonRemoveConsole);

            VisualElement space = new VisualElement();
            space.style.height = 30f;
            pharusFoldout.Add(space);
        }

        public void CreateDeploymentGUI(VisualElement parent)
        {
            VisualElement root = parent;

            Foldout foldout = CreateFoldout("Deployment", false);
            root.Add(foldout);

            _buttonDeploymentDeploy = CreateButton("Deploy", OnDeploymentDeploy);
            foldout.Add(_buttonDeploymentDeploy);

            VisualElement space = new VisualElement();
            space.style.height = 30f;
            foldout.Add(space);
        }

        #region PharusCallbacks
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

        private void OnConsoleRemoveTest()
        {
            if(_processTracksimMulticastFloor != null)
            {
                System.IntPtr consoleHandle = Utility.ProcessUtility.GetProcessConsoleWindowHandle(_processTracksimMulticastFloor);

                if(consoleHandle != (System.IntPtr)0x0)
                {
                    Utility.ProcessUtility.DetachAndCloseConsole(consoleHandle);
                }
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
            Process result = null;

            string baseDirectory = EditorConfig.PharusBaseDirectory;
            baseDirectory = baseDirectory.Replace("\\", "/");

            if (File.Exists(EditorConfig.PharusBaseDirectory + "/" + directory + "/" + exeName))
            {
                result = EditorProcessUtility.LaunchProcess(EditorConfig.PharusBaseDirectory + "/" + directory, exeName, recordingName);

                if (result != null)
                {
                    System.IntPtr consoleHandle = Utility.ProcessUtility.GetProcessConsoleWindowHandle(result);

                    UnityEngine.Debug.Log("pharus process console handle: " + consoleHandle);

                    int foo = 0;
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
        #endregion

        #region ConfigCallbacks
        private void OnResetClicked()
        {
            if (EditorUtility.DisplayDialog("Reset to Default", "Are you sure you want to reset the configuration? All your changes will be lost.", "Reset", "Keep Current") == true)
            {
                EditorConfig.EndpointDirectory = EditorConfig.DefaultEndpointDirectory;
                EditorConfig.PharusBaseDirectory = EditorConfig.DefaultPharusBaseDirectory;
                EditorConfig.PharusRecording = EditorConfig.DefaultPharusRecording;
                EditorConfig.NDisplayConfig = EditorConfig.DefaultNDisplayConfig;
                EditorConfig.MoxConfig = EditorConfig.DefaultMoxConfig;

                _textFieldEndpoint.value = EditorConfig.EndpointDirectory;
                _textFieldPharusBase.value = EditorConfig.PharusBaseDirectory;
                _textFieldPharusRecording.value = EditorConfig.PharusRecording;
                _textFieldNDisplayConfig.value = EditorConfig.NDisplayConfig;
                _textFieldMoxConfig.value = EditorConfig.MoxConfig;

                EditorConfig.CommitChanges();
            }
        }


        private void OnEndpointBaseDirectoryPickerClicked()
        {
            string defaultDirectory = ResolveDefaultDirectory(EditorConfig.EndpointDirectory);
            string selectedDirectory = EditorUtility.OpenFolderPanel("Select Endpoint Base Directory", defaultDirectory, "");

            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                _textFieldEndpoint.value = selectedDirectory;
            }
        }

        private void OnPharusBaseDirectoryPickerClicked()
        {
            string defaultDirectory = ResolveDefaultDirectory(EditorConfig.PharusBaseDirectory);
            string selectedDirectory = EditorUtility.OpenFolderPanel("Select Pharus Base Directory", defaultDirectory, "");

            if (string.IsNullOrEmpty(selectedDirectory) == false)
            {
                _textFieldPharusBase.value = selectedDirectory;
            }
        }

        private void OnPharusRecordingPickerClicked()
        {
            string defaultDirectory = ResolveDefaultDirectory(EditorConfig.PharusRecording);
            string selectedFile = EditorUtility.OpenFilePanel("Select Pharus Recording", defaultDirectory, "rec");

            if (string.IsNullOrEmpty(selectedFile) == false)
            {
                _textFieldPharusRecording.value = selectedFile;
            }
        }

        private void OnNDisplayConfigPickerClicked()
        {
            string defaultDirectory = ResolveDefaultDirectory(EditorConfig.NDisplayConfig);
            string selectedFile = EditorUtility.OpenFilePanel("Select NDisplay Config", defaultDirectory, "ndisplay");

            if (string.IsNullOrEmpty(selectedFile) == false)
            {
                _textFieldNDisplayConfig.value = selectedFile;
            }
        }

        private void OnMoxConfigPickerClicked()
        {
            string defaultDirectory = ResolveDefaultDirectory(EditorConfig.MoxConfig);
            string selectedFile = EditorUtility.OpenFilePanel("Select Mox Config", defaultDirectory, "ini");

            if (string.IsNullOrEmpty(selectedFile) == false)
            {
                _textFieldMoxConfig.value = selectedFile;
            }
        }

        private static string ResolveDefaultDirectory(string configuredPath)
        {
            string projectDirectory = System.IO.Path.GetDirectoryName(Application.dataPath);

            if (string.IsNullOrEmpty(configuredPath))
            {
                return projectDirectory;
            }

            string result = configuredPath;

            if (System.IO.Path.IsPathRooted(result) == false)
            {
                result = System.IO.Path.Combine(projectDirectory, result);
            }

            result = System.IO.Path.GetFullPath(result);

            if (System.IO.File.Exists(result))
            {
                result = System.IO.Path.GetDirectoryName(result);
            }

            if (System.IO.Directory.Exists(result) == false)
            {
                return projectDirectory;
            }

            return result;
        }
        #endregion

        #region SimulatorCallbacks
        private void OnStartEndpoint()
        {
            if (EndpointControlls.IsEndpointRunning() == false)
            {
                EndpointControlls.StartEndpointIfNotRunning();
            }
        }

        private void OnStartSingle()
        {
            StartPlay();
        }

        private void OnStartCluster()
        {
            GameObject paramObject = new GameObject("MoxSimulationParams", typeof(Mox.Simulator.SimulationParams));
            Mox.Simulator.SimulationParams._destroy = false;

            StartPlay();
        }

        private static void StartPlay()
        {
            if (EndpointControlls.IsEndpointRunning() == false)
            {
                EndpointControlls.StartEndpointIfNotRunning();
            }

            EditorApplication.isPlaying = true;
        }
        #endregion

        #region DeploymentCallbacks
        private void OnDeploymentDeploy()
        {
            DeploymentControlls.Deploy();
        }
        #endregion

        public void Update()
        {
            bool commitChanges = false;

            if (_textFieldEndpoint != null)
            {
                if (_textFieldEndpoint.value != EditorConfig.EndpointDirectory)
                {
                    EditorConfig.EndpointDirectory = _textFieldEndpoint.value;

                    commitChanges = true;
                }
            }

            if (_textFieldPharusBase != null)
            {
                if (_textFieldPharusBase.value != EditorConfig.PharusBaseDirectory)
                {
                    EditorConfig.PharusBaseDirectory = _textFieldPharusBase.value;

                    commitChanges = true;
                }
            }

            if (_textFieldPharusRecording != null)
            {
                if (_textFieldPharusRecording.value != EditorConfig.PharusRecording)
                {
                    EditorConfig.PharusRecording = _textFieldPharusRecording.value;

                    commitChanges = true;
                }
            }

            if (_textFieldNDisplayConfig != null)
            {
                if (_textFieldNDisplayConfig.value != EditorConfig.NDisplayConfig)
                {
                    EditorConfig.NDisplayConfig = _textFieldNDisplayConfig.value;

                    commitChanges = true;
                }
            }

            if (_textFieldMoxConfig != null)
            {
                if (_textFieldMoxConfig.value != EditorConfig.MoxConfig)
                {
                    EditorConfig.MoxConfig = _textFieldMoxConfig.value;
                    commitChanges = true;
                }
            }

            if (commitChanges)
            {
                EditorConfig.CommitChanges();
            }
        }
    }
}
#endif

