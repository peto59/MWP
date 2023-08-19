using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Android.App;
using Android.Content;
using Java.Lang;
using Xamarin.Essentials;
using NetworkAccess = Xamarin.Essentials.NetworkAccess;
#if DEBUG
using Ass_Pain.Helpers;
#endif

namespace Ass_Pain.BackEnd.Network
{
    internal class NetworkManagerCommon
    {
        internal static List<IPAddress> Connected = new List<IPAddress>();
        private IPAddress myIp;
        internal bool CanSend; //false
        private IPAddress broadcastIp;
        private const int BroadcastPort = 8008;
        internal const int RsaDataSize = 256;
        
        private static readonly Android.Net.Wifi.WifiManager WifiManager = (Android.Net.Wifi.WifiManager)Application.Context.GetSystemService(Context.WifiService);
        private static readonly Android.Net.DhcpInfo DhcpInfo = WifiManager.DhcpInfo;
        
        internal static void P2PDecide(EndPoint groupEp, IPAddress targetIp, Socket sock)
        {
            EndPoint endPoint = groupEp;
            byte[] buffer = new byte[4];
            while (true)
            {
                int state = new Random().Next(0, 2);
                sock.SendTo(BitConverter.GetBytes(state), groupEp);
#if DEBUG
                MyConsole.WriteLine($"sending {state} to {((IPEndPoint)groupEp).Address}");
#endif
                sock.ReceiveFrom(buffer, ref endPoint);
#if DEBUG
                MyConsole.WriteLine($"received {BitConverter.ToInt32(buffer)} from {((IPEndPoint)endPoint).Address}");
                MyConsole.WriteLine($"debug: {((IPEndPoint)endPoint).Address}  {targetIp}  {!((IPEndPoint)endPoint).Address.Equals(targetIp)}");
#endif
                while (!((IPEndPoint)endPoint).Address.Equals(targetIp))
                {
                    sock.ReceiveFrom(buffer, ref endPoint);
                }
                int resp = BitConverter.ToInt32(buffer);
                if (resp is not (0 or 1))
                {
                    Console.WriteLine("Got invalid state in P2PDecide. Exiting!");
                    return;
                }

                if (state == resp) continue;
                if (state == 0)
                {
                    //server
#if DEBUG
                    MyConsole.WriteLine("Server");
#endif
                    (TcpListener server, int listenPort) = NetworkManagerServer.StartServer(NetworkManager.Common.myIp);
                    sock.SendTo(BitConverter.GetBytes(listenPort), groupEp);
                    new Thread(() => { NetworkManagerServer.Server(server, targetIp); }).Start();
                }
                else
                {
                    //client
#if DEBUG
                    MyConsole.WriteLine("Client");
#endif
                    sock.ReceiveFrom(buffer, ref groupEp);
                    int sendPort = BitConverter.ToInt32(buffer);
                    new Thread(() => { NetworkManagerClient.Client(((IPEndPoint)groupEp).Address, sendPort); }).Start();
                }
                return;
            }
        }

        private static IPAddress GetBroadCastIp(IPAddress host, IPAddress mask)
        {
            byte[] broadcastIpBytes = new byte[4];
            byte[] hostBytes = host.GetAddressBytes();
            byte[] maskBytes = mask.GetAddressBytes();
            for (int i = 0; i < 4; i++)
            {
                broadcastIpBytes[i] = (byte)(hostBytes[i] | (byte)~maskBytes[i]);
            }
            return new IPAddress(broadcastIpBytes);
        }
        
        internal static (string pubKeyString, RSAParameters privKey) CreateKeyPair()
        {
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(2048);
            string pubKey = csp.ToXmlString(false);
            RSAParameters privKey = csp.ExportParameters(true);
            csp.Dispose();
            return (pubKey, privKey);
        }
        
        internal void SendBroadcast()
        {
            if (CanSend)
            {
#if DEBUG
                MyConsole.WriteLine($"My IP IS: {myIp}");
                MyConsole.WriteLine($"My MASK IS: {new IPAddress(BitConverter.GetBytes(DhcpInfo.Netmask))}");
                MyConsole.WriteLine($"My BROADCAST IS: {GetBroadCastIp(myIp, new IPAddress(BitConverter.GetBytes(DhcpInfo.Netmask)))}" );
#endif


                
                IPEndPoint destinationEndpoint = new IPEndPoint(broadcastIp, BroadcastPort);
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                sock.ReceiveTimeout = 2000;
                byte[] buffer = new byte[256];

                int retries = 0;
                const int maxRetries = 3;

                IPEndPoint iep = new IPEndPoint(IPAddress.Any, 8008);
                EndPoint groupEp = iep;
                do
                {
                    sock.SendTo(Encoding.UTF8.GetBytes(DeviceInfo.Name), destinationEndpoint);
                    retries++;
                    try
                    {
                        sock.ReceiveFrom(buffer, ref groupEp);
                        break;
                    }
                    catch
                    {
                        // ignored
                    }
                } while (retries < maxRetries);
                if (retries == maxRetries)
                {
                    sock.Close();
#if DEBUG
                    MyConsole.WriteLine("No reply");
#endif
                    return;
                }


                IPAddress targetIp = ((IPEndPoint)groupEp).Address;
                // ReSharper disable once UnusedVariable
                string remoteHostname = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

                Connected.Add(targetIp);

                P2PDecide(groupEp, targetIp, sock);
                
                sock.Close();
            }
            else
            {
#if DEBUG
                MyConsole.WriteLine("No Wifi");
#endif
            }
        }
        
        internal void OnWiFiChange(object sender, ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess is NetworkAccess.Internet or NetworkAccess.Local)
            {
                GetConnectionInfo();
                CanSend = true;
            }
            else
            {
                CanSend = false;
            }
        }

        internal void GetConnectionInfo()
        {
            Connected = new List<IPAddress>{ myIp };
            myIp = new IPAddress(BitConverter.GetBytes(DhcpInfo.IpAddress));
            broadcastIp = GetBroadCastIp(myIp, new IPAddress(BitConverter.GetBytes(DhcpInfo.Netmask)));
        }
    }
}
