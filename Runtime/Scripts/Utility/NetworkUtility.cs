using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Mox
{
    namespace Utility
    {
        public class NetworkUtility : MonoBehaviour
        {
            public static string GetNetworkIP()
            {
                string result = "";

                string hostName = System.Net.Dns.GetHostName();
                IPHostEntry hostEntry = System.Net.Dns.GetHostEntry(hostName);

                result = hostEntry.AddressList[hostEntry.AddressList.Length - 1].ToString(); // the last one is always the best one, right?

                return result;
            }

            public static TcpClient ConnectClient(string host, int port)
            {
                TcpClient tcpClient = new TcpClient();

                try
                {
                    tcpClient.ConnectAsync(host, port).Wait(3000);
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to connect client at " + host + ":" + port + ": " + e.Message);
                }

                return tcpClient;
            }
        }
    }
}

