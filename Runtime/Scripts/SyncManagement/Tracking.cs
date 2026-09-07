using System.Threading;
using System;
using UnityEngine;
using System.Text;
using System.Collections.Generic;
using System.Collections;
using System.Net.Sockets;
using System.IO;

namespace Mox
{
    namespace Sync
    {
        public class Tracking : MonoBehaviour
        {
            #region Instance
            private static Tracking _instance = null;

            public static Tracking Instance
            {
                get { return _instance; }
            }
            #endregion

            #region MoxPharusMessages
            [Serializable]
            private struct MoxPharusTrackInternal
            {
                [SerializeField]
                public int id;

                [SerializeField]
                public float posX;
                [SerializeField]
                public float posY;
                [SerializeField]
                public float posZ;

                [SerializeField]
                public float rotPitch;
                [SerializeField]
                public float rotYaw;
                [SerializeField]
                public float rotRoll;

                [SerializeField]
                public string instance;
            }

            [Serializable]
            private struct MoxPharusMessage
            {
                [SerializeField]
                public MoxPharusTrackInternal[] pharus;
            }

            public struct MoxPharusTrack
            {
                public int Id;

                public Vector3 Position;
                public Vector3 Rotation;

                public string InstanceName;
            }
            #endregion

            #region MoxOptiTrackMessages
            [Serializable]
            private struct MoxOptiTrackTrackInternal
            {
                [SerializeField]
                public int id;

                [SerializeField]
                public float posX;
                [SerializeField]
                public float posY;
                [SerializeField]
                public float posZ;

                [SerializeField]
                public float rotPitch;
                [SerializeField]
                public float rotYaw;
                [SerializeField]
                public float rotRoll;
            }

            [Serializable]
            private struct MoxOptiTrackMessage
            {
                [SerializeField]
                public MoxOptiTrackTrackInternal[] optiTrack;
            }

            public struct MoxOptiTrackTrack
            {
                public int Id;

                public Vector3 Position;
                public Vector3 Rotation;
            }
            #endregion

            #region MoxPharusConfigData
            [Serializable]
            private struct PharusWallRegionMessage
            {
                [SerializeField]
                public float wallPosX;
                [SerializeField]
                public float wallPosY;
                [SerializeField]
                public float wallPosZ;

                [SerializeField]
                public float wallRotPitch;
                [SerializeField]
                public float wallRotRoll;
                [SerializeField]
                public float wallRotYaw;

                [SerializeField]
                public float wallSizeX;
                [SerializeField]
                public float wallSizeY;
                [SerializeField]
                public float wallSizeZ;

                [SerializeField]
                public float wallOriginX;
                [SerializeField]
                public float wallOriginY;

                [SerializeField]
                public float wallRot2D;

                [SerializeField]
                public float wallScaleX;
                [SerializeField]
                public float wallScaleY;

                [SerializeField]
                public bool invertY;
            }

            [Serializable]
            private struct PharusInstanceMessage
            {
                [SerializeField]
                public string name;

                [SerializeField]
                public float surfaceDimX;
                [SerializeField]
                public float surfaceDimY;

                [SerializeField]
                public float floorRotation;

                [SerializeField]
                public float scaleX;
                [SerializeField]
                public float scaleY;

                [SerializeField]
                public float rootPositionX;
                [SerializeField]
                public float rootPositionY;
                [SerializeField]
                public float rootPositionZ;

                [SerializeField]
                public float rootRotationPitch;
                [SerializeField]
                public float rootRotationRoll;
                [SerializeField]
                public float rootRotationYaw;

                [SerializeField]
                public bool useNormalizedCoords;
                [SerializeField]
                public bool invertY;

                [SerializeField]
                public PharusWallRegionMessage[] wallRegions;
            }

            [Serializable]
            private struct PharusSpaceDataMessage
            {
                [SerializeField]
                public PharusInstanceMessage[] pharusInstances;
            }

            private struct PharusWallRegion
            {
                public Vector3 _position;
                public Vector3 _rotation;
                public Vector3 _size;
                public Vector2 _origin;
                public float _rotation2D;
                public Vector2 _scale;
                public bool _invertY;

                public Bounds _bounds;
            }

            private struct PharusInstance
            {
                public string _instanceName;

