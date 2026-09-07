using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Mox
{
    namespace Utility
    {
        class ProcessUtility
        {
            [DllImport("user32.dll")]
            private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

            [DllImport("user32.dll")]
            private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            [DllImport("user32.dll")]
            private static extern bool IsWindowVisible(IntPtr hWnd);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

            private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool AttachConsole(uint dwProcessId);

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool FreeConsole();

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

            private const uint WM_CLOSE = 0x0010;

            public static IntPtr GetProcessConsoleWindowHandle(Process process)
            {
                if (process == null || process.HasExited)
                    return IntPtr.Zero;

                IntPtr foundHandle = IntPtr.Zero;
                uint processId = (uint)process.Id;

                EnumWindows((hWnd, lParam) =>
                {
                    GetWindowThreadProcessId(hWnd, out uint windowProcessId);

                    // UnityEngine.Debug.Log("process id: " + windowProcessId);

                    if (windowProcessId == processId /*&& IsWindowVisible(hWnd)*/)
                    {
                        // Check if the window is a console window
                        StringBuilder className = new StringBuilder(256);
                        GetClassName(hWnd, className, className.Capacity);

                        if (className.ToString() == "ConsoleWindowClass"
                            || className.ToString() == "PseudoConsoleWindow")
                        {
                            foundHandle = hWnd;
                            return false; // Stop enumeration
                        }
                    }
                    return true; // Continue enumeration
                }, IntPtr.Zero);

                return foundHandle;
            }

            public static IntPtr GetProcessMainWindowHandle(Process process)
            {
                if (process == null || process.HasExited)
                    return IntPtr.Zero;

                // First try the built-in MainWindowHandle
                if (process.MainWindowHandle != IntPtr.Zero)
                    return process.MainWindowHandle;

                // If that fails, enumerate all windows to find one belonging to this process
                IntPtr foundHandle = IntPtr.Zero;
                uint processId = (uint)process.Id;

                EnumWindows((hWnd, lParam) =>
                {
                    GetWindowThreadProcessId(hWnd, out uint windowProcessId);

                    if (windowProcessId == processId && IsWindowVisible(hWnd))
                    {
                        foundHandle = hWnd;
                        return false; // Stop enumeration
                    }
                    return true; // Continue enumeration
                }, IntPtr.Zero);

                return foundHandle;
            }

            public static bool DetachAndCloseConsole(IntPtr consoleHandle)
            {
                if (consoleHandle == IntPtr.Zero)
                {
                    return false;
                }

                GetWindowThreadProcessId(consoleHandle, out uint processId);
                if (processId == 0)
                {
                    return false;
                }

                // Attach to that console (if possible) and detach again.
                // Detach failure is non-fatal for closing.
                if (AttachConsole(processId))
                {
                    FreeConsole();
                }

                // Request console window close.
                return PostMessage(consoleHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            }
        }
    }
}