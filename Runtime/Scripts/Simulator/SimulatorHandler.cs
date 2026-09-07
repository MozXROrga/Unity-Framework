#if UNITY_EDITOR

using System.IO;
using System.Threading;
using UnityEngine;

namespace Mox
{
    namespace Simulator
    {
        [DefaultExecutionOrder(80)]
        public class SimulatorHandler : MonoBehaviour
        {
            private int _configPort = 7783;
            private int _modePort = 7784;

            private void Awake()
            {
                if (Application.isEditor)
                {
                    SimulationParams simParams = FindFirstObjectByType<SimulationParams>();


                    if (simParams != null)
                    {
                        SimulatorInterface.SendModeCommand(SimulatorInterface.MoxSimulatorMode.CLUSTER, _modePort);

                        Thread.Sleep(50);

                        string configPath = Editor.EditorConfig.NDisplayConfig;

                        if (!Path.IsPathRooted(configPath))
                        {
                            configPath = Path.GetFullPath(configPath);
                        }

                        SimulatorInterface.SendLoadNDisplayConfigCommand(configPath, _configPort);
                    }
                    else
                    {
                        SimulatorInterface.SendModeCommand(SimulatorInterface.MoxSimulatorMode.SINGLE, _modePort);
                    }
                }
            }
        }
    }
}


#endif