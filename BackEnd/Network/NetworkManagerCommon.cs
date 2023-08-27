using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Net;
using AngleSharp.Common;
using Java.Net;
using Xamarin.Essentials;
using NetworkAccess = Xamarin.Essentials.NetworkAccess;
using Socket = System.Net.Sockets.Socket;
using SocketType = System.Net.Sockets.SocketType;
using TransportType = Android.Net.TransportType;
#if DEBUG
using Ass_Pain.Helpers;
#endif

namespace Ass_Pain.BackEnd.Network
{
    internal class NetworkManagerCommon
    {
        internal static readonly List<IPAddress> Connected = new List<IPAddress>();
        internal IPAddress MyIp;
        private IPAddress myMask = new IPAddress(0); //0.0.0.0
        
        /// <summary>
        /// Interval between broadcasts
        /// </summary>
        internal const int BroadcastInterval = 20000;
        
        internal static readonly System.Timers.Timer BroadcastTimer = new System.Timers.Timer();
        private CanSend canSend = CanSend.Rejected;
        private static Socket _sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private static readonly byte[] Buffer = new byte[256];
        internal CanSend CanSend
        {
            get => canSend;
            set
            {
#if DEBUG
                MyConsole.WriteLine($"Changing CanSend to {value}");
#endif
                BroadcastTimer.Enabled = value != CanSend.Rejected;
                canSend = value;
            }
        }
        private IPAddress myBroadcastIp;
        internal const int BroadcastPort = 8008;
        internal const int P2PPort = 8009;
        internal const int RsaDataSize = 256;
        internal string CurrentSsid = string.Empty;

