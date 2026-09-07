using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Mox
{
    namespace Simulator
    {
        public class SimulatorInterface : MonoBehaviour
        {
            public enum MoxSimulatorMode
            {
                UNKOWN = 0,
                SINGLE,
                CLUSTER
            }

            private static string _hostAddres = "127.0.0.1";

            public static void SendLoadNDisplayConfigCommand(string configPath, int port)
            {
                if (configPath.Length <= 0)
                {
                    Debug.LogError("Empty config path provided, cannot send command");

                    return;
                }

                if (System.IO.File.Exists(configPath) == false)
                {
                    Debug.LogError("File at provided config path does not exist. Make sure to use the absolut path");

                    return;
                }

                string command = "load:" + configPath;
                byte[] bCommand = Encoding.ASCII.GetBytes(command);

                TcpClient tcpClient = Utility.NetworkUtility.ConnectClient(_hostAddres, port);

                if (tcpClient.Connected)
                {
                    NetworkStream stream = tcpClient.GetStream();

                    if (stream.CanWrite)
                    {
                        stream.Write(bCommand);
                    }

                    stream.Close();
                    tcpClient.Close();
                }
                else
                {
                    Debug.LogError("Failed to send 'load' command at " + _hostAddres + ":" + port);
                }
            }

            public static void SendModeCommand(MoxSimulatorMode simMode, int port)
            {
                if (simMode == MoxSimulatorMode.UNKOWN)
                {
                    Debug.LogError("Cannot request simulator mode 'unknown'");

                    return;
                }

                string command = "";

                switch (simMode)
                {
                    case MoxSimulatorMode.SINGLE:
                        command = "mono";
                        break;
                    case MoxSimulatorMode.CLUSTER:
                        command = "simulator";
                        break;
                }

                byte[] bCommand = Encoding.ASCII.GetBytes(command);

                TcpClient tcpClient = Utility.NetworkUtility.ConnectClient(_hostAddres, port);

                if (tcpClient.Connected)
                {
                    NetworkStream stream = tcpClient.GetStream();

                    if (stream.CanWrite)
                    {
                        stream.Write(bCommand);
                    }

                    stream.Close();
                    tcpClient.Close();
                }
                else
                {
                    Debug.LogError("Failed to send 'load' command at " + _hostAddres + ":" + port);
                }
            }
        }
    }
}

