using System.Collections.Generic;
using UnityEngine;
using static Mox.NDisplay.CameraUtility;

namespace Mox
{
    namespace NDisplay
    {
        [DefaultExecutionOrder(99)]
        public class CameraHandler : MonoBehaviour
        {
            [SerializeField]
            private Spout.SpoutInitializer _spoutInitializer = null;

            // [SerializeField]
            // private string _nDisplayConfigPath = ""; // may be an absolute path or relative to streaming assets

            [SerializeField]
            private float _interOcularDistanceInM = 0.064f;

            [Header("Debug Options")]
            [SerializeField]
            private bool _showScreenDebug = false;

            [SerializeField]
            private Material _frameMaterial = null;

            // Hide in Inspector, this setting is really only useful for debugging stereo camera setup, for framework users it's probably just confusing
            //[Header("Only applies in Editor, use '--stereo' cmd argument for standalone)")]
            //[SerializeField]
            private bool _debugStereo = false;

            private bool _stereo = false; // set at runtime according to _debugStereo (editor) or launch arguments (standalone)

            private static CameraHandler _instance = null;

            public static CameraHandler Instance
            {
                get
                {
                    return _instance;
                }
            }

            private Vector2 _frontLeftCorner = Vector2.zero;
            private bool _frontLeftCornerSet = false;

            // Front-left corner of all floor screens in world space, usefull to programatically place objects relative to NDisplay setup
            public Vector2 FrontLeftCorner
            {
                get
                {
                    return _frontLeftCorner;
                }
            }

            // To check whether the front-left corner has been set, because 0/0 might be a valid position...
            public bool FrontLeftCornerSet
            {
                get
                {
                    return _frontLeftCornerSet;
                }
            }


            #region MONOBEHAVIOUR
            // Stereo3d script sets up stereo cameras in OnEnable. To set the main camera before that, CameraHandler has to do it's thing in OnEnable too...
            private void OnEnable()
            {
                // only one camera handler per scene, redo setup on scene change though
                CameraHandler[] cameraHandler = FindObjectsByType<CameraHandler>(FindObjectsSortMode.None);
                if (cameraHandler.Length == 1)
                {
                    _instance = this;

                    // DontDestroyOnLoad(gameObject);

                    Enable();
                }
                else if (cameraHandler.Length > 1)
                {
                    gameObject.SetActive(false);

                    return;
                }
            }

            private void Enable()
            {
                if (Application.isEditor)
                {
                    _stereo = _debugStereo;
                }
                else
                {
                    string[] cmdArgs = System.Environment.GetCommandLineArgs();

                    _stereo = false;

                    foreach (string arg in cmdArgs)
                    {
                        if (arg == "--stereo")
                        {
                            _stereo = true;
                        }
                    }
                }

                Debug.Log("Stereo mode: " + _stereo);

                if (Application.isEditor)
                {
                    OnEnableInEditor();
                }
                else
                {
                    OnEnableStandalone();
                }
            }

            private void OnEnableInEditor()
            {
                // if (_nDisplayConfigPath.Length > 0)
                {
                    NDisplayParser.NDisplayConfig config = NDisplayParser.ParseNDisplayConfig(Editor.EditorConfig.NDisplayConfig);

                    Simulator.SimulationParams simParams = FindFirstObjectByType<Simulator.SimulationParams>();

                    List<Camera> cameras = new List<Camera>();

                    int spoutStartIndex = 0;

                    if (simParams != null)
                    {
                        string primaryNodeId = GetPrimaryNodeId(config);

                        foreach (NDisplayParser.NodeData nd in config._nodeData)
                        {
                            string ip = nd.ip;

                            List<Camera> newCameras = SetupNodeCameras(nd, config, ip);

                            // cameras.Add(newCamera);

                            if(nd.name == primaryNodeId
                                && newCameras.Count > 0)
                            {
                                HandleAudioListener(newCameras[0]);
                            }

                            cameras.AddRange(newCameras);
                        }
                    }
                    else // single camera
                    {
                        SetupSingleNodeCameras(config, ref cameras, ref spoutStartIndex);

                        if(cameras.Count > 0)
                        {
                            HandleAudioListener(cameras[0]);
                        }
                    }

                    foreach (Camera cam in cameras)
                    {
                        cam.transform.SetParent(gameObject.transform, false);
                    }

                    if (_spoutInitializer != null)
                    {
                        Debug.Log("Initialize spout senders with offset: " + spoutStartIndex);

                        _spoutInitializer.SetupSpoutForCameras(cameras, spoutStartIndex);
                    }
                }
            }

