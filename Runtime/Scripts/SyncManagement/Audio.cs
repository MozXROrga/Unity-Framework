using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Mox
{
    namespace Sync
    {
        public class Audio : MonoBehaviour
        {
            private static Audio _instance = null;

            private static int _lisaPort = 7786;
            private static int _abletonPort = 7787;

            private static string _hostAddres = "127.0.0.1";

            private IntPtr _clientPipeHandle = IntPtr.Zero;
            private bool _clientConnected = false;

            private Thread _ipcConnectThread = null;
            private bool _threadRunning = false;

            private int _unrealAudioSyncServerPort = 7788;

            private List<LisaPositionXYZMessage> _positionXYZMessages = new List<LisaPositionXYZMessage>();
            private List<LisaPositionAEDMessage> _positionAEDMessages = new List<LisaPositionAEDMessage>();

            private static Mutex _lisaPositionXYZMutex = new Mutex();
            private static Mutex _lisaPositionAEDMutex = new Mutex();

            [Serializable]
            private struct LisaPositionXYZMessage
            {
                [SerializeField]
                public int objectID;
                [SerializeField]
                public float x;
                [SerializeField]
                public float y;
                [SerializeField]
                public float z;
            }

            [Serializable]
            private struct LisaPositionAEDMessage
            {
                [SerializeField]
                public int objectID;
                [SerializeField]
                public float azimuth;
                [SerializeField]
                public float elevation;
                [SerializeField]
                public float distance;
            }

            [Serializable]
            private struct LisaPositionMessage
            {
                [SerializeField]
                public List<LisaPositionXYZMessage> positionXYZMessages;
                [SerializeField]
                public List<LisaPositionAEDMessage> positionAEDMessages;
            }

            private enum MessageType
            {
                LISA_POSITION = 0
            }

            #region Monobehaviour
            public void Start()
            {
                Audio[] audioManagers = FindObjectsByType<Audio>(FindObjectsSortMode.None);
                if (audioManagers.Length == 1)
                {
                    _instance = this;

                    DontDestroyOnLoad(gameObject);


                    if (SceneSyncManager.IsPrimary == true)
                    {
                        _threadRunning = true;
                        _ipcConnectThread = new Thread(ConnectClientPipe);
                        _ipcConnectThread.Start();
                    }
                }
                else if (audioManagers.Length > 1)
                {
                    gameObject.SetActive(false);

                    return;
                }
            }

            public void Update()
            {
                if (_positionXYZMessages.Count > 0
                    || _positionAEDMessages.Count > 0)
                {
                    _lisaPositionXYZMutex.WaitOne();
                    _lisaPositionAEDMutex.WaitOne();

                    LisaPositionMessage message = new LisaPositionMessage
                    {
                        positionXYZMessages = new List<LisaPositionXYZMessage>(_positionXYZMessages),
                        positionAEDMessages = new List<LisaPositionAEDMessage>(_positionAEDMessages)
                    };

                    _positionXYZMessages.Clear();
                    _positionAEDMessages.Clear();

                    _lisaPositionXYZMutex.ReleaseMutex();
                    _lisaPositionAEDMutex.ReleaseMutex();

                    SendMessage(JsonUtility.ToJson(message), MessageType.LISA_POSITION);
                }
            }

            private void OnDestroy()
            {
                _threadRunning = false;
                if (_ipcConnectThread != null)
                {
                    _ipcConnectThread.Join();
                }
            }
            #endregion

            #region IPC
            private void ConnectClientPipe()
            {
                while (Utility.IPCUtility.TryLockPipeConnecting() == false
                    && _threadRunning == true)
                {
                    Thread.Sleep(16); // about 1 frame at 60fps
                }

                // Handle scenario where play mode was stopped before we got to this point
                if (_threadRunning == false)
                {
                    return;
                }

                TCPSyncClient.SendServerConnectCommand(_unrealAudioSyncServerPort);

                byte[] sb = Encoding.ASCII.GetBytes("unityAudioPipe\0");

                LibIPC.InitClientPipe(sb, ref _clientPipeHandle);

                while (_clientPipeHandle.ToInt32() == -1
                    && _threadRunning)
                {
                    Thread.Sleep(1000);

                    LibIPC.InitClientPipe(sb, ref _clientPipeHandle);
                }

                Utility.IPCUtility.ReleasePipeConnecting();

                _clientConnected = true;
            }

            private void SendMessage(string message, MessageType messageType)
            {
                if ((UInt64)_clientPipeHandle.ToInt64() != 0xffffffffffffffff
                    && (UInt64)_clientPipeHandle.ToInt64() != 0x0
                    && _clientConnected == true)
                {
                    message = ((int)messageType).ToString().PadLeft(4, '0') + (message.Length.ToString().PadLeft(10, '0')) + message;

                    byte[] sb = Encoding.ASCII.GetBytes(message);
                    IntPtr bytesWritten = IntPtr.Zero;
                    LibIPC.WritePipe(ref _clientPipeHandle, sb, message.Length, ref bytesWritten);
                }
            }
            #endregion

            #region L-Isa Interface
            public static void SendObjectPositionXYZ(int objectID, Vector3 position)
            {
                if (_instance != null)
                {
                    // _instance.SendMessage(JsonUtility.ToJson(message), MessageType.LISA_POSITION_XYZ);

                    LisaPositionXYZMessage message = new LisaPositionXYZMessage
                    {
                        objectID = objectID,
                        x = position.x,
                        y = position.y,
                        z = position.z
                    };

                    {
                        _lisaPositionXYZMutex.WaitOne();

                        _instance._positionXYZMessages.Add(message);

                        _lisaPositionXYZMutex.ReleaseMutex();
                    }
                }
            }

            public static void SendObjectPositionAED(int objectID, float azimuth, float elevation, float distance)
            {
                if (_instance != null)
                {
                    LisaPositionAEDMessage message = new LisaPositionAEDMessage
                    {
                        objectID = objectID,
                        azimuth = azimuth,
                        elevation = elevation,
                        distance = distance
                    };

                    {
                        _lisaPositionAEDMutex.WaitOne();

                        _instance._positionAEDMessages.Add(message);

                        _lisaPositionAEDMutex.ReleaseMutex();
                    }

                    // _instance.SendMessage(JsonUtility.ToJson(message), MessageType.LISA_POSITION_AED);
                }
            }

            public static void SendObjectWidth(int objectID, float width)
            {
                string message = "sendObjectWidth " + objectID + " " + width;
                SendLisaCommand(message);
            }

            public static void SendObjectPanSpread(int objectID, float panSpread)
            {
                string message = "sendObjectPanSpread " + objectID + " " + panSpread;
                SendLisaCommand(message);
            }

            public static void SendObjectStereoLink(int objectID, bool bLink)
            {
                string message = "sendObjectStereoLink " + objectID + " " + (bLink ? "1" : "0");
                SendLisaCommand(message);
            }

            public static void SendObjectDistance(int objectID, float distance)
            {
                string message = "sendObjectDistance " + objectID + " " + distance;
                SendLisaCommand(message);
            }

            public static void SetMasterGain(float gain)
            {
                string message = "setMasterGain " + gain;
                SendLisaCommand(message);
            }

            public static void SetMasterFaderPosition(float position)
            {
                string message = "setMasterFaderPosition " + position;
                SendLisaCommand(message);
            }

            public static void SetMasterMute(bool bMute)
            {
                string message = "setMasterMute " + (bMute ? "1" : "0");
                SendLisaCommand(message);
            }

            public static void SetReverbGain(float gain)
            {
                string message = "setReverbGain " + gain;
                SendLisaCommand(message);
            }

            public static void SetReverbFaderPosition(float position)
            {
                string message = "setReverbFaderPosition " + position;
                SendLisaCommand(message);
            }

            public static void SetReverbMute(bool bMute)
            {
                string message = "setReverbMute " + (bMute ? "1" : "0");
                SendLisaCommand(message);
            }

            //public static void SetTempoSource(ELisaTempoSource Source)
            //{

            //}

            public static void SetBPM(float bpm)
            {
                string message = "setBPM " + bpm;
                SendLisaCommand(message);
            }

            public static void TapTempo()
            {
                string message = "tapTempo";
                SendLisaCommand(message);
            }
            #endregion

            #region Ableton Interface
            public static void StartSong()
            {
                string message = "startSong";
                SendAbletonCommand(message);
            }

            public static void StopSong()
            {
                string message = "stopSong";
                SendAbletonCommand(message);
            }

            public static void StopAllClips()
            {
                string message = "stopAllClips";
                SendAbletonCommand(message);
            }

            public static void FireScene(int sceneId)
            {
                string message = "fireScene " + sceneId;
                SendAbletonCommand(message);
            }

            public static void FireClip(int trackId, int clipSlot)
            {
                string message = "fireClip " + trackId + " " + clipSlot;
                SendAbletonCommand(message);
            }

            public static void StopClip(int trackId, int clipSlot)
            {
                string message = "stopClip " + trackId + " " + clipSlot;
                SendAbletonCommand(message);
            }

            public static void StopTrack(int trackID)
            {
                string message = "stopTrack " + trackID;
                SendAbletonCommand(message);
            }

            public static void SetTrackVolume(int trackID, float volume)
            {
                string message = "setTrackVolume " + trackID + " " + volume;
                SendAbletonCommand(message);
            }

            public static void SetTrackMute(int trackID, bool bMute)
            {
                string message = "setTrackMute " + trackID + " " + (bMute ? "1" : "0");
                SendAbletonCommand(message);
            }

            public static void SetTrackPanning(int trackID, float panning)
            {
                string message = "setTrackPanning " + trackID + " " + panning;
                SendAbletonCommand(message);
            }

            public static void SetTrackSolo(int trackID, bool bSolo)
            {
                string message = "setTrackSolo " + trackID + " " + (bSolo ? "1" : "0");
                SendAbletonCommand(message);
            }

            public static void SetDeviceParameter(int trackID, int deviceID, int parameterID, float value)
            {
                string message = "setDeviceParameter " + trackID + " " + deviceID + " " + parameterID + " " + value;
                SendAbletonCommand(message);
            }

            public static void SendCustomEvent(string eventPath, List<string> parameters)
            {
                string message = "sendCustomEvent " + eventPath;
                if (parameters != null)
                {
                    foreach (string param in parameters)
                    {
                        message += " " + param;
                    }
                }
                SendAbletonCommand(message);
            }
            #endregion



            public static void SendLisaCommand(string command)
            {
                Utility.NetworkUtility.ConnectClient(_hostAddres, _lisaPort).GetStream().Write(System.Text.Encoding.ASCII.GetBytes(command));
            }

            public static void SendAbletonCommand(string command)
            {
                Utility.NetworkUtility.ConnectClient(_hostAddres, _abletonPort).GetStream().Write(System.Text.Encoding.ASCII.GetBytes(command));
            }
        }
    }
}