                public Vector2 _surfaceDimensions;
                public float _floorRotation;
                public Vector2 _scale;

                public Vector3 _rootPosition;
                public Vector3 _rootRotation;

                public bool useNormalizedCoords;
                public bool invertY;

                public List<PharusWallRegion> _wallRegions;
            }

            private struct PharusSpaceData
            {
                public List<PharusInstance> _instances;
            }

            private static PharusSpaceData _pharusSpaceData;
            private static bool _pharusDataSet = false;
            private static bool _pharusDataRequestPending = false;

            private static Vector2 _pharusFloorDimensionsInM = Vector2.zero;
            private static Vector2 _pharusWallDimensionsInM = Vector2.zero;

            public static bool PharusDataSet
            {
                get { return _pharusDataSet; }
            }

            public static Vector2 PharusFloorDimensionsInM
            {
                get { return _pharusFloorDimensionsInM; }
            }

            public static Vector2 PharusWallDimensionsInM
            {
                get { return _pharusWallDimensionsInM; }
            }
            #endregion

            #region MoxInputIPC
            private IntPtr _serverPipeHandle = IntPtr.Zero;

            private Thread _serverPipeThread = null;
            private bool _serverPipeThreadRunning = false;
            private bool _pipeConnected = false;

            [SerializeField]
            private int _unrealTrackingSyncServerPort = 7781;
            #endregion

            #region MoxTrackingState
            private List<MoxPharusTrack> _pharusTracks = new List<MoxPharusTrack>();

            private List<MoxOptiTrackTrack> _optiTrackTracks = new List<MoxOptiTrackTrack>();
            #endregion

            private int _messageBufferSize = 1024 * 16;

            #region Monobehaviour
            void Start()
            {
                Tracking[] trackingManagers = FindObjectsByType<Tracking>(FindObjectsSortMode.None);
                if (trackingManagers.Length == 1)
                {
                    _instance = this;

                    DontDestroyOnLoad(gameObject);


                    if (SceneSyncManager.IsPrimary == true)
                    {
                        byte[] sb2 = Encoding.ASCII.GetBytes("unityTrackingPipe\0");
                        LibIPC.InitServerPipe(sb2, ref _serverPipeHandle, false);

                        _serverPipeThreadRunning = true;
                        _serverPipeThread = new Thread(ServerPipeThread);
                        _serverPipeThread.Start();
                    }
                }
                else if (trackingManagers.Length > 1)
                {
                    gameObject.SetActive(false);

                    return;
                }
            }

            void Update()
            {
                // only request pharus config stuff after we know that the unreal endpoint is actually there
                if (_pipeConnected == true
                    && _pharusDataSet == false
                    && _pharusDataRequestPending == false)
                {
                    _pharusDataRequestPending = true;
                    StartCoroutine(RequestPharusConfigData());
                }
            }

            private void OnDestroy()
            {
                _serverPipeThreadRunning = false;
            }
            #endregion

            #region TrackingInterface

            public static List<MoxPharusTrack> PharusTracks()
            {
                List<MoxPharusTrack> result = null;

                if (_instance != null)
                {
                    lock (_instance._pharusTracks)
                    {
                        result = new List<MoxPharusTrack>(_instance._pharusTracks);
                    }
                }

                return result;
            }

            public static List<MoxOptiTrackTrack> OptiTrackTracks()
            {
                List<MoxOptiTrackTrack> result = null;

                if (_instance != null)
                {
                    lock (_instance._optiTrackTracks)
                    {
                        result = new List<MoxOptiTrackTrack>(_instance._optiTrackTracks);
                    }
                }

                return result;
            }

            /*
             * 
             */
            public static Vector2 NormalizePharusTrack(MoxPharusTrack track)
            {
                Vector2 result = Vector2.zero;

                if (_pharusDataSet == true)
                {
                    if (track.InstanceName == "Floor")
                    {
                        result = NormalizeFloorTrack(track);
                    }
                    else if (track.InstanceName == "Wall")
                    {
                        result = NormalizeWallTrack(track);
                    }
                }

                return result;
            }

            #endregion

            #region TrackSpaceConversion

