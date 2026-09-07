using System.Diagnostics;
using UnityEngine;

namespace Mox
{
    namespace Editor
    {
        public class EditorProcessUtility : MonoBehaviour
        {
            public static Process LaunchProcess(string directory, string exeName, string arguments)
            {
                Process result = null;

                directory = directory.Replace('\\', '/');

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = exeName;
                startInfo.WorkingDirectory = directory;
                startInfo.Arguments = arguments;
                result = Process.Start(startInfo);

                return result;
            }
        }
    }
}
