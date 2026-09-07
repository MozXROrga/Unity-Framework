// Handles input reported from Unreal and provides a user interface for it

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mox
{
    namespace Sync
    {
        public class Input : MonoBehaviour
        {
            #region Instance

            private static Input _instance = null;

            public static Input Instance
            {
                get { return _instance; }
            }

            #endregion

            #region KeyCodes
            // https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
            public enum KEYCODE
            {
                BACK = 8,
                TAB,
                ENTER = 13,
                SHIFTKEY = 16,
                CONTROLKEY,
                MENU,
                PAUSE,
                CAPSLOCK,
                ESCAPE = 27,
                SPACE = 32,
                PAGEUP,
                PAGEDOWN,
                END,
                HOME,
                LEFT,
                UP,
                RIGHT,
                DOWN,
                SELECT,
                PRINT,
                EXECUTE,
                PRINTSCREEN,
                INSERT,
                DELETE,
                HELP,
                ALPHA_0 = 48,
                ALPHA_1,
                ALPHA_2,
                ALPHA_3,
                ALPHA_4,
                ALPHA_5,
                ALPHA_6,
                ALPHA_7,
                ALPHA_8,
                ALPHA_9,
                A = 65,
                B,
                C,
                D,
                E,
                F,
                G,
                H,
                I,
                J,
                K,
                L,
                M,
                N,
                O,
                P,
                Q,
                R,
                S,
                T,
                U,
                V,
                W,
                X,
                Y,
                Z,
                LWIN,
                RWIN,
                APPS,
                SLEEP = 95,
                NUMPAD_0,
                NUMPAD_1,
                NUMPAD_2,
                NUMPAD_3,
                NUMPAD_4,
                NUMPAD_5,
                NUMPAD_6,
                NUMPAD_7,
                NUMPAD_8,
                NUMPAD_9,
                MULTIPLY,
                ADD,
                SEPARATOR,
                SUBTRACT,
                DECIMAL,
                DIVIDE,
                F1,
                F2,
                F3,
                F4,
                F5,
                F6,
                F7,
                F8,
                F9,
                F10,
                F11,
                F12,
                F13,
                F14,
                F15,
                F16,
                F17,
                F18,
                F19,
                F20,
                F21,
                F22,
                F23,
                F24,
                NUMLOCK = 144,
                SCROLL,
                LSHIFTKEY = 160,
                RSHIFTKEY,
                LCONTROLKEY,
                RCONTROLKEY,
                LMENU,
                RMENU
            }

            // https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
            public enum MOUSECODE
            {
                UNKNOWN = 0,
                LBUTTON = 1,
                RBUTTON,
                MBUTTON = 4,
                XBUTTON1,
                XBUTTON2
            }

            // based on windows game controller setup menu with xbox controller
            public enum GAMEPADCODE
            {
                UNKOWN = -1,
                FACEBOTTOM = 0,
                FACERIGHT,
                FACELEFT,
                FACETOP,
                TRIGGERLEFT,
                TRIGGERRIGHT,
                SPECIALLEFT,
                SPECIALRIGHT,
                STICKLEFT,
                STICKRIGHT,
                DPADDOWN,
                DPADRIGHT,
                DPADLEFT,
                DPADUP
            }

            public enum GAMEPADAXISCDOE
            {
                UNKNOWN = -1,
                STICKLEFTX = 0,
                STICKLEFTY,
                STICKRIGHTX,
                STICKRIGHTY,
                TRIGGERLEFT,
                TRIGGERRIGHT
            }
            #endregion

            #region MoxInputMessages
            [Serializable]
            private struct KeyState
            {

                [SerializeField]
                public UInt64 key;
                [SerializeField]
                public bool state;
            }

            [Serializable]
            private struct KeyboardMessage
            {
                [SerializeField]
                public KeyState[] keyboard;
            }

            [Serializable]
            private struct MouseState
            {
                [SerializeField]
                public int x;
                [SerializeField]
                public int y;
                [SerializeField]
                public UInt16 buttons;
            }

            [Serializable]
            private struct ButtonState
            {
                [SerializeField]
                public UInt64 button;
                [SerializeField]
                public bool state;
            }

            [Serializable]
            private struct Position
            {
                [SerializeField]
                public float x;
                [SerializeField]
                public float y;
            }

            [Serializable]
            private struct MouseMessage
            {
                [SerializeField]
                public ButtonState[] buttons;
                [SerializeField]
                public Position position;
                [SerializeField]
                public Position delta;
            }

            [Serializable]
            private struct Axis
            {
                [SerializeField]
                public UInt64 axis;
                [SerializeField]
                public float value;
            }

            [Serializable]
            private struct GamepadMessage
            {
                [SerializeField]
                public ButtonState[] buttons;

                [SerializeField]
                public Axis[] axes;
            }
            #endregion

            #region MoxInputIPC
            private IntPtr _serverPipeHandle = IntPtr.Zero;

            private Thread _serverPipeThread = null;
            private bool _serverPipeThreadRunning = false;

            [SerializeField]
            private int _unrealInputSyncServerPort = 7780;
            #endregion

            #region MoxInputState
            private Dictionary<KEYCODE, bool> _keyboardState = new Dictionary<KEYCODE, bool>();

            private Dictionary<MOUSECODE, bool> _mouseState = new Dictionary<MOUSECODE, bool>();
            private Vector2 _mousePosition = Vector2.zero;
            private Vector2 _mouseDetla = Vector2.zero;

            private Dictionary<GAMEPADCODE, bool> _gamepadState = new Dictionary<GAMEPADCODE, bool>();
            private Dictionary<GAMEPADAXISCDOE, float> _gamepadAxisState = new Dictionary<GAMEPADAXISCDOE, float>();
            #endregion

            #region MonoBehaviour
            void Start()
            {
                Input[] inputManagers = FindObjectsByType<Input>(FindObjectsSortMode.None);
                if (inputManagers.Length == 1)
                {
                    _instance = this;

                    DontDestroyOnLoad(gameObject);

                    if (SceneSyncManager.IsPrimary == true)
                    {
                        byte[] sb2 = Encoding.ASCII.GetBytes("unityInputPipe\0");
                        LibIPC.InitServerPipe(sb2, ref _serverPipeHandle, false);

                        _serverPipeThreadRunning = true;
                        _serverPipeThread = new Thread(ServerPipeThread);
                        _serverPipeThread.Start();
                    }
                }
                else if (inputManagers.Length > 1)
                {
                    gameObject.SetActive(false);

                    return;
                }
            }

            void Update()
            {

            }

            private void OnDestroy()
            {
                _serverPipeThreadRunning = false;
            }
            #endregion

            #region InputInterface

            public static bool KeyDown(KEYCODE key)
            {
                if (_instance != null)
                {
                    if (_instance._keyboardState.ContainsKey(key))
                    {
                        return _instance._keyboardState[key];
                    }
                }

                return false;
            }

            public static Vector2 MousePosition()
            {
                if (_instance != null)
                {
                    return _instance._mousePosition;
                }

                return Vector2.zero;
            }

            public static Vector2 MouseDelta()
            {
                if (_instance != null)
                {
                    return _instance._mouseDetla;
                }
                return Vector2.zero;
            }

            public static bool MouseButtonDown(MOUSECODE key)
            {
                if (_instance != null)
                {
                    if (_instance._mouseState.ContainsKey(key))
                    {
                        return _instance._mouseState[key];
                    }
                }

                return false;
            }

            public static bool GamepadButtonDown(GAMEPADCODE key)
            {
                if (_instance != null)
                {
                    if (_instance._gamepadState.ContainsKey(key))
                    {
                        return _instance._gamepadState[key];
                    }
                }

                return false;
            }

            public static float GamepadAxisValue(GAMEPADAXISCDOE key)
            {
                if (_instance != null)
                {
                    if (_instance._gamepadAxisState.ContainsKey(key))
                    {
                        return _instance._gamepadAxisState[key];
                    }
                }

                return -1.0f;
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

                // Handle scenario where play mode was stopped before we got to this point
                if (_serverPipeThreadRunning == false)
                {
                    return;
                }

                TCPSyncClient.SendResetConnectionsCommand(_unrealInputSyncServerPort);

                // init server connection
                NativeOverlapped overlapped = new NativeOverlapped();
                int r = LibIPC.CreateOverlappedStruct(ref overlapped);

                r = LibIPC.AcceptConnection(ref _serverPipeHandle, ref overlapped);

                TCPSyncClient.SendClientConnectCommand(_unrealInputSyncServerPort);

                r = LibIPC.CheckOperationFinished(ref _serverPipeHandle, ref overlapped, 1000 / 60);

                int counter = 0;
                while (r != 0
                    && _serverPipeThreadRunning)
                {
                    r = LibIPC.CheckOperationFinished(ref _serverPipeHandle, ref overlapped, 1000 / 60);

                    Debug.Log("AcceptConnection: " + r);

                    counter++;

                    if (counter > 4 && r != 0)
                    {
                        TCPSyncClient.SendResetConnectionsCommand(_unrealInputSyncServerPort);

                        r = LibIPC.DisconnectPipe(ref _serverPipeHandle);
                        r = LibIPC.AcceptConnection(ref _serverPipeHandle, ref overlapped);

                        TCPSyncClient.SendClientConnectCommand(_unrealInputSyncServerPort);

                        counter = 0;
                    }
                }

                Utility.IPCUtility.ReleasePipeConnecting();

                // main loop
                byte[] readBuffer = new byte[2048];
                IntPtr readBytes = IntPtr.Zero;

                while (_serverPipeThreadRunning)
                {
                    LibIPC.PeekPipe(ref _serverPipeHandle, readBuffer, 2048, ref readBytes);

                    if (readBytes.ToInt64() > 0)
                    {
                        LibIPC.ReadPipe(ref _serverPipeHandle, readBuffer, 2048, ref readBytes, ref overlapped);

                        if (readBytes.ToInt64() > 0)
                        {
                            string message = Encoding.ASCII.GetString(readBuffer, 0, readBytes.ToInt32()); // readBuffer.ToString();

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
                                            HandleKeyboardMessage(msg);
                                            break;
                                        case 1:
                                            HandleMouseMessage(msg);
                                            break;
                                        case 2:
                                            HandleGamepadMessage(msg);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }

                LibIPC.CloseServerPipe(ref _serverPipeHandle);
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
            private void HandleKeyboardMessage(string message)
            {
                try
                {
                    KeyboardMessage sm = JsonUtility.FromJson<KeyboardMessage>(message);

                    foreach (KeyState state in sm.keyboard)
                    {
                        if (_keyboardState.ContainsKey((KEYCODE)state.key) == false)
                        {
                            _keyboardState.Add((KEYCODE)state.key, state.state);
                        }
                        else
                        {
                            _keyboardState[(KEYCODE)state.key] = state.state;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to handle keyboard message: " + e.Message);
                }
            }

            private void HandleMouseMessage(string message)
            {
                try
                {
                    MouseMessage mm = JsonUtility.FromJson<MouseMessage>(message);

                    foreach (ButtonState button in mm.buttons)
                    {
                        MOUSECODE mc = ButtonIdxToMouseCode(button.button);

                        if (_mouseState.ContainsKey(mc) == false)
                        {
                            _mouseState.Add(mc, button.state);
                        }
                        else
                        {
                            _mouseState[mc] = button.state;
                        }
                    }

                    _mousePosition = new Vector2(mm.position.x, mm.position.y);
                    _mouseDetla = new Vector2(mm.delta.x, mm.delta.y);
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to handle mouse message: " + e.Message);
                }
            }

            private void HandleGamepadMessage(string message)
            {
                try
                {
                    GamepadMessage gm = JsonUtility.FromJson<GamepadMessage>(message);

                    foreach (ButtonState button in gm.buttons)
                    {
                        if (_gamepadState.ContainsKey((GAMEPADCODE)button.button) == false)
                        {
                            _gamepadState.Add((GAMEPADCODE)button.button, button.state);
                        }
                        else
                        {
                            _gamepadState[(GAMEPADCODE)button.button] = button.state;
                        }
                    }

                    foreach (Axis axis in gm.axes)
                    {
                        if (_gamepadAxisState.ContainsKey((GAMEPADAXISCDOE)axis.axis) == false)
                        {
                            _gamepadAxisState.Add((GAMEPADAXISCDOE)axis.axis, axis.value);
                        }
                        else
                        {
                            _gamepadAxisState[(GAMEPADAXISCDOE)axis.axis] = axis.value;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to handle gamepad message: " + e.Message);
                }
            }

            private MOUSECODE ButtonIdxToMouseCode(UInt64 buttonIdx)
            {
                MOUSECODE result = MOUSECODE.UNKNOWN;

                if (buttonIdx == 0)
                {
                    result = MOUSECODE.LBUTTON;
                }
                else if (buttonIdx == 1)
                {
                    result = MOUSECODE.RBUTTON;
                }
                else if (buttonIdx == 2)
                {
                    result = MOUSECODE.MBUTTON;
                }
                else if (buttonIdx == 3)
                {
                    result = MOUSECODE.XBUTTON1;
                }
                else if (buttonIdx == 4)
                {
                    result = MOUSECODE.XBUTTON2;
                }

                return result;
            }

            #endregion
        }
    }
}