            // Pharus Unreal Plugin UMoxPharusInstance::TrackToWorldFloor(...) in reverse
            static private Vector2 NormalizeFloorTrack(MoxPharusTrack track)
            {
                Vector2 result = Vector2.zero;

                if (track.InstanceName != "Floor")
                {
                    Debug.LogWarning("Tried to floor-normalize non-floor track.");

                    return result;
                }

                foreach (PharusInstance instance in _pharusSpaceData._instances)
                {
                    // NOTE: step number correspond to MoxPharusInstance (unreal plugin) code, some numbered steps there are just retrieving data and such

                    if (track.InstanceName == instance._instanceName)
                    {
                        Vector3 position = UnityToUnreal(track.Position);

                        // step 8
                        position = position - instance._rootPosition;

                        // step 7
                        position = Quaternion.Euler(instance._rootRotation * -1.0f) * position;


                        // setp 4
                        float invRot = 360.0f - instance._floorRotation;
                        invRot = invRot / 180.0f * 3.14159f;

                        float tmpX = position.x;
                        float tmpY = position.y;
                        position.x = tmpX * Mathf.Cos(invRot) - tmpY * Mathf.Sin(invRot);
                        position.y = tmpX * Mathf.Sin(invRot) + tmpY * Mathf.Cos(invRot);

                        // step 3
                        position.x /= instance._scale.x;
                        position.y /= instance._scale.y;

                        // step 2
                        if (instance.invertY)
                        {
                            if (instance.useNormalizedCoords)
                            {
                                position.y = 1.0f - position.y;
                            }
                            else
                            {
                                position.y = -position.y;
                            }
                        }

                        // step 1
                        if (instance.useNormalizedCoords == false)
                        {
                            position.x *= instance._surfaceDimensions.x;
                            position.y *= instance._surfaceDimensions.y;
                        }

                        result.x = position.x;
                        result.y = position.y;

                        break;
                    }
                }

                return result;
            }

            // Pharus Unreal Plugin FPharusWallRegion::TrackToWorld(...) in reverse
            static private Vector2 NormalizeWallTrack(MoxPharusTrack track)
            {
                Vector2 result = Vector2.zero;

                if (track.InstanceName != "Wall")
                {
                    Debug.LogWarning("Tried to wall-normalize non-wall track.");

                    return result;
                }

                foreach (PharusInstance instance in _pharusSpaceData._instances)
                {
                    if (track.InstanceName == instance._instanceName)
                    {
                        foreach (PharusWallRegion region in instance._wallRegions)
                        {
                            // NOTE: step number correspond to MoxPharusInstance (unreal plugin) code, some numbered steps there are just retrieving data and such

                            if (region._bounds.Contains(track.Position))
                            {
                                Vector3 position = UnityToUnreal(track.Position);

                                // step 10
                                position = position - instance._rootPosition;

                                // step 9
                                position = Quaternion.Euler(instance._rootRotation * -1.0f) * position;

                                // step 8
                                position -= region._position;

                                // step 7
                                position = Quaternion.Euler(region._rotation * -1.0f) * position;


                                Vector2 position2 = new Vector2(position.x, position.y);


                                // step 5
                                position2 -= region._origin;

                                // step 4
                                float invRot = 360.0f - region._rotation2D;
                                invRot = invRot / 180.0f * 3.14159f;

                                float tmpX = position.x;
                                float tmpY = position.y;
                                position2.x = tmpX * Mathf.Cos(invRot) - tmpY * Mathf.Sin(invRot);
                                position2.y = tmpX * Mathf.Sin(invRot) + tmpY * Mathf.Cos(invRot);

                                // step 3
                                position2 /= region._scale;

                                // step 2
                                if (region._invertY)
                                {
                                    if (instance.useNormalizedCoords)
                                    {
                                        position.y = 1.0f - position.y;
                                    }
                                    else
                                    {
                                        position.y = -position.y;
                                    }
                                }

                                // step 1
                                if (instance.useNormalizedCoords == false)
                                {
                                    position.x *= instance._surfaceDimensions.x;
                                    position.y *= instance._surfaceDimensions.y;
                                }

                                result = position2;

                                break;
                            }
                        }

                        break;
                    }
                }

                return result;
            }
            #endregion

