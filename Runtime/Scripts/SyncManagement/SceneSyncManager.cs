// Deals with synchronizing the game scene
// Prepares synchronizable objects with unique IDs during editing

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using System.Web;

namespace Mox
{
    namespace Sync
    {
        [ExecuteInEditMode, DefaultExecutionOrder(98)]
        public class SceneSyncManager : MonoBehaviour
        {
            #region MoxSyncMessages

            [Serializable]
            private struct STrans
            {
                [SerializeField]
                public Vector3 localPos;
                [SerializeField]
                public Vector3 localScale;
                [SerializeField]
                public Vector3 eulerAngles;

                public STrans(Vector3 pos, Vector3 scale, Vector3 angles)
                {
                    localPos = pos;
                    localScale = scale;
                    eulerAngles = angles;
                }
            }

            [Serializable]
            private struct ObjectData
            {
                [SerializeField]
                public string hash;
                [SerializeField]
                public STrans trans;
                [SerializeField]
                public object userData;
                [SerializeField]
                public string userDataString;
            }

            private enum MessageType
            {
                START = 0,
                SYNC,
                SPAWN,
                DESTROY,
                LOAD_SCENE,
                UNLOAD_SCENE,
                STOP,
                READY
            }

            [Serializable]
            private struct StartMessage
            {
            }

            [Serializable]
            private struct ReadyMessage
            {
            }

            [Serializable]
            private struct SyncMessage
            {
                [SerializeField]
                public string[] objectDataStrings;
            }

            [Serializable]
            private struct SpawnMessage
            {
                [SerializeField]
                public string[] prefabGuids;

                [SerializeField]
                public string[] instanceIds;
            }

            [Serializable]
            private struct DestroyMessage
            {
                [SerializeField]
                public string[] guids;
            }

            [Serializable]
            private struct LoadSceneMessage
            {
                [SerializeField]
                public string sceneName;
                [SerializeField]
                public int sceneIndex;
                [SerializeField]
                public bool loadAdditive;
            }

            int _frameCount = 0;

            #endregion

            #region MoxSyncIPC
            private IntPtr _clientPipeHandle = IntPtr.Zero;

            private IntPtr _serverPipeHandle = IntPtr.Zero;

            private Thread _serverPipeThread = null;
            private bool _serverPipeThreadRunning = false;

            [SerializeField]
            private int _readBufferSize = 1024 * 512;

            private bool _clientConnected = false;

            [SerializeField]
            private int _unrealSceneSyncServerPort = 7779;
            #endregion

            #region MoxSyncData
            private Dictionary<string, SyncBehaviour> _syncObjectsById = new Dictionary<string, SyncBehaviour>();

            private ConcurrentQueue<string> _syncMessageQueue = new ConcurrentQueue<string>();
            private string _messageOverflow = "";

            [SerializeField, HideInInspector]
            private List<GameObject> _prefabs = new List<GameObject>();
            [SerializeField, HideInInspector]
            private List<string> _prefabGuids = new List<string>();

            private bool _isPrimary = true;

            public static bool IsPrimary
            {
                get
                {
                    if (_instance != null)
                    {
                        return _instance._isPrimary;
                    }

                    return false;
                }
            }

            private bool _gameHasStarted = false;

            private int _instantiationCounter = 0;
            public delegate void ObjectInstatiatedCallback(SyncBehaviour instatiatedObject);
            private Dictionary<string, ObjectInstatiatedCallback> _instatiationCallbacks = new Dictionary<string, ObjectInstatiatedCallback>();

            private Mutex _sceneLoadingMutex = new Mutex();
            private bool _loadingScene = false;

            private static SceneSyncManager _instance = null;
            #endregion