            private void OnEnableStandalone()
            {
                string configPath = "D:/DeepSpace/Misc/MoxUnityEndpoint57/ND_X_Reality_Lab.ndisplay"; // debug fallback

                string[] cmdArgs = System.Environment.GetCommandLineArgs();
                bool configPathNext = false;
                foreach (string arg in cmdArgs)
                {
                    if (arg == "--ndisplay")
                    {
                        configPathNext = true;
                    }
                    else if (configPathNext)
                    {
                        configPath = arg;

                        break;
                    }
                }

                Debug.Log("Using nDisplay config at: " + configPath);

                NDisplayParser.NDisplayConfig config = NDisplayParser.ParseNDisplayConfig(configPath);

                _showScreenDebug = false; // never show the debug frame in standalone, don't want to accidentally keep it in when executing in render cluster

                List<Camera> cameras = new List<Camera>();

                int spoutStartIndex = 0;
                SetupSingleNodeCameras(config, ref cameras, ref spoutStartIndex);

                if (_spoutInitializer != null)
                {
                    _spoutInitializer.SetupSpoutForCameras(cameras, spoutStartIndex);
                }
            }

            private void SetupSingleNodeCameras(NDisplayParser.NDisplayConfig config, ref List<Camera> cameras, ref int spoutStartIndex)
            {
                Debug.Log("Init single node cameras");

                spoutStartIndex = 0;
                string ip = Utility.NetworkUtility.GetNetworkIP();

                NDisplayParser.NodeData nodeData = null;

                if (Application.isEditor)
                {
                    Debug.Log("Init single node cameras - editor");

                    string primaryNodeId = GetPrimaryNodeId(config);

                    Debug.Log("Using primary node ID: " + primaryNodeId);

                    foreach (NDisplayParser.NodeData nd in config._nodeData)
                    {
                        if (nd.name == primaryNodeId)
                        {
                            ip = nd.ip;
                            nodeData = nd;
                            break;
                        }

                        spoutStartIndex += nd.viewports.Count;
                    }

                    if (_stereo == true)
                    {
                        spoutStartIndex *= 2;
                    }
                }
                else
                {
                    string[] cmdArgs = System.Environment.GetCommandLineArgs();
                    bool overrideIPNext = false;
                    foreach (string arg in cmdArgs)
                    {
                        if (arg == "--overrideClusterIP")
                        {
                            overrideIPNext = true;
                        }
                        else if (overrideIPNext)
                        {
                            Debug.Log("Overriding detected IP '" + ip + "' from command line: " + arg);

                            ip = arg;

                            break;
                        }
                    }

                    Debug.Log("Init cameras - standalone with IP " + ip);

                    foreach (NDisplayParser.NodeData nd in config._nodeData)
                    {
                        if (nd.ip == ip)
                        {
                            nodeData = nd;
                            break;
                        }

                        spoutStartIndex += nd.viewports.Count;
                    }

                    if (_stereo == true)
                    {
                        spoutStartIndex *= 2;
                    }
                }

                cameras = SetupNodeCameras(nodeData, config, ip);

                foreach (Camera cam in cameras)
                {
                    cam.transform.SetParent(gameObject.transform, false);
                }
            }

            void Start()
            {
                Simulator.SimulationParams simParams = FindFirstObjectByType<Simulator.SimulationParams>();
            }
            #endregion

