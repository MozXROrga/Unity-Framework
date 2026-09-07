using System;
using System.IO;
using UnityEngine;

namespace Mox
{
    namespace Editor
    {
        public class EditorConfig
        {
            // default values
            private string _defaultPharusBaseDirectory = "../Pharus/";
            private string _defaultPharusRecording = "limelight1.rec";

            private string _defaultEndpointDirectory = "../Endpoint/";

            private string _defaultNDisplayConfig = "../Endpoint/Switchboard/ND_X_Reality_Lab.ndisplay";

            private string _defaultMoxConfig = "../Endpoint/Build/Windows/MoxUnityEndpoint/Content/Configs/X-Reality-Lab.ini";

            public static string PharusBaseDirectory
            {
                get
                {
                    InitInstance();

                    return _instance._config.PharusBaseDirectory;
                }
                internal set { _instance._config.PharusBaseDirectory = value; }
            }

            public static string PharusRecording
            {
                get
                {
                    InitInstance();

                    return _instance._config.PharusRecording;
                }
                internal set { _instance._config.PharusRecording = value; }
            }

            public static string EndpointDirectory
            {
                get
                {
                    InitInstance();

                    return _instance._config.EndpointDirectory;
                }
                internal set { _instance._config.EndpointDirectory = value; }
            }

            public static string NDisplayConfig
            {
                get
                {
                    InitInstance();

                    return _instance._config.NDisplayConfig;
                }
                internal set { _instance._config.NDisplayConfig = value; }
            }

            public static string MoxConfig
            {
                get
                {
                    InitInstance();

                    return _instance._config.MoxConfig;
                }
                internal set { _instance._config.MoxConfig = value; }
            }


            public static string DefaultPharusBaseDirectory
            {
                get { InitInstance(); return _instance._defaultPharusBaseDirectory; }
            }

            public static string DefaultPharusRecording
            {
                get { InitInstance(); return _instance._defaultPharusRecording; }
            }

            public static string DefaultNDisplayConfig
            {
                get { InitInstance(); return _instance._defaultNDisplayConfig; }
            }

            public static string DefaultEndpointDirectory
            {
                get { InitInstance(); return _instance._defaultEndpointDirectory; }
            }

            public static string DefaultMoxConfig
            {
                get { InitInstance(); return _instance._defaultMoxConfig; }
            }

            private static EditorConfig _instance = null;

            private SEditorConfig _config;
            private string _configPath = "MoxEditor\\config.json";

            [Serializable]
            struct SEditorConfig
            {
                [SerializeField]
                public string PharusBaseDirectory;
                [SerializeField]
                public string PharusRecording;
                [SerializeField]
                public string EndpointDirectory;
                [SerializeField]
                public string NDisplayConfig;
                [SerializeField]
                public string MoxConfig;
            }

            internal static void CommitChanges()
            {
                InitInstance();

                _instance.CommitChangesIntenal();
            }

            private static void InitInstance()
            {
                if (_instance == null)
                {
                    _instance = new EditorConfig();
                    _instance.ParseEditorConfig();
                }
            }

            private void ParseEditorConfig()
            {
                string configPath = Application.dataPath + "\\" + _configPath;

                string configJson = "";
                if (System.IO.File.Exists(configPath) == false)
                {
                    configJson = GetDefaultEditorConfig();

                    if (System.IO.Directory.Exists(Application.dataPath + "\\MoxEditor\\") == false)
                    {
                        System.IO.Directory.CreateDirectory(Application.dataPath + "\\MoxEditor\\");
                    }

                    StreamWriter configWriter = new StreamWriter(System.IO.File.Create(configPath));
                    configWriter.Write(configJson);
                    configWriter.Close();
                }
                else
                {
                    configJson = System.IO.File.ReadAllText(configPath);
                }

                _config = JsonUtility.FromJson<SEditorConfig>(configJson);
            }

            private string GetDefaultEditorConfig()
            {
                string result = "";

                result += "{\n";

                result += "    \"PharusBaseDirectory\":" + "\"" + _defaultPharusBaseDirectory + "\"" + ",\n";
                result += "    \"PharusRecording\":" + "\"" + _defaultPharusRecording + "\"" + ",\n";
                result += "    \"EndpointDirectory\":" + "\"" + _defaultEndpointDirectory + "\"" + ",\n";
                result += "    \"NDisplayConfig\":" + "\"" + _defaultNDisplayConfig + "\"" + ",\n";
                result += "    \"MoxConfig\":" + "\"" + _defaultMoxConfig + "\"" + "\n";

                result += "}";

                return result;
            }

            private void CommitChangesIntenal()
            {
                if (System.IO.Directory.Exists(Application.dataPath + "\\MoxEditor\\") == false)
                {
                    System.IO.Directory.CreateDirectory(Application.dataPath + "\\MoxEditor\\");
                }

                string configPath = Application.dataPath + "\\" + _configPath;
                StreamWriter configWriter = null;
                if (System.IO.File.Exists(configPath) == false)
                {
                    configWriter = new StreamWriter(System.IO.File.Create(configPath));
                }
                else
                {
                    configWriter = new StreamWriter(System.IO.File.Open(configPath, FileMode.Truncate));
                }

                string configJson = JsonUtility.ToJson(_config);
                configWriter.Write(configJson);
                configWriter.Close();
            }
        }
    }
}