            #region MonoBehaviour
            // Start is called before the first frame update
            public void Start()
            {
                if (Application.isPlaying)
                {
                    SceneSyncManager[] syncManagers = FindObjectsByType<SceneSyncManager>(FindObjectsSortMode.None);
                    if (syncManagers.Length == 1)
                    {
                        _instance = this;

                        DontDestroyOnLoad(gameObject);
                    }
                    else if (syncManagers.Length > 1)
                    {
                        gameObject.SetActive(false);

                        return;
                    }

                    Time.timeScale = 0.0f;

                    if (Application.isEditor)
                    {
                        _isPrimary = true;
                    }
                    else
                    {
                        string[] cmdArgs = System.Environment.GetCommandLineArgs();

                        Debug.Log("SceneSyncManager: parsing args for primary flag");

                        _isPrimary = false;

                        foreach (string arg in cmdArgs)
                        {
                            Debug.Log("SceneSyncManager:    " + arg);

                            if (arg == "--primary")
                            {
                                _isPrimary = true;
                            }
                        }
                    }

                    if (_isPrimary)
                    {
                        Debug.Log("PRIMARY");
                    }
                    else
                    {
                        Debug.Log("SECONDARY");
                    }

                    SyncBehaviour[] objects = FindObjectsByType<SyncBehaviour>(FindObjectsSortMode.None);

                    foreach (SyncBehaviour obj in objects)
                    {
                        if (_syncObjectsById.ContainsKey(obj._syncId))
                        {
                            Debug.LogError("Duplicate sync ID detected: " + obj._syncId + " assigning new ID, Cluster-Sync might be defective for this object");

                            string objectPath = GetScenePath(obj.gameObject);
                            obj._syncId = GetStringHash(objectPath);
                        }

                        obj.PrepareForSync(_isPrimary);
                        _syncObjectsById.Add(obj._syncId, obj);

                        obj.SyncInitDoneInternal();
                    }


                    byte[] sb2 = Encoding.ASCII.GetBytes("unityPipe2\0");
                    LibIPC.InitServerPipe(sb2, ref _serverPipeHandle, false);

                    _serverPipeThreadRunning = true;
                    _serverPipeThread = new Thread(ServerPipeThread);
                    _serverPipeThread.Name = "SceneSyncThread";
                    _serverPipeThread.Start();

                    SceneManager.sceneLoaded += OnSceneLoaded;
                    SceneManager.sceneUnloaded += OnSceneUnloaded;
                }
            }

            public void OnDestroy()
            {
                if (Application.isPlaying)
                {
                    if (_serverPipeThread != null)
                    {
                        _serverPipeThreadRunning = false;
                        _serverPipeThread.Join();
                    }
                    LibIPC.CloseClientPipe(ref _clientPipeHandle);
                }
            }

            // Update is called once per frame
            public void Update()
            {
                if (Application.isPlaying)
                {
                    if (_gameHasStarted == false)
                    {
                        // if(_isPrimary)
                        {
                            ReportReady();
                        }
                    }
                    else
                    {
                        ++_frameCount;

                        // reset framecount after 24h at 60fps
                        if (_frameCount > 5184000)
                        {
                            _frameCount = 0;
                        }

                        SerializeSyncObjects();
                    }

                    HandleMessageBuffer();
                }
                else
                {
                    UpdateSyncIds();

                    GatherPrefabs();
                }
            }

            #endregion

            #region MoxSyncInterface
            public static void InstantiatePrefab(GameObject prefab, ObjectInstatiatedCallback callback)
            {
                if (_instance != null)
                {
                    _instance.InstantiatePrefabInternal(prefab, callback);
                }
            }

            private void InstantiatePrefabInternal(GameObject prefab, ObjectInstatiatedCallback callback)
            {
                int idx = _prefabs.IndexOf(prefab);
                if (idx > -1)
                {
                    string guid = _prefabGuids[idx];

                    SpawnMessage message = new SpawnMessage();
                    message.prefabGuids = new string[1];
                    message.prefabGuids[0] = guid;
                    message.instanceIds = new string[1];
                    message.instanceIds[0] = GetStringHash(DateTime.Now.ToString("yyMMddHHmmssfff") + _instantiationCounter);
                    ++_instantiationCounter;

                    SendMessage(JsonUtility.ToJson(message), MessageType.SPAWN);

                    _instatiationCallbacks.Add(message.instanceIds[0], callback);
                }
            }

            public static void RemoveSyncObject(string id)
            {
                if (_instance != null)
                {
                    _instance.RemoveSyncObjectInternal(id);
                }
            }

