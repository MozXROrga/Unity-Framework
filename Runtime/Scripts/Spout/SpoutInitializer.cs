using System.Collections.Generic;
using UnityEngine;
using Klak.Spout;

namespace Mox
{
    namespace Spout
    {
        public class SpoutInitializer : MonoBehaviour
        {
            [SerializeField]
            private Camera _mainCamera = null;

            [SerializeField]
            private SpoutSender _spoutSenderPrefab = null;

            [SerializeField]
            private string _spoutSenderBaseName = "MoxSpout";

            [SerializeField]
            private int _unrealSpoutSyncServerPort = 7782;

            private Camera[] _stereoCameras = new Camera[0];
            // private bool _camerasGathered = false;

            private List<SpoutSender> _spoutSenders = new List<SpoutSender>();

            // private bool _connectCommandSent = false;

            [SerializeField]
            private float _desiredStartCommandDelayInS = 0.1f;
            private float _timeSinceStartInS = 0.0f;
            private bool _startCommandSent = false;

            void Start()
            {
                SpoutInitializer[] initializers = FindObjectsByType<SpoutInitializer>(FindObjectsSortMode.None);

                if(initializers.Length <= 1)
                {
                    Sync.TCPSyncClient.SendResetConnectionsCommand(_unrealSpoutSyncServerPort);
                }
                // else this is initializer is probably part of an additive scene, spout connection should be initialized and running already
            }

            void Update()
            {
                if (_spoutSenders.Count > 0
                    && _startCommandSent == false)
                {
                    if (_timeSinceStartInS >= _desiredStartCommandDelayInS)
                    {
                        Debug.Log("Sending Spout Start command");

                        Sync.TCPSyncClient.SendClientConnectCommand(_unrealSpoutSyncServerPort);

                        _startCommandSent = true;
                    }

                    _timeSinceStartInS += Time.deltaTime;
                }
            }

            public void SetupSpoutForCameras(List<Camera> cameras, int startingIndex)
            {
                int senderIdx = startingIndex;

                foreach (Camera camera in cameras)
                {
                    Debug.Log("Setting up spout for camera " + camera.name + " with sender index " + senderIdx);

                    SetupSpoutForCamera(camera, senderIdx);

                    ++senderIdx;
                }
            }

            void GatherCameras()
            {
                if (_mainCamera != null)
                {
                    string mainCamName = _mainCamera.name;
                    Camera[] cameras = _mainCamera.gameObject.GetComponentsInChildren<Camera>();

                    List<Camera> stereoCameras = new List<Camera>();

                    foreach (Camera camera in cameras)
                    {
                        if (camera.name.Contains(mainCamName)
                            && (camera.name.Contains("_left") || camera.name.Contains("_right")))
                        {
                            stereoCameras.Add(camera);
                        }
                    }

                    _stereoCameras = stereoCameras.ToArray();
                }

                // _camerasGathered = true;
            }

            void SetupSpoutSenders()
            {
                if (_spoutSenderPrefab != null)
                {
                    int i = 0;

                    if (_stereoCameras.Length > 0)
                    {

                        foreach (Camera camera in _stereoCameras)
                        {
                            if (camera != _mainCamera)
                            {
                                SpoutSender sender = GameObject.Instantiate(_spoutSenderPrefab);
                                sender.transform.parent = transform;
                                sender.spoutName = _spoutSenderBaseName + i;
                                sender.sourceCamera = camera;

                                _spoutSenders.Add(sender);

                                ++i;
                            }
                        }
                    }
                    else if (_mainCamera != null)
                    {
                        SpoutSender sender = GameObject.Instantiate(_spoutSenderPrefab);
                        sender.transform.parent = transform;
                        sender.spoutName = _spoutSenderBaseName + i;
                        sender.sourceCamera = _mainCamera;

                        _spoutSenders.Add(sender);
                    }
                }
            }

            private void SetupSpoutForCamera(Camera camera, int senderIndex)
            {
                if (_spoutSenderPrefab != null)
                {
                    SpoutSender sender = GameObject.Instantiate(_spoutSenderPrefab);
                    sender.name = _spoutSenderBaseName + "_" + camera.name;
                    sender.transform.parent = transform;
                    sender.spoutName = _spoutSenderBaseName + senderIndex;
                    sender.sourceCamera = camera;

                    _spoutSenders.Add(sender);
                }
            }
        }
    }
}
