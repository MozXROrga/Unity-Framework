using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mox
{
#if UNITY_EDITOR
    namespace Editor
    {
        public class EditorConfigEditor : EditorWindow
        {
            private TextField _textFieldPharusBase = null;
            private TextField _textFieldPharusRecording = null;

            private TextField _textFieldEndpoint = null;
            private TextField _textFieldNDisplayConfig = null;

            private TextField _textFieldMoxConfig = null;

            // [MenuItem("Tools/MozXR/Config", false, 4)]
            public static void ShowExample()
            {
                EditorConfigEditor wnd = GetWindow<EditorConfigEditor>();
                wnd.titleContent = new GUIContent("Configuration");
            }

            public void CreateGUI()
            {
                VisualElement root = rootVisualElement;

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
                    moxConfigInfo.style.whiteSpace = WhiteSpace.Normal;
                    moxConfigInfo.style.unityFontStyleAndWeight = FontStyle.Italic;
                    moxConfigRoot.Add(moxConfigInfo);
                }

                space = new VisualElement();
                space.style.height = 40f;
                root.Add(space);

                UnityEngine.UIElements.Button resetButton = new UnityEngine.UIElements.Button();
                resetButton.text = "Reset to Default";
                resetButton.clicked += OnResetClicked;
                root.Add(resetButton);

                // EditorApplication.isPlaying = true;

                // Application.isPlaying = true;
            }

            private void ValidatePathString(string pathString)
            {
                // int foo = 0;
            }

            public void Update()
            {
                bool commitChanges = false;

                if (_textFieldEndpoint != null)
                {
                    if (_textFieldEndpoint.value != EditorConfig.EndpointDirectory)
                    {
                        EditorConfig.EndpointDirectory = _textFieldEndpoint.value;

                        ValidatePathString(_textFieldEndpoint.value);

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



            private static VisualElement CreateHorizontalLayoutGroup(VisualElement parent)
            {
                VisualElement group = new VisualElement();
                group.style.flexDirection = FlexDirection.Row;
                group.style.alignItems = Align.Center;
                group.style.flexGrow = 1f;

                parent.Add(group);

                return group;
            }
        }
    }
#endif
}