            private void RemoveSyncObjectInternal(string id)
            {
                if (_syncObjectsById.ContainsKey(id))
                {
                    DestroyMessage destroyMessage = new DestroyMessage();
                    destroyMessage.guids = new string[1];
                    destroyMessage.guids[0] = id;

                    SendMessage(JsonUtility.ToJson(destroyMessage), MessageType.DESTROY);
                }
            }

            public static void LoadScene(string sceneName, bool loadAdditive)
            {
                if (_instance != null)
                {
                    _instance.LoadSceneInternal(sceneName, loadAdditive);
                }
            }

            public static void LoadScene(int sceneIdx, bool loadAdditive)
            {
                if (_instance != null)
                {
                    _instance.LoadSceneInternal(sceneIdx, loadAdditive);
                }
            }

            private void LoadSceneInternal(string sceneName, bool loadAdditive)
            {
                _sceneLoadingMutex.WaitOne();

                if (_loadingScene == false)
                {
                    _loadingScene = true;

                    Debug.Log("LoadSceneInternal " + sceneName);

                    LoadSceneMessage message = new LoadSceneMessage();
                    message.sceneName = sceneName;
                    message.sceneIndex = -1;
                    message.loadAdditive = loadAdditive;

                    SendMessage(JsonUtility.ToJson(message), MessageType.LOAD_SCENE);
                }

                _sceneLoadingMutex.ReleaseMutex();
            }

            private void LoadSceneInternal(int sceneIdx, bool loadAdditive)
            {
                _sceneLoadingMutex.WaitOne();

                if (_loadingScene == false)
                {
                    _loadingScene = true;

                    LoadSceneMessage message = new LoadSceneMessage();
                    message.sceneName = "";
                    message.sceneIndex = sceneIdx;
                    message.loadAdditive = loadAdditive;

                    SendMessage(JsonUtility.ToJson(message), MessageType.LOAD_SCENE);
                }

                _sceneLoadingMutex.ReleaseMutex();
            }

            public static void UnloadScene(string sceneName)
            {
                if (_instance != null)
                {
                    _instance.UnloadSceneInternal(sceneName);
                }
            }

            public static void UnloadScene(int sceneIdx)
            {
                if (_instance != null)
                {
                    _instance.UnloadSceneInternal(sceneIdx);
                }
            }

            private void UnloadSceneInternal(string sceneName)
            {
                LoadSceneMessage message = new LoadSceneMessage();
                message.sceneName = sceneName;
                message.sceneIndex = -1;
                message.loadAdditive = false;

                SendMessage(JsonUtility.ToJson(message), MessageType.UNLOAD_SCENE);
            }

            private void UnloadSceneInternal(int sceneIdx)
            {
                LoadSceneMessage message = new LoadSceneMessage();
                message.sceneName = "";
                message.sceneIndex = sceneIdx;
                message.loadAdditive = false;

                SendMessage(JsonUtility.ToJson(message), MessageType.UNLOAD_SCENE);
            }
            #endregion

            #region MoxOutMessageHandling

            private void ReportReady()
            {
                ReadyMessage readyMessage = new ReadyMessage();

                SendMessage(JsonUtility.ToJson(readyMessage), MessageType.READY);
            }

            private void SerializeSyncObjects()
            {
                // MoxSyncBehaviour[] objects = FindObjectsOfType<MoxSyncBehaviour>();

                SyncMessage syncMessage = new SyncMessage();
                syncMessage.objectDataStrings = new string[_syncObjectsById.Count];

                int i = 0;
                foreach (KeyValuePair<string, SyncBehaviour> obj in _syncObjectsById)
                {
                    string jData = SerializeSyncObject(obj.Value);

                    syncMessage.objectDataStrings[i] = jData;
                    ++i;
                }

                string jObjects = JsonUtility.ToJson(syncMessage);

                // Debug.Log("Sending Sync Message: " + _syncObjectsById.Count);

                SendMessage(jObjects, MessageType.SYNC);
            }

            private string SerializeSyncObject(SyncBehaviour obj)
            {
                string result = "";

                ObjectData objectData = SyncObjectToObjectData(obj);

                result = JsonUtility.ToJson(objectData);

                return result;
            }

