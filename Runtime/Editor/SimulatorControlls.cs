using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Mox
{
#if UNITY_EDITOR
    namespace Editor
    {
        public class SimulatorControlls : EditorWindow
        {
            // private static Thread _startThread = null;

            // private static bool _isLaunching = false;

            // [MenuItem("Tools/MozXR/Simulator/Play Cluster", false, 1)]
            public static void PlaySimulationCluster()
            {
                GameObject paramObject = new GameObject("MoxSimulationParams", typeof(Mox.Simulator.SimulationParams));
                Mox.Simulator.SimulationParams._destroy = false;

                Debug.Log("Starting Cluster Simulation");

                StartPlay();
            }

            // [MenuItem("Tools/MozXR/Simulator/Play Single", false, 1)]
            public static void PlaySimulationSingle()
            {
                StartPlay();
            }

            private static void StartPlay()
            {
                if (EndpointControlls.IsEndpointRunning() == false)
                {
                    EndpointControlls.StartEndpointIfNotRunning();
                }

                EditorApplication.isPlaying = true;
            }
        }
    }
#endif
}


