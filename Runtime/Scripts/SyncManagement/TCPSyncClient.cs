// Used to control the establishing of named-pipe connections between Unity and Unreal

using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Mox
{
    namespace Sync
    {
        public class TCPSyncClient
        {
            private static string _hostAddres = "127.0.0.1"; // the unreal instance is always on the same computer

            public static void SendClientConnectCommand(int port)
            {
                string command = "connectClient";
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
                    Debug.LogError("Failed to send 'Connect Client' command at " + _hostAddres + ":" + port);
                }
            }

            public static void SendServerConnectCommand(int port)
            {
                string command = "connectServer";
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
                    Debug.LogError("Failed to send 'Connect Server' command at " + _hostAddres + ":" + port);
                }
            }

            public static void SendResetConnectionsCommand(int port)
            {
                string command = "resetConnections";
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
                    Debug.LogError("Failed to send 'Reset Connection' command at " + _hostAddres + ":" + port);
                }
            }
        }
    }
}