            #region MoxIPCHandling
            private void ServerPipeThread()
            {
                while (Utility.IPCUtility.TryLockPipeConnecting() == false
                    && _serverPipeThreadRunning == true)
                {
                    Thread.Sleep(16); // about 1 frame at 60fps
                }

                // MoxIPCUtility.PipeConnecting = true;

                // Handle scenario where play mode was stopped before we got to this point
                if (_serverPipeThreadRunning == false)
                {
                    return;
                }

                TCPSyncClient.SendResetConnectionsCommand(_unrealTrackingSyncServerPort);

                // init server connection
                NativeOverlapped overlapped = new NativeOverlapped();
                int r = LibIPC.CreateOverlappedStruct(ref overlapped);

                r = LibIPC.AcceptConnection(ref _serverPipeHandle, ref overlapped);

                TCPSyncClient.SendClientConnectCommand(_unrealTrackingSyncServerPort);

                r = LibIPC.CheckOperationFinished(ref _serverPipeHandle, ref overlapped, 1000 / 4);

                int counter = 0;
                while (r != 0
                    && _serverPipeThreadRunning)
                {
                    r = LibIPC.CheckOperationFinished(ref _serverPipeHandle, ref overlapped, 1000 / 4);

                    Debug.Log("AcceptConnection: " + r);

                    counter++;

                    if (counter > 4 && r != 0)
                    {
                        TCPSyncClient.SendResetConnectionsCommand(_unrealTrackingSyncServerPort);

                        r = LibIPC.DisconnectPipe(ref _serverPipeHandle);
                        r = LibIPC.AcceptConnection(ref _serverPipeHandle, ref overlapped);

                        TCPSyncClient.SendClientConnectCommand(_unrealTrackingSyncServerPort);

                        counter = 0;
                    }
                }

                Utility.IPCUtility.ReleasePipeConnecting();

                // main loop
                byte[] readBuffer = new byte[_messageBufferSize];
                IntPtr readBytes = IntPtr.Zero;

                _pipeConnected = true;

                while (_serverPipeThreadRunning)
                {
                    LibIPC.PeekPipe(ref _serverPipeHandle, readBuffer, _messageBufferSize, ref readBytes);

                    if (readBytes.ToInt64() > 0)
                    {
                        LibIPC.ReadPipe(ref _serverPipeHandle, readBuffer, _messageBufferSize, ref readBytes, ref overlapped);

                        if (readBytes.ToInt64() > 0)
                        {
                            string message = Encoding.ASCII.GetString(readBuffer, 0, readBytes.ToInt32());

                            List<string> messages = SeparateInMessages(message);

                            foreach (string m in messages)
                            {
                                string sMsgType = m.Substring(0, 2);
                                // string sMsgLength = message.Substring(2, 10);
                                string msg = m.Substring(12);

                                int iMsgType = -1;
                                if (int.TryParse(sMsgType, out iMsgType))
                                {
                                    switch (iMsgType)
                                    {
                                        case 0:
                                            HandlePharusMessage(msg);
                                            break;
                                        case 1:
                                            HandleOptiTrackMessage(msg);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            private List<string> SeparateInMessages(string rawMessage)
            {
                List<string> result = new List<string>();

                if (rawMessage.Length <= 12)
                {
                    // any valid message should be as long as the header plus at least two brackets
                    return result;
                }

                string sMsgLength = rawMessage.Substring(2, 10);

                int msgLength = 0;
                while (int.TryParse(sMsgLength, out msgLength)
                    && rawMessage.Length >= (msgLength + 12))
                {
                    string m = rawMessage.Substring(0, msgLength + 12);

                    result.Add(m);

                    rawMessage = rawMessage.Substring(msgLength + 12);

                    sMsgLength = "";
                    if (rawMessage.Length > 12)
                    {
                        sMsgLength = rawMessage.Substring(2, 10);
                    }
                }

                return result;
            }
            #endregion

            #region MessageHandling
            private void HandlePharusMessage(string message)
            {
                try
                {
                    MoxPharusMessage sm = JsonUtility.FromJson<MoxPharusMessage>(message);

                    List<MoxPharusTrack> tracks = new List<MoxPharusTrack>();

                    if (sm.pharus != null)
                    {
                        foreach (MoxPharusTrackInternal t in sm.pharus)
                        {
                            MoxPharusTrack track = new MoxPharusTrack();
                            track.Id = t.id;
                            track.Position = UnrealToUnity(t.posX, t.posY, t.posZ); // switch x and z to align with physical space
                            track.Rotation = new Vector3(t.rotPitch, t.rotYaw, t.rotRoll);
                            track.InstanceName = t.instance;


                            // pharus tracks on unreal-side are not rooted at 0/0 but at the corner of the tracking surface
                            // apply this offset to get tracks to origin-relative positions
                            //if (_instanceOffsets.ContainsKey(track.InstanceName))
                            //{
                            //    Vector2 offset = _instanceOffsets[track.InstanceName];

                            //    if(track.InstanceName == "Floor")
                            //    {
                            //        track.Position += new Vector3(offset.x, 0.0f, offset.y);
                            //    }
                            //    else if(track.InstanceName == "Wall")
                            //    {
                            //        track.Position += new Vector3(offset.x, offset.y, 0.0f); // TODO: verify axis order
                            //    }
                            //}


                            tracks.Add(track);

                            //Debug.Log("Track " + track.Id + ": " + track.Position);

                            //for(int i = 0; i < _pharusSpaceData._instances.Count; i++)
                            //{
                            //    if (_pharusSpaceData._instances[i]._instanceName == track.InstanceName)
                            //    {
                            //        if(_pharusSpaceData._instances[i]._wallRegions.Count > 0)
                            //        {
                            //            for(int j = 0; j < _pharusSpaceData._instances[i]._wallRegions.Count; j++)
                            //            {
                            //                bool contains = _pharusSpaceData._instances[i]._wallRegions[j]._bounds.Contains(track.Position);
                            //            }
                            //        }
                            //    }
                            //}
                        }
                    }

                    lock (_pharusTracks)
                    {
                        _pharusTracks.Clear();
                        _pharusTracks = tracks;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to handle pharus message: " + e.Message);
                }
            }

            private void HandleOptiTrackMessage(string message)
            {
                try
                {
                    MoxOptiTrackMessage sm = JsonUtility.FromJson<MoxOptiTrackMessage>(message);

                    List<MoxOptiTrackTrack> tracks = new List<MoxOptiTrackTrack>();

                    if (sm.optiTrack != null)
                    {
                        foreach (MoxOptiTrackTrackInternal t in sm.optiTrack)
                        {
                            MoxOptiTrackTrack track = new MoxOptiTrackTrack();

                            track.Id = t.id;
                            track.Position = UnrealToUnity(t.posX, t.posY, t.posZ);
                            track.Rotation = new Vector3(t.rotPitch, t.rotYaw, t.rotRoll);

                            tracks.Add(track);
                        }
                    }

                    lock (_optiTrackTracks)
                    {
                        _optiTrackTracks.Clear();
                        _optiTrackTracks = tracks;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to handle OptiTrack message: " + e.Message);
                }
            }

            #endregion

            private static IEnumerator RequestPharusConfigData()
            {
                yield return null;

                TcpClient tcpClient = Utility.NetworkUtility.ConnectClient("127.0.0.1", 7785);

                if (tcpClient.Connected)
                {
                    NetworkStream stream = tcpClient.GetStream();

                    if (stream.CanWrite)
                    {
                        stream.Write(Encoding.ASCII.GetBytes("instanceData"));
                    }

                    if (stream.CanRead)
                    {
                        char[] buffer = new char[2048];

                        StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
                        int readBytes = streamReader.Read(buffer, 0, 2048);

                        string response = new string(buffer, 0, readBytes);

                        PharusSpaceDataMessage spaceData = JsonUtility.FromJson<PharusSpaceDataMessage>(response);

                        _pharusSpaceData = new PharusSpaceData();
                        _pharusSpaceData._instances = new List<PharusInstance>();

                        // convert coordinate system (axis swap, scale, etc.)
                        for (int i = 0; i < spaceData.pharusInstances.Length; i++)
                        {
                            PharusInstance instance = new PharusInstance();
                            instance._instanceName = spaceData.pharusInstances[i].name;
                            instance._surfaceDimensions = UnrealToUnity(spaceData.pharusInstances[i].surfaceDimX, spaceData.pharusInstances[i].surfaceDimY);
                            instance._floorRotation = spaceData.pharusInstances[i].floorRotation;
                            instance._scale = new Vector2(spaceData.pharusInstances[i].scaleX, spaceData.pharusInstances[i].scaleY);

                            instance._rootPosition = UnrealToUnity(spaceData.pharusInstances[i].rootPositionX, spaceData.pharusInstances[i].rootPositionY, spaceData.pharusInstances[i].rootPositionZ);
                            instance._rootRotation = new Vector3(spaceData.pharusInstances[i].rootRotationPitch, spaceData.pharusInstances[i].rootRotationYaw, spaceData.pharusInstances[i].rootRotationRoll); // * 0.01f;

                            instance.useNormalizedCoords = spaceData.pharusInstances[i].useNormalizedCoords;
                            instance.invertY = spaceData.pharusInstances[i].invertY;

                            instance._wallRegions = new List<PharusWallRegion>();

                            for (int j = 0; j < spaceData.pharusInstances[i].wallRegions.Length; j++)
                            {
                                PharusWallRegionMessage wr = spaceData.pharusInstances[i].wallRegions[j];

                                PharusWallRegion wallRegion = new PharusWallRegion();
                                wallRegion._position = new Vector3(wr.wallPosX, wr.wallPosZ, wr.wallPosY) * 0.01f;
                                wallRegion._rotation = new Vector3(wr.wallRotPitch, wr.wallRotYaw, wr.wallRotRoll); // TODO: verify roll and yaw need switching (like y and z axis)
                                wallRegion._size = UnrealToUnity(wr.wallSizeX, 0.0f, wr.wallSizeZ);
                                wallRegion._origin = UnrealToUnity(wr.wallOriginX, wr.wallOriginY);
                                wallRegion._rotation2D = wr.wallRot2D;
                                wallRegion._scale = new Vector2(wr.wallScaleX, wr.wallScaleY);

                                Vector3 max = wallRegion._size * 0.5f;
                                Vector3 min = wallRegion._size * -0.5f;

                                max = Quaternion.Euler(wallRegion._rotation.x, wallRegion._rotation.y, wallRegion._rotation.z) * max;
                                min = Quaternion.Euler(wallRegion._rotation.x, wallRegion._rotation.y, wallRegion._rotation.z) * min;

                                wallRegion._bounds.min = wallRegion._position + min;
                                wallRegion._bounds.max = wallRegion._position + max;

                                wallRegion._invertY = wr.invertY;

                                instance._wallRegions.Add(wallRegion);
                            }

                            _pharusSpaceData._instances.Add(instance);

                            Vector2 instanceOffset = new Vector2(instance._surfaceDimensions.x, instance._surfaceDimensions.y) * 100.0f * new Vector2(-0.5f, 0.5f);

                            if (instance._instanceName == "Wall")
                            {
                                _pharusWallDimensionsInM = instance._surfaceDimensions * 100.0f;
                            }
                            else if (instance._instanceName == "floor")
                            {
                                _pharusFloorDimensionsInM = instance._surfaceDimensions * 100.0f;
                            }
                        }

                        _pharusDataSet = true;
                    }

                    stream.Close();
                    tcpClient.Close();
                }

                _pharusDataRequestPending = false;
            }

            private static Vector3 UnrealToUnity(float x, float y, float z)
            {
                return UnrealToUnity(new Vector3(x, y, z));
            }

            private static Vector3 UnrealToUnity(Vector3 unrealVector)
            {
                return (new Vector3(unrealVector.y, unrealVector.z, unrealVector.x) * 0.01f);
            }

            private static Vector2 UnrealToUnity(float x, float y)
            {
                return UnrealToUnity(new Vector2(x, y));
            }

            private static Vector2 UnrealToUnity(Vector2 unrealVector)
            {
                return (unrealVector * 0.01f);
            }



            private static Vector3 UnityToUnreal(float x, float y, float z)
            {
                return UnityToUnreal(new Vector3(x, y, z));
            }

            private static Vector3 UnityToUnreal(Vector3 unityVector)
            {
                return (new Vector3(unityVector.y, unityVector.z, unityVector.x) / 0.01f);
            }

            private static Vector2 UnityToUnreal(float x, float y)
            {
                return UnityToUnreal(new Vector2(x, y));
            }

            private static Vector2 UnityToUnreal(Vector2 unityVector)
            {
                return (unityVector / 0.01f);
            }
        }
    }
}