            private ObjectData SyncObjectToObjectData(SyncBehaviour obj)
            {
                ObjectData result = new ObjectData();
                result.hash = obj._syncId;
                result.trans = new STrans(
                    obj.transform.localPosition,
                    obj.transform.localScale,
                    obj.transform.eulerAngles);
                result.userData = obj.GetUserDataInternal();
                result.userDataString = JsonUtility.ToJson(result.userData);
                return result;
            }

            private void SendMessage(string message, MessageType messageType)
            {
                if ((UInt64)_clientPipeHandle.ToInt64() != 0xffffffffffffffff
                    && (UInt64)_clientPipeHandle.ToInt64() != 0x0
                    && _clientConnected == true)
                {
                    message = ((int)messageType).ToString().PadLeft(4, '0') + (message.Length.ToString().PadLeft(10, '0')) + (_frameCount.ToString().PadLeft(7, '0')) + message;

                    // Debug.Log("Sending Sync Message: " + message);

                    byte[] sb = Encoding.ASCII.GetBytes(message);
                    IntPtr bytesWritten = IntPtr.Zero;
                    LibIPC.WritePipe(ref _clientPipeHandle, sb, message.Length, ref bytesWritten);
                }
            }

            #endregion

            #region MoxInMessageHandling

            private void HandleMessageBuffer()
            {
                while (_syncMessageQueue.Count > 0)
                {
                    string jMessage = "";
                    _syncMessageQueue.TryDequeue(out jMessage);

                    if (_messageOverflow.Length > 0)
                    {
                        jMessage = _messageOverflow + jMessage;
                        _messageOverflow = "";
                    }

                    string sMsgType = jMessage.Substring(0, 4);
                    string sMsgLength = jMessage.Substring(4, 10);
                    jMessage = jMessage.Substring(21);

                    int messageLength = 0;

                    if (int.TryParse(sMsgLength, out messageLength))
                    {
                        if (jMessage.Length > messageLength)
                        {
                            _messageOverflow = jMessage.Substring(messageLength);
                            jMessage = jMessage.Substring(0, messageLength);
                        }

                        int iMsgType = -1;
                        if (int.TryParse(sMsgType, out iMsgType))
                        {
                            MessageType msgType = (MessageType)iMsgType;

                            switch (msgType)
                            {
                                case MessageType.START:
                                    DoStartTime(jMessage);
                                    break;
                                case MessageType.SYNC:
                                    DeserializeSyncObjects(jMessage);
                                    break;
                                case MessageType.SPAWN:
                                    DoInstantiatePrefab(jMessage);
                                    break;
                                case MessageType.DESTROY:
                                    DoDestroySyncObjects(jMessage);
                                    break;
                                case MessageType.LOAD_SCENE:
                                    DoLoadScene(jMessage);
                                    break;
                                case MessageType.UNLOAD_SCENE:
                                    DoUnloadScene(jMessage);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to parse message header");
                    }
                }
            }

            private void DoStartTime(string startMessage)
            {
                Debug.Log("MoxSceneSyncManager: start command received");

                Time.timeScale = 1.0f;

                _gameHasStarted = true;
            }

            private void DeserializeSyncObjects(string syncMessage)
            {
                SyncMessage sm = JsonUtility.FromJson<SyncMessage>(syncMessage);

                foreach (string jObject in sm.objectDataStrings)
                {
                    ObjectData objData = DeserializeSyncObject(jObject);

                    if (_syncObjectsById.ContainsKey(objData.hash))
                    {
                        _syncObjectsById[objData.hash].internalTransform.localPosition = objData.trans.localPos;
                        _syncObjectsById[objData.hash].internalTransform.localScale = objData.trans.localScale;
                        _syncObjectsById[objData.hash].internalTransform.localEulerAngles = objData.trans.eulerAngles;

                        // GameObject po = GameObject.Find("presentation_" + _syncObjectsById[objData.hash].name);

                        // po.transform.position = objData.trans.localPos;

                        // Debug.Log(po.name + ": " + po.transform.position);

                        _syncObjectsById[objData.hash].SetUserDataInternal(objData.userData);
                    }
                }
            }

            private ObjectData DeserializeSyncObject(string jsonObject)
            {
                ObjectData result = new ObjectData();

                result = JsonUtility.FromJson<ObjectData>(jsonObject);

                if (result.userDataString.Length > 0
                    && _syncObjectsById.ContainsKey(result.hash))
                {
                    object userData = JsonUtility.FromJson(result.userDataString, _syncObjectsById[result.hash].GetUserDataTypeInternal());

                    result.userData = userData;
                }

                return result;
            }

            private void DoInstantiatePrefab(string spawnMessage)
            {
                SpawnMessage sm = JsonUtility.FromJson<SpawnMessage>(spawnMessage);

                foreach (string guid in sm.prefabGuids)
                {
                    int idx = _prefabGuids.IndexOf(guid);

                    if (idx > -1)
                    {
                        GameObject go = GameObject.Instantiate(_prefabs[idx]);

                        if (go.GetComponent<SyncBehaviour>())
                        {
                            SyncBehaviour sb = go.GetComponent<SyncBehaviour>();

                            string objectPath = GetScenePath(go);
                            string objectId = sm.instanceIds[0];

                            sb._syncId = objectId;

                            _syncObjectsById[objectId] = sb;

                            sb.PrepareForSync(_isPrimary);

                            sb.SyncInitDoneInternal();

                            if (_instatiationCallbacks.ContainsKey(objectId))
                            {
                                _instatiationCallbacks[objectId](sb);
                                _instatiationCallbacks.Remove(objectId);
                            }
                        }
                    }
                }
            }

            private void DoDestroySyncObjects(string destroyMessage)
            {
                DestroyMessage dm = JsonUtility.FromJson<DestroyMessage>(destroyMessage);

                foreach (string guid in dm.guids)
                {
                    if (_syncObjectsById.ContainsKey(guid))
                    {
                        SyncBehaviour syncObject = _syncObjectsById[guid];

                        if (syncObject != null) // if it's null it was already destroyed, Unity apparently handles nulls the reference...
                        {
                            Destroy(syncObject.gameObject);
                        }

                        _syncObjectsById.Remove(guid);
                    }
                }
            }

            private void DoLoadScene(string loadSceneMessage)
            {
                _sceneLoadingMutex.WaitOne();


                Debug.Log(gameObject.name + " - DoLoadScene");

                LoadSceneMessage lsm = JsonUtility.FromJson<LoadSceneMessage>(loadSceneMessage);

                LoadSceneMode mode = LoadSceneMode.Single;
                if (lsm.loadAdditive)
                {
                    mode = LoadSceneMode.Additive;
                }

                if (lsm.sceneName.Length > 0)
                {
                    SceneManager.LoadScene(lsm.sceneName, mode);
                }
                else if (lsm.sceneIndex > -1)
                {
                    SceneManager.LoadScene(lsm.sceneIndex, mode);
                }

                _loadingScene = false;


                _sceneLoadingMutex.ReleaseMutex();
            }

            private void DoUnloadScene(string unloadSceneMessage)
            {
                Debug.Log(gameObject.name + " - DoUnloadScene");

                LoadSceneMessage lsm = JsonUtility.FromJson<LoadSceneMessage>(unloadSceneMessage);

                if (lsm.sceneName.Length > 0)
                {
                    SceneManager.UnloadSceneAsync(lsm.sceneName);
                }
                else if (lsm.sceneIndex > -1)
                {
                    SceneManager.UnloadSceneAsync(lsm.sceneIndex);
                }
            }
            #endregion

            #region MoxIPC

            private void ServerPipeThread()
            {
                while (Utility.IPCUtility.TryLockPipeConnecting() == false
                    && _serverPipeThreadRunning == true)
                {
                    Thread.Sleep(16); // about 1 frame at 60fps
                }

                // Handle scenario where play mode was stopped before we got to this point
                if (_serverPipeThreadRunning == false)
                {
                    return;
                }

                TCPSyncClient.SendResetConnectionsCommand(_unrealSceneSyncServerPort);

                // init server connection
                NativeOverlapped overlapped = new NativeOverlapped();
                int result = LibIPC.CreateOverlappedStruct(ref overlapped);

                result = LibIPC.AcceptConnection(ref _serverPipeHandle, ref overlapped);

                TCPSyncClient.SendClientConnectCommand(_unrealSceneSyncServerPort);

                result = LibIPC.CheckOperationFinished(ref _serverPipeHandle, ref overlapped, 1000 / 60);

                int counter = 0;
                while (result != 0
                    && _serverPipeThreadRunning)
                {
                    result = LibIPC.CheckOperationFinished(ref _serverPipeHandle, ref overlapped, 1000 / 60);

                    counter++;
                    if (counter > 4 && result != 0)
                    {
                        TCPSyncClient.SendResetConnectionsCommand(_unrealSceneSyncServerPort);

                        result = LibIPC.DisconnectPipe(ref _serverPipeHandle);
                        result = LibIPC.AcceptConnection(ref _serverPipeHandle, ref overlapped);

                        TCPSyncClient.SendClientConnectCommand(_unrealSceneSyncServerPort);

                        counter = 0;
                    }
                }



                // init client pipe
                byte[] sb = Encoding.ASCII.GetBytes("unityPipe\0");

                TCPSyncClient.SendServerConnectCommand(_unrealSceneSyncServerPort);

                LibIPC.InitClientPipe(sb, ref _clientPipeHandle);

                while (_clientPipeHandle.ToInt32() == -1
                    && _serverPipeThreadRunning)
                {
                    System.Threading.Thread.Sleep(1000);

                    result = LibIPC.InitClientPipe(sb, ref _clientPipeHandle);
                }

                Thread.Sleep(100);

                _clientConnected = true;

                Utility.IPCUtility.ReleasePipeConnecting();


                byte[] readBuffer = new byte[_readBufferSize];
                IntPtr readBytes = IntPtr.Zero;

                Debug.Log("Scene Sync connected");

                while (_serverPipeThreadRunning)
                {
                    LibIPC.PeekPipe(ref _serverPipeHandle, readBuffer, _readBufferSize, ref readBytes);

                    if (readBytes.ToInt64() > 0)
                    {
                        LibIPC.ReadPipe(ref _serverPipeHandle, readBuffer, _readBufferSize, ref readBytes, ref overlapped);

                        string message = Encoding.ASCII.GetString(readBuffer, 0, readBytes.ToInt32());

                        List<string> messages = SeparateInMessages(message);

                        foreach (string m in messages)
                        {
                            _syncMessageQueue.Enqueue(m);
                        }
                    }
                }

                LibIPC.CloseServerPipe(ref _serverPipeHandle);
                LibIPC.CloseClientPipe(ref _clientPipeHandle);
            }

            #endregion

            #region MoxSyncUtility

            private List<string> SeparateInMessages(string rawMessage)
            {
                List<string> result = new List<string>();

                if (rawMessage.Length <= 21)
                {
                    return result;
                }

                string sMsgLength = rawMessage.Substring(4, 10);

                int msgLength = 0;
                while (int.TryParse(sMsgLength, out msgLength)
                    && rawMessage.Length >= (msgLength + 21))
                {
                    string m = rawMessage.Substring(0, msgLength + 21);

                    result.Add(m);

                    rawMessage = rawMessage.Substring(msgLength + 21);

                    sMsgLength = "";
                    if (rawMessage.Length > 21)
                    {
                        sMsgLength = rawMessage.Substring(4, 10);
                    }
                }

                return result;
            }

            private void GatherPrefabs()
            {
#if UNITY_EDITOR // AssetDatabase causes build errors otherwise
                string[] guids = AssetDatabase.FindAssets("t:Prefab");

                if (guids.Length > 0)
                {
                    _prefabs.Clear();
                    _prefabGuids.Clear();

                    foreach (string guid in guids)
                    {
                        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                        SyncBehaviour sb = prefab.GetComponent<SyncBehaviour>();

                        if (sb != null)
                        {
                            _prefabs.Add(prefab);
                            _prefabGuids.Add(guid);
                        }
                    }
                }
#endif
            }

            private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
            {
                Debug.Log(gameObject.name + " - OnSceneLoaded - " + SceneManager.GetActiveScene().name);

                if (_instance != null
                    && _instance != this)
                {
                    return;
                }

                if (mode == LoadSceneMode.Single)
                {
                    _syncObjectsById.Clear();
                }

                SyncBehaviour[] objects = FindObjectsByType<SyncBehaviour>(FindObjectsSortMode.None);

                foreach (SyncBehaviour obj in objects)
                {
                    if (_syncObjectsById.ContainsKey(obj._syncId) == false)
                    {
                        obj.PrepareForSync(_isPrimary);
                        _syncObjectsById.Add(obj._syncId, obj);

                        obj.SyncInitDoneInternal();
                    }
                }

                // remove event system, audio listeners, maybe cameras?
                if(mode == LoadSceneMode.Additive)
                {
                    DisableEventSystemAudioListener(scene);
                }
            }

            private void DisableEventSystemAudioListener(Scene scene)
            {
                GameObject[] rootObjects = scene.GetRootGameObjects();

                foreach (GameObject rootObject in rootObjects)
                {
                    // event system
                    UnityEngine.EventSystems.EventSystem eventSystem = rootObject.GetComponent<UnityEngine.EventSystems.EventSystem>();
                    if (eventSystem != null)
                    {
                        Debug.Log("Found EventSystem as root object in additively loaded scene: " + eventSystem.gameObject.name);
                        Destroy(eventSystem.gameObject);
                    }
                    else
                    {
                        eventSystem = rootObject.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>();
                        if (eventSystem != null)
                        {
                            Debug.Log("Found EventSystem in children of additively loaded scene: " + eventSystem.gameObject.name);
                            Destroy(eventSystem.gameObject);
                        }
                    }

                    // audio listener
                    AudioListener audioListener = rootObject.GetComponent<AudioListener>();
                    if (audioListener != null)
                    {
                        Debug.Log("Found AudioListener as root object in additively loaded scene: " + audioListener.gameObject.name);
                        Destroy(audioListener.gameObject);
                    }
                    else
                    {
                        audioListener = rootObject.GetComponentInChildren<AudioListener>();
                        if (audioListener != null)
                        {
                            Debug.Log("Found AudioListener in children of additively loaded scene: " + audioListener.gameObject.name);
                            Destroy(audioListener.gameObject);
                        }
                    }
                }
            }

            private void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
            {
                Debug.Log(gameObject.name + " - OnSceneUnloaded - " + SceneManager.GetActiveScene().name);

                SyncBehaviour[] objects = FindObjectsByType<SyncBehaviour>(FindObjectsSortMode.None);

                List<string> entriesToRemove = new List<string>();
                foreach (KeyValuePair<string, SyncBehaviour> kvp in _syncObjectsById)
                {
                    if (objects.Contains(kvp.Value) == false)
                    {
                        Debug.Log(gameObject.name + " - removing \"" + kvp.Key + "\" from sync objects list");

                        entriesToRemove.Add(kvp.Key);
                    }
                }

                foreach (string key in entriesToRemove)
                {
                    _syncObjectsById.Remove(key);
                }
            }

            private void UpdateSyncIds()
            {
                SyncBehaviour[] objects = FindObjectsByType<SyncBehaviour>(FindObjectsSortMode.None);

                foreach (SyncBehaviour obj in objects)
                {
                    if (obj._syncId == string.Empty)
                    {
                        string objectPath = GetScenePath(obj.gameObject);
                        string objectId = GetStringHash(objectPath);

                        obj._syncId = objectId;
                    }
                }
            }

            private string GetScenePath(GameObject obj)
            {
                string result = obj.name + "_" + DateTime.Now.ToString("yyMMddHHmmssfff");

                Transform trans = obj.transform;
                int siblingIndex = trans.GetSiblingIndex();
                while (trans.parent != null)
                {
                    result = trans.parent.gameObject.name + "/" + result;

                    trans = trans.parent;
                }

                result += "(" + siblingIndex + ")";

                return result;
            }

            private string GetStringHash(string input)
            {
                string result = "";

                HashAlgorithm hash = SHA256.Create();
                result = System.Text.Encoding.ASCII.GetString(hash.ComputeHash(Encoding.UTF8.GetBytes(input)));

                result = HttpUtility.JavaScriptStringEncode(result);

                return result;
            }

            #endregion
        }
    }
}