        public NetworkManagerCommon()
        {
            
            _sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            _sock.ReceiveTimeout = 2000;
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.S) {
                return;
            }
            try
            {
                NetworkRequest request = new NetworkRequest.Builder()
                    .AddTransportType(TransportType.Wifi)
                    ?.Build();
                while (MainActivity.stateHandler.view == null)
                {
#if DEBUG
                    MyConsole.WriteLine("Waiting for stateHandler.view");
#endif
                    Thread.Sleep(10);
                }
                ConnectivityManager connectivityManager =
                    (ConnectivityManager)MainActivity.stateHandler.view.GetSystemService(
                        Context.ConnectivityService);
                MyNetworkCallback myNetworkCallback = new MyNetworkCallback(NetworkCallbackFlags.IncludeLocationInfo);
                if (request != null) connectivityManager?.RegisterNetworkCallback(request, myNetworkCallback);
#if DEBUG
                else MyConsole.WriteLine("request or connectivityManager is null"); 
#endif
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e.ToString());
#endif
            }
        }

        internal static bool P2PDecide(IPAddress ipAddress)
        {
#if DEBUG
            MyConsole.WriteLine($"New P2P from {ipAddress}");
#endif
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //TODO: add timeout
            IPEndPoint iep = new IPEndPoint(NetworkManager.Common.MyIp, P2PPort);
            EndPoint endPoint = iep;
            sock.Bind(endPoint);
            iep = new IPEndPoint(ipAddress, P2PPort);
            endPoint = iep;
            byte[] buffer = new byte[4];
            //TODO: this is stupid
            Thread.Sleep(1000);
            while (true)
            {
                //int state = new Random().Next(0, 2);
                int state = 1;
                sock.SendTo(BitConverter.GetBytes(state), endPoint);
#if DEBUG
                MyConsole.WriteLine($"sending {state} to {((IPEndPoint)endPoint).Address}");
#endif
                int maxResponseCounter = 4;
                int response;
                do
                {
                    sock.ReceiveFrom(buffer, 4, SocketFlags.None, ref endPoint);
#if DEBUG
                    MyConsole.WriteLine($"received {BitConverter.ToInt32(buffer)} from {((IPEndPoint)endPoint).Address}");
#endif
                    response = BitConverter.ToInt32(buffer);
                    maxResponseCounter--;
#if DEBUG
                    if (response is not (0 or 1))
                    {
                        MyConsole.WriteLine($"Got invalid state in P2PDecide: {response}");
                    }
#endif
                } while (response is not (0 or 1) && maxResponseCounter > 0);

                if (maxResponseCounter == 0)
                {
                    sock.Dispose();
                    return false;
                }

                if (state == response) continue;
                if (state == 0)
                {
                    //server
#if DEBUG
                    MyConsole.WriteLine("Server");
#endif
                    (TcpListener server, int listenPort) = NetworkManagerServer.StartServer(NetworkManager.Common.MyIp);
                    sock.SendTo(BitConverter.GetBytes(listenPort), endPoint);
                    try
                    {
                        sock.Dispose();
                        NetworkManagerServer.Server(server, ipAddress);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        MyConsole.WriteLine(e.ToString());
#endif
                    }
                    sock.Dispose();
                    return true;
                }
                //client
#if DEBUG
                MyConsole.WriteLine("Client");
#endif
                sock.ReceiveFrom(buffer, ref endPoint);
                int sendPort = BitConverter.ToInt32(buffer);
                try
                {
                    sock.Dispose();
                    NetworkManagerClient.Client(((IPEndPoint)endPoint).Address, sendPort);
                }
                catch (Exception e)
                {
#if DEBUG
                    MyConsole.WriteLine(e.ToString());
#endif
                }
                sock.Dispose();
                return true;
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
        
        internal async void SendBroadcast()
        {
            switch (CanSend)
            {
                case CanSend.Allowed:
                {
                    IPEndPoint destinationEndpoint = new IPEndPoint(myBroadcastIp, BroadcastPort);

                    int retries = 0;
                    const int maxRetries = 3;

                    IPEndPoint iep = new IPEndPoint(IPAddress.Any, BroadcastPort);
                    bool processedAtLestOne = false;
                    do
                    {
                        _sock.SendTo(Encoding.UTF8.GetBytes(DeviceInfo.Name), destinationEndpoint);
                        retries++;
                        try
                        {
                            while (true)
                            {
                                EndPoint groupEp = iep;
                                _sock.ReceiveFrom(Buffer, ref groupEp);
                                IPAddress targetIp = ((IPEndPoint)groupEp).Address;
                                string remoteHostname = Encoding.UTF8.GetString(Buffer).TrimEnd('\0');
#if DEBUG
                                MyConsole.WriteLine($"found remote: {remoteHostname}, {targetIp}");       
#endif                
                                
                                DateTime now = DateTime.Now;
                                MainActivity.stateHandler.AvailableHosts.Add((targetIp, now, remoteHostname));
                                processedAtLestOne = true;
                                //TODO: add to available targets. Don't connect directly, check if sync is allowed.
                                //TODO: here
                                if (!FileManager.IsTrustedSyncTarget(remoteHostname))
                                {
                                    FileManager.AddTrustedSyncTarget(remoteHostname);
                                    //Thread.Sleep(100);
                                }
                                if (!FileManager.IsTrustedSyncTarget(remoteHostname)) continue;
                                
                                Connected.Add(targetIp);
                                new Thread(() =>
                                {
                                    if (!P2PDecide(targetIp))
                                    {
                                        Connected.Remove(targetIp);
                                    }
                                }).Start();
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    } while (retries < maxRetries && !processedAtLestOne);

#if DEBUG
                    if (retries == maxRetries)
                    {
                        MyConsole.WriteLine("No reply");
                    }
#endif
                    
                    break;
                }
                case CanSend.Test:
                    TestNetwork();
                    break;
                case CanSend.Rejected:
                default:
#if DEBUG
                    MyConsole.WriteLine("Not allowed to send");
#endif
                    BroadcastTimer.Enabled = false;
                    break;
            }
        }

        internal bool GetConnectionInfo(IPAddress ip = null)
        {
            if (ip != null)
            {
                MyIp = ip;
            }
            else
            {
                string name = Dns.GetHostName();
                MyIp = Dns.GetHostAddresses(name)[0];
            }
            try
            {
                InetAddress inetAddress = InetAddress.GetByAddress(MyIp.GetAddressBytes());
                NetworkInterface networkInterface = NetworkInterface.GetByInetAddress(inetAddress);
                if (networkInterface is not { InterfaceAddresses: not null }) return false;
                InetAddress broadcast = networkInterface.InterfaceAddresses.First(a => a.Address != null && a.Address.Equals(inetAddress)).Broadcast;
                if (broadcast != null)
                {
                    myBroadcastIp = IPAddress.Parse(broadcast.ToString().Trim('/'));
                }
                else
                {
                    short prefix = networkInterface.InterfaceAddresses.First(a => a.Address != null && a.Address.Equals(inetAddress))
                        .NetworkPrefixLength;
                    myMask = PrefixLengthToNetmask(prefix);
                    myBroadcastIp = GetBroadCastIp(MyIp, myMask);
                }
#if DEBUG
                MyConsole.WriteLine($"My IP IS: {MyIp}");
                MyConsole.WriteLine($"My MASK IS: {myMask}");
                MyConsole.WriteLine($"My BROADCAST IS: {myBroadcastIp}" );
#endif
                return true;
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e.ToString());
#endif
                return false;
            }
        }

        private static IPAddress PrefixLengthToNetmask(short prefixLength)
        {
            uint binaryMask = 0xFFFFFFFF;
            binaryMask <<= 32 - prefixLength;

            byte[] ipBytes = BitConverter.GetBytes(binaryMask);
            Array.Reverse(ipBytes);  // Convert to big-endian.

            return new IPAddress(ipBytes);
        }

        internal static void TestNetwork()
        {
            //TODO: add evaluation
            NetworkManager.Common.CanSend = CanSend.Allowed;
        }
        
        
        internal void OnWiFiChange(ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess is NetworkAccess.Internet or NetworkAccess.Local && e.ConnectionProfiles.Contains(ConnectionProfile.WiFi))
            {
                CanSend = GetConnectionInfo() ? CanSend.Test : CanSend.Rejected;
                return;
            }

            CanSend = CanSend.Rejected;
        }

        internal static async Task<(string storedPrivKey, string storedPubKey)> LoadKeys(string remoteHostname)
        {
            string storedPubKey, storedPrivKey;
            try
            {
                storedPrivKey = await SecureStorage.GetAsync($"{remoteHostname}_privkey");
                storedPubKey = await SecureStorage.GetAsync($"{remoteHostname}_pubkey");
            }
            catch
            {
                throw new Exception("You're fucked, boy. Go buy something else than Nokia 3310");
            }
            return (storedPrivKey, storedPubKey);
        }
    }

    internal class MyNetworkCallback : ConnectivityManager.NetworkCallback
    {
        internal MyNetworkCallback(NetworkCallbackFlags flags = NetworkCallbackFlags.None) : base((int)flags)
        {
        }
        
        public override void OnCapabilitiesChanged(Android.Net.Network network, NetworkCapabilities networkCapabilities)
        {
            base.OnCapabilitiesChanged(network, networkCapabilities);
            if (networkCapabilities.HasCapability(NetCapability.NotMetered) && networkCapabilities.HasCapability(NetCapability.Trusted))
            {
                Dictionary<string, string> transportInfo = networkCapabilities.TransportInfo.ToDictionary();
                string ipString = transportInfo["IpAddress"];
                if (int.TryParse(ipString, out int ipInt))
                {
                    IPAddress ipAdd = new IPAddress(ipInt);
                    string ip = ipAdd.ToString();
                    if (ValidateIPv4(ip) && ip != "0.0.0.0")
                    {
                        if (NetworkManager.Common.MyIp == null)
                        {
                            NetworkManager.Common.CanSend = NetworkManager.Common.GetConnectionInfo(ipAdd) ? CanSend.Test : CanSend.Rejected;
                        }
                        string ssid = transportInfo["SSID"];
                        if (ssid != NetworkManager.Common.CurrentSsid)
                        {
                            NetworkManager.Common.CanSend = CanSend.Test;
                            NetworkManager.Common.CurrentSsid = ssid;
                        }
                        else if (NetworkManager.Common.CanSend == CanSend.Rejected)
                        {
                            NetworkManager.Common.CanSend = CanSend.Test;
                        }
                        return;
                    }
                }
            }
            NetworkManager.Common.CanSend = CanSend.Rejected;
        }

        public override void OnLost(Android.Net.Network network)
        {
            base.OnLost(network);
#if DEBUG
            MyConsole.WriteLine("Network lost!");
#endif
            NetworkManager.Common.CanSend = CanSend.Rejected;
            NetworkManager.Common.MyIp = null;
        }

        private static bool ValidateIPv4(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }
            string[] splitValues = ipString.Split('.');
            return splitValues.Length == 4 && splitValues.All(r => byte.TryParse(r, out byte _));
        }
    }

    internal enum CanSend : byte
    {
        Rejected,
        Test,
        Allowed
    }
}