            private List<Camera> SetupNodeCameras(NDisplayParser.NodeData nodeData, NDisplayParser.NDisplayConfig config, string ip)
            {
                List<Camera> result = new List<Camera>();

                Debug.Log("Setting up camera(s) for node + " + nodeData.name);

                if (nodeData != null)
                {
                    foreach (NDisplayParser.ViewportData viewportData in nodeData.viewports)
                    {
                        string cameraName = viewportData.cameraName;
                        string screenName = viewportData.screenName;
                        if (screenName.Length <= 0)
                        {
                            screenName = viewportData.meshName; // fallback to mesh name if screen name is not provided
                        }

                        NDisplayParser.CameraData cameraData = null;
                        NDisplayParser.ScreenData screenData = null;
                        List<NDisplayParser.TransformData> transformData = null;

                        if (cameraName.Length > 0
                            && screenName.Length > 0)
                        {
                            foreach (NDisplayParser.CameraData camData in config._cameraData)
                            {
                                if (camData.name == cameraName)
                                {
                                    cameraData = camData;
                                }
                            }

                            foreach (NDisplayParser.ScreenData sData in config._screenData)
                            {
                                if (sData.name == screenName)
                                {
                                    screenData = sData;
                                }
                            }

                            if (screenData != null)
                            {
                                transformData = GetTransformDataForScreen(screenData, config._transformData);
                            }
                        }

                        if (cameraData != null
                            && screenData != null)
                        {
                            // temporary object to help calculating camera frustum
                            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                            plane.name = "nDisplay_" + screenName + "_" + ip;

                            Stack<NDisplayParser.TransformData> transformStack = new Stack<NDisplayParser.TransformData>();

                            NDisplayParser.TransformData transform = new NDisplayParser.TransformData();
                            transform.position = screenData.position;
                            transform.orientation = screenData.orientation;

                            transformStack.Push(transform);

                            foreach (NDisplayParser.TransformData td in transformData)
                            {
                                transformStack.Push(td);
                            }

                            Vector3 position = Vector3.zero;
                            Vector3 prevOrientation = Vector3.zero;
                            Vector3 orientation = Vector3.zero;

                            while (transformStack.Count > 0)
                            {
                                NDisplayParser.TransformData td = transformStack.Pop();

                                Vector3 pos = new Vector3(td.position.y, td.position.z, td.position.x);

                                Quaternion rot = Quaternion.Euler(prevOrientation);

                                pos = rot * pos;

                                position += pos;

                                prevOrientation = new Vector3(-td.orientation.y, td.orientation.z, td.orientation.x);

                                orientation += prevOrientation;
                            }


                            plane.transform.localScale = new UnityEngine.Vector3(screenData.size.x / 100.0f, 0.0f, screenData.size.y / 100.0f) * 0.1f; // x 0.1 because the plane is 10x10 m, right?

                            plane.transform.position = position * 0.01f;

                            plane.transform.localEulerAngles = new Vector3(-90.0f, 0.0f, 0.0f) + orientation; // rotation;



                            {
                                GameObject cameraObject = new GameObject("cam_" + ip, typeof(Camera));
                                Camera camera = cameraObject.GetComponent<Camera>();

                                camera.transform.position = new UnityEngine.Vector3(cameraData.position.y, cameraData.position.z, cameraData.position.x) * 0.1f;
                                camera.transform.forward = plane.transform.up * -1.0f; // align camera "plane" with view plane


                                if (_stereo)
                                {
                                    // Source Stereo3D.cs
                                    float imageWidth = Screen.width * 25.4f / Screen.dpi;
                                    float shift = (_interOcularDistanceInM * 1000.0f) / imageWidth;

                                    //if (method == Method.SideBySide_Full || method == Method.SideBySide_HMD)
                                    //    imageWidth *= .5f;

                                    ////shift = imageOffset = userIPD / imageWidth; //shift optic axis relative to the screen size (UserIPD/screenSize)
                                    //float shift = imageOffset = userIPD / imageWidth;

                                    // left
                                    {
                                        camera.name += "_left";

                                        camera.transform.position += plane.transform.right * -0.032f;

                                        Frustum frustum = CameraUtility.CalculateCameraFrustum(camera, plane.transform);
                                        UnityEngine.Matrix4x4 projectionMatrix = CameraUtility.CreateProjectionMatrix(frustum);
                                        float fov = CameraUtility.CalculateFOV(camera, plane.transform);

                                        projectionMatrix[0, 2] = shift;

                                        camera.fieldOfView = fov;
                                        camera.projectionMatrix = projectionMatrix;

                                        result.Add(camera);
                                    }

                                    // right
                                    {
                                        GameObject cameraObjectRight = new GameObject("cam_" + ip + "_right", typeof(Camera));
                                        Camera cameraRight = cameraObjectRight.GetComponent<Camera>();

                                        cameraRight.transform.position = new UnityEngine.Vector3(cameraData.position.y, cameraData.position.z, cameraData.position.x) * 0.1f;
                                        cameraRight.transform.forward = plane.transform.up * -1.0f; // align camera "plane" with view plane
                                        cameraRight.transform.position += plane.transform.right * 0.032f;

                                        Frustum frustum = CameraUtility.CalculateCameraFrustum(cameraRight, plane.transform);
                                        UnityEngine.Matrix4x4 projectionMatrix = CameraUtility.CreateProjectionMatrix(frustum);
                                        float fov = CameraUtility.CalculateFOV(cameraRight, plane.transform);

                                        projectionMatrix[0, 2] = -shift;

                                        cameraRight.fieldOfView = fov;
                                        cameraRight.projectionMatrix = projectionMatrix;

                                        result.Add(cameraRight);
                                    }
                                }
                                else
                                {
                                    Frustum frustum = CameraUtility.CalculateCameraFrustum(camera, plane.transform);
                                    UnityEngine.Matrix4x4 projectionMatrix = CameraUtility.CreateProjectionMatrix(frustum);
                                    float fov = CameraUtility.CalculateFOV(camera, plane.transform);

                                    camera.fieldOfView = fov;
                                    camera.projectionMatrix = projectionMatrix;

                                    result.Add(camera);
                                }
                            }

                            if(nodeData.name.ToLower().Contains("floor"))
                            {
                                float xPos = plane.transform.position.x - (plane.transform.localScale.x * (10.0f * 0.5f)); // * 10 because planes are 10x10 at scale 1.0
                                float zPos = plane.transform.position.z + (plane.transform.localScale.z * (10.0f * 0.5f)); // * 10 because planes are 10x10 at scale 1.0

                                if(_frontLeftCornerSet == false)
                                {
                                    _frontLeftCorner = new Vector2(xPos, zPos);

                                    _frontLeftCornerSet = true;
                                }
                                else
                                {
                                    if(xPos < _frontLeftCorner.x)
                                    {
                                        _frontLeftCorner.x = xPos;
                                    }
                                    if(zPos > _frontLeftCorner.y)
                                    {
                                        _frontLeftCorner.y = zPos;
                                    }
                                }
                            }

                            if (_showScreenDebug == false)
                            {
                                GameObject.DestroyImmediate(plane);
                            }
                            else
                            {
                                MeshRenderer meshRenderer = plane.GetComponent<MeshRenderer>();
                                if (meshRenderer != null)
                                {
                                    meshRenderer.material = _frameMaterial;
                                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                                }

                                plane.transform.SetParent(gameObject.transform, false);
                            }
                        }
                    }
                }

                return result;
            }

            private List<NDisplayParser.TransformData> GetTransformDataForScreen(NDisplayParser.ScreenData screenData, List<NDisplayParser.TransformData> transformData)
            {
                List<NDisplayParser.TransformData> result = new List<NDisplayParser.TransformData>();

                if (transformData.Count <= 0)
                {
                    return result;
                }

                string parentId = screenData.parentId;

                List<string> visitedTransforms = new List<string>(); // for circular dependency detection

                while (parentId != null
                    && parentId.Length > 0)
                {
                    foreach (NDisplayParser.TransformData td in transformData)
                    {
                        if (td.name == parentId)
                        {
                            if (visitedTransforms.Contains(parentId))
                            {
                                Debug.LogError("Circular dependency in transform hierarchy");
                                result.Clear();
                                return result;
                            }

                            result.Add(td);
                            visitedTransforms.Add(parentId);
                            parentId = td.parentId;
                        }
                    }
                }

                return result;
            }

            private void HandleAudioListener(Camera targetCamera)
            {
                AudioListener mainCameraListener = Camera.main.transform.GetComponent<AudioListener>();
                if(mainCameraListener != null)
                {
                    Destroy(mainCameraListener);
                }

                targetCamera.gameObject.AddComponent<AudioListener>();
            }

            private string GetPrimaryNodeId(NDisplayParser.NDisplayConfig config)
            {
                string result = "";
                if (config._nodeData.Count > 0)
                {
                    result = config._nodeData[0].primaryId;
                }
                return result;
            }
        }
    }
}
