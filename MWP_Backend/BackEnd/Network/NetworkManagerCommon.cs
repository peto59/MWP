using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if ANDROID
using Android.App;
using Android.Content;
using Android.Net;
using Android.Net.Wifi;
using TransportType = Android.Net.TransportType;
using Java.Net;
using Xamarin.Essentials;
using NetworkAccess = Xamarin.Essentials.NetworkAccess;
#endif
using ProtocolType = System.Net.Sockets.ProtocolType;
using Socket = System.Net.Sockets.Socket;
using SocketType = System.Net.Sockets.SocketType;

using Mono.Nat;
using AngleSharp.Common;
using MWP_Backend.BackEnd;
using MWP_Backend.DatatypesAndExtensions;
using MWP.DatatypesAndExtensions;
using SocketException = System.Net.Sockets.SocketException;
#if DEBUG
using MWP.BackEnd.Helpers;
using MWP.Helpers;
#endif

namespace MWP.BackEnd.Network
{
    internal class NetworkManagerCommon
    {
        internal static readonly List<IPAddress> Connected = new List<IPAddress>();
#if DEBUG
        private IPAddress? myIp;
        internal IPAddress? MyIp
        {
            get => myIp;
            set
            {
                MyConsole.WriteLine($"New IP is {value}");
                myIp = value;
            }
        }
#else
        internal IPAddress? MyIp;
#endif
        private IPAddress myMask = new IPAddress(0); //0.0.0.0
        
        /// <summary>
        /// Interval between broadcasts
        /// </summary>
        internal const int BroadcastInterval = 50_000;
        
        internal static readonly System.Timers.Timer BroadcastTimer = new System.Timers.Timer();
        private CanSend canSend = CanSend.Rejected;
        private static readonly Socket Sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
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
        private IPAddress? myBroadcastIp;
        internal const int BroadcastPort = 8008;
        private const int P2PPort = 8009;
        internal const int RsaDataSize = 256;
#if DEBUG
        private string currentSsid = string.Empty;
        internal string CurrentSsid
        {
            get => currentSsid;
            set
            {
                MyConsole.WriteLine($"Changing CurrentSsid to {value}");
                currentSsid = value;
            }
        }
#else
        internal string CurrentSsid = string.Empty;
#endif

        public NetworkManagerCommon()
        {
            
            Sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            Sock.ReceiveTimeout = 500;
            if (SettingsManager.CanUseWan)
            {
                NatUtility.DeviceFound += delegate(object _, DeviceEventArgs args)
                {
                    INatDevice device = args.Device;
#if DEBUG
                    MyConsole.WriteLine("Found Upnp device");
#endif
                    try
                    {
                        Mapping mapping = device.GetSpecificMapping(Protocol.Tcp, SettingsManager.WanPort);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        MyConsole.WriteLine(e);
                        MyConsole.WriteLine("Adding Upnp");
#endif
                        //Mapping mapping = device.CreatePortMap(new Mapping(Protocol.Tcp, SettingsManager.WanPort, SettingsManager.WanPort, 0, "Ass Pain Music Sharing"));
                        try
                        {
                            Mapping mapping = device.CreatePortMap(new Mapping(Protocol.Tcp, SettingsManager.WanPort,
                                SettingsManager.WanPort));
                            if (mapping.PublicPort != SettingsManager.WanPort || mapping.PublicPort != mapping.PrivatePort)
                            {
#if DEBUG
                                MyConsole.WriteLine("Failed to create Upnp, create port forwarding manually");
#endif
                            }
                            //TODO: delete upnp on disable
                        }
                        catch (Exception exception)
                        {
#if DEBUG
                            MyConsole.WriteLine(exception);
#endif
                        }
                    }
                };
                NatUtility.StartDiscovery();
            }
#if ANDROID
            if (Android.OS.Build.VERSION.SdkInt <= Android.OS.BuildVersionCodes.SV2) {
                return;
            }
#endif
            try
            {
#if ANDROID
                NetworkRequest? request = new NetworkRequest.Builder()
                    .AddTransportType(TransportType.Wifi)
                    ?.Build();
                while (MainActivity.StateHandler.view == null)
                {
#if DEBUG
                    MyConsole.WriteLine("Waiting for stateHandler.view");
#endif
                    Thread.Sleep(10);
                }

                ConnectivityManager? connectivityManager =
                    (ConnectivityManager?)MainActivity.StateHandler.view.GetSystemService(
                        Context.ConnectivityService);

                MyNetworkCallback myNetworkCallback = new MyNetworkCallback(NetworkCallbackFlags.IncludeLocationInfo);
                if (request != null && connectivityManager != null) connectivityManager.RegisterNetworkCallback(request, myNetworkCallback);

#if DEBUG
                else MyConsole.WriteLine("request or connectivityManager is null"); 
#endif
#endif
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
            }
        }

        internal static bool P2PDecide(IPAddress ipAddress, List<Song>? songsToSend = null)
        {
            if (ipAddress.Equals(NetworkManager.Common.MyIp) || Enumerable.Contains(Connected, ipAddress))
            {
#if DEBUG
                MyConsole.WriteLine("Exit pls2");
#endif
                return true;
            }
            Connected.Add(ipAddress);
            songsToSend ??= new List<Song>();
#if DEBUG
            MyConsole.WriteLine($"New P2P from {ipAddress}");
#endif
            if (NetworkManager.Common.MyIp == null) return false;
            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sock.ReceiveTimeout = 500;
            IPEndPoint iep = new IPEndPoint(NetworkManager.Common.MyIp, P2PPort);
            EndPoint endPoint = iep;
            sock.Bind(endPoint);
            iep = new IPEndPoint(ipAddress, P2PPort);
            IPEndPoint remoteEndpoint = new IPEndPoint(ipAddress, P2PPort);
            endPoint = iep;
            byte[] buffer = new byte[4];
            byte cnt = 0;
            Dictionary<byte, byte> remote = new Dictionary<byte, byte>();
            Dictionary<byte, byte> local = new Dictionary<byte, byte>();
            P2PState stateObject = new P2PState(new []{(byte)0, (byte)0, (byte)0, (byte)0});
            try
            {
                while (true)
                {
                    byte state = (byte)new Random().Next(0, 2);
                    sock.SendTo(P2PState.Send(cnt, state), remoteEndpoint);
                    local.TryAdd(cnt, state);
#if DEBUG
                    MyConsole.WriteLine($"sending {state} at cnt {cnt} to {((IPEndPoint)endPoint).Address}");
#endif
                    cnt++;
                    byte maxResponseCounter = NetworkManager.P2PMaxResponseCounter;
                    byte? response = null;
                    do
                    {
                        bool breakFlag = false;
                        do
                        {
                            while (maxResponseCounter > 0)
                            {
                                try
                                {
                                    sock.ReceiveFrom(buffer, 4, SocketFlags.None, ref endPoint);
                                    if (!((IPEndPoint)endPoint).Address.Equals(remoteEndpoint.Address))
                                    {
                                        maxResponseCounter--;
                                        continue;
                                    }
                                }
                                catch (SocketException)
                                {
                                    cnt++;
                                    state = (byte)new Random().Next(0, 2);
                                    local.TryAdd(cnt, state);
                                    sock.SendTo(P2PState.Send(cnt, state), remoteEndpoint);
                                    maxResponseCounter--;
                                    continue;
                                }

                                break;
                            }

                            if (maxResponseCounter <= 0)
                            {
                                breakFlag = true;
                                break;
                            }
                            stateObject = new P2PState(buffer);
#if DEBUG
                            MyConsole.WriteLine(
                                $"received {stateObject.Type} with {stateObject.State} at cnt {stateObject.Cnt} from {((IPEndPoint)endPoint).Address}");
#endif
                            if (!stateObject.IsValid)
                            {
                                maxResponseCounter--;
                                break;
                            }

                            if (stateObject.Type == P2PStateTypes.Port)
                            {
                                breakFlag = true;
                                state = 1;
                                response = 0;
                                break;
                            }

                            if (stateObject.Type == P2PStateTypes.Request)
                            {
                                if (local.TryGetValue(stateObject.Cnt, out state))
                                {
                                    sock.SendTo(P2PState.Send(stateObject.Cnt, state), remoteEndpoint);
                                }
                                else
                                {
                                    state = (byte)new Random().Next(0, 2);
                                    local.TryAdd(stateObject.Cnt, state);
                                    sock.SendTo(P2PState.Send(stateObject.Cnt, state), remoteEndpoint);
                                }
                            }

                            if (stateObject.Type == P2PStateTypes.State)
                            {
                                remote.TryAdd(stateObject.Cnt, stateObject.State);
                                bool check = true;
                                foreach ((byte key, byte localVal) in local)
                                {
                                    if (!remote.TryGetValue(key, out byte remoteVal))
                                    {
                                        sock.SendTo(P2PState.Request(key), remoteEndpoint);
                                        check = false;
                                    }

                                    if (!check)
                                    {
                                        continue;
                                    }

                                    if (localVal == remoteVal)
                                    {
                                        continue;
                                    }

                                    response = remoteVal;
                                    state = localVal;
                                    breakFlag = true;
                                    break;
                                }

                                if (breakFlag)
                                {
                                    break;
                                }
                            }
                        } while (sock.Available >= 4);

                        if (breakFlag)
                        {
                            break;
                        }
                    } while (maxResponseCounter > 0);

                    if (maxResponseCounter == 0)
                    {
                        sock.Dispose();
#if DEBUG
                        MyConsole.WriteLine("Max Response Counter exceeded");
#endif
                        return false;
                    }

                    if (state == (response ?? state)) continue;
                    if (state == 0)
                    {
                        //server
#if DEBUG
                        MyConsole.WriteLine("Server");
#endif
                        if (NetworkManager.Common.MyIp == null) return false;
                        (TcpListener server, int listenPort) =
                            NetworkManagerServer.StartServer(NetworkManager.Common.MyIp);
                        sock.SendTo(BitConverter.GetBytes(listenPort), remoteEndpoint);
                        try
                        {
                            NetworkManagerServer.Server(server, ipAddress, songsToSend, ref endPoint, ref sock,
                                local);
                        }
                        catch (Exception e)
                        {
#if DEBUG
                            MyConsole.WriteLine(e);
#endif
                        }

                        sock.Dispose();
                        return true;
                    }
                    //client
#if DEBUG
                    MyConsole.WriteLine("Client");
#endif
                    while (stateObject.Type != P2PStateTypes.Port)
                    {
                        while (true)
                        {
                            try
                            {
                                sock.ReceiveFrom(buffer, 4, SocketFlags.None, ref endPoint);
                                if (!((IPEndPoint)endPoint).Address.Equals(remoteEndpoint.Address))
                                {
                                    continue;
                                }
                            }
                            catch (SocketException)
                            {
                                sock.SendTo(P2PState.Send(cnt, state), remoteEndpoint);
                                continue;
                            }

                            break;
                        }

                        stateObject = new P2PState(buffer);
                        if (stateObject.Type == P2PStateTypes.Request)
                        {
                            if (local.TryGetValue(stateObject.Cnt, out state))
                            {
                                sock.SendTo(P2PState.Send(stateObject.Cnt, state), remoteEndpoint);
                            }
                            else
                            {
                                state = (byte)new Random().Next(0, 2);
                                local.TryAdd(stateObject.Cnt, state);
                                sock.SendTo(P2PState.Send(stateObject.Cnt, state), remoteEndpoint);
                            }
                        }
                    }

                    int sendPort = stateObject.Port;
                    try
                    {
                        sock.Dispose();
                        NetworkManagerClient.Client(((IPEndPoint)endPoint).Address, sendPort, songsToSend);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        MyConsole.WriteLine(e);
#endif
                        return false;
                    }

                    sock.Dispose();
                    return true;
                }
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
                MyConsole.WriteLine("Returning false in P2PDecide");
#endif
                return false;
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
        
        
        internal void SendBroadcast(List<Song>? songsToSend = null, IPAddress? targetIpAddress = null)
        {
            
#if DEBUG
            MyConsole.WriteLine($"SSID: {CurrentSsid}");            
#endif
            switch (CanSend)
            {
                case CanSend.Allowed:
                {
                    if (targetIpAddress != null)
                    {
                        while (Enumerable.Contains(Connected, targetIpAddress))
                        {
#if DEBUG
                            MyConsole.WriteLine("Waiting for remote host to became available");
#endif
                            Thread.Sleep(1000);
                        }
                    }
                    if (myBroadcastIp != null || targetIpAddress != null)
                    {
                        IPEndPoint destinationEndpoint = targetIpAddress != null
                            ? new IPEndPoint(targetIpAddress,
                                BroadcastPort)
                            : new IPEndPoint(myBroadcastIp!,
                                BroadcastPort);

                        int retries = 0;
                        const int maxRetries = 3;

                        IPEndPoint iep = targetIpAddress != null
                            ? new IPEndPoint(targetIpAddress,
                                BroadcastPort)
                            : new IPEndPoint(IPAddress.Any,
                                BroadcastPort);
                        bool processedAtLestOne = false;
                        do
                        {
                            Sock.SendTo(Encoding.UTF8.GetBytes(NetworkManager.DeviceName), destinationEndpoint);
                            retries++;
                            try
                            {
                                while (true)
                                {
                                    EndPoint groupEp = iep;
                                    Sock.ReceiveFrom(Buffer, ref groupEp);
                                    IPAddress targetIp = ((IPEndPoint)groupEp).Address;
                                    string remoteHostname = Encoding.UTF8.GetString(Buffer).TrimEnd('\0');
#if DEBUG
                                    MyConsole.WriteLine($"found remote: {remoteHostname}, {targetIp}");       
#endif                
                                    AddAvailableHost(targetIp, remoteHostname);
                                    if (targetIp.Equals(MyIp) || Enumerable.Contains(Connected, targetIp))
                                    {
#if DEBUG
                                        MyConsole.WriteLine("Exit pls2");
#endif
                                        continue;
                                    }
                                    new Thread(() =>
                                    {
                                        if (!P2PDecide(targetIp, songsToSend))
                                        {
                                            Connected.Remove(targetIp);
                                        }
                                    }).Start();
                                    break;
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
                    }
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

        internal bool GetConnectionInfo(IPAddress? ip = null)
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
#if ANDROID
                InetAddress inetAddress = InetAddress.GetByAddress(MyIp.GetAddressBytes());
                NetworkInterface? networkInterface = NetworkInterface.GetByInetAddress(inetAddress);
                if (networkInterface is not { InterfaceAddresses: not null }) return false;
                InetAddress? broadcast = networkInterface.InterfaceAddresses.First(a => a.Address != null && a.Address.Equals(inetAddress)).Broadcast;
                if (broadcast != null)
                {
                    myBroadcastIp = IPAddress.Parse(broadcast.ToString().Trim('/'));
                }
                else
                {
                    short? prefix = networkInterface.InterfaceAddresses.First(a => a.Address != null && a.Address.Equals(inetAddress))
                        .NetworkPrefixLength;
                    myMask = PrefixLengthToNetmask((short)prefix);
                    myBroadcastIp = GetBroadCastIp(MyIp, myMask);
                }
#if DEBUG
                MyConsole.WriteLine($"My IP IS: {MyIp}");
                MyConsole.WriteLine($"My MASK IS: {myMask}");
                MyConsole.WriteLine($"My BROADCAST IS: {myBroadcastIp}" );
#endif
                return true;
#endif
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                return false;
            }

            return false;
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
            if (SettingsManager.CanUseNetwork != CanUseNetworkState.Allowed)
            {
                NetworkManager.Common.CanSend = CanSend.Rejected;
                return;
            }
#if ANDROID
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.S && NetworkManager.Common.CurrentSsid == string.Empty)
            {
                NetworkManager.Common.GetWifiSsid();
            }
#endif
            
            if (!FileManager.IsTrustedSsid(NetworkManager.Common.CurrentSsid))
            {
                NetworkManager.Common.CanSend = CanSend.Rejected;
                return;
            }
            NetworkManager.Common.CanSend = CanSend.Allowed;
        }
        
#if ANDROID
        [Obsolete("Deprecated")]
        internal void OnWiFiChange(ConnectivityChangedEventArgs e)
        {
#if DEBUG
            MyConsole.WriteLine("Changing wifi states: deprecated mode");
#endif
            if (e.NetworkAccess is NetworkAccess.Internet or NetworkAccess.Local && e.ConnectionProfiles.Contains(ConnectionProfile.WiFi))
            {
#if DEBUG
                MyConsole.WriteLine($"NetworkAccess is {e.NetworkAccess}");
#endif
                WifiManager? wifiManager = (WifiManager?)Application.Context.GetSystemService(Context.WifiService);
                WifiInfo? info = wifiManager?.ConnectionInfo;
                CurrentSsid = info?.SSID ?? string.Empty;
                StateHandler.TriggerShareFragmentRefresh();

                CanSend = GetConnectionInfo() ? CanSend.Test : CanSend.Rejected;
                return;
            }

            CurrentSsid = string.Empty;
            CanSend = CanSend.Rejected;
            StateHandler.TriggerShareFragmentRefresh();
        }

        [Obsolete("Deprecated")]
        internal void GetWifiSsid()
        {
            ConnectivityManager? connectivityManager = (ConnectivityManager?)Application.Context.GetSystemService(Context.ConnectivityService);
            NetworkInfo? activeNetwork = connectivityManager?.ActiveNetworkInfo;

            if (activeNetwork?.Type != ConnectivityType.Wifi) return;
            WifiManager? wifiManager = (WifiManager?)Application.Context.GetSystemService(Context.WifiService);
            WifiInfo? wifiInfo = wifiManager?.ConnectionInfo;
            CurrentSsid = wifiInfo?.SSID ?? string.Empty;
            StateHandler.TriggerShareFragmentRefresh();
        }
#endif

        internal static async Task<(string? storedPrivKey, string? storedPubKey)> LoadKeys(string remoteHostname)
        {
            string? storedPubKey, storedPrivKey;
            try
            {
#if ANDROID
                storedPrivKey = await SecureStorage.GetAsync($"{remoteHostname}_privkey");
                storedPubKey = await SecureStorage.GetAsync($"{remoteHostname}_pubkey");
#endif
            }
            catch
            {
                throw new Exception("You're fucked, boy. Go buy something else than Nokia 3310");
            }
#if ANDROID
            return (storedPrivKey, storedPubKey);
#elif LINUX
            return ("", "");
#endif
        }
        
        internal static void AddAvailableHost(IPAddress targetIp, string hostname)
        {
            DateTime now = DateTime.Now;
            
            List<(IPAddress ipAddress, DateTime lastSeen, string hostname)> currentAvailableHosts = StateHandler.AvailableHosts.Where(a => a.hostname == hostname).ToList();
            switch (currentAvailableHosts.Count)
            {
                case > 1:
                {
                    foreach ((IPAddress ipAddress, DateTime lastSeen, string hostname) currentAvailableHost in currentAvailableHosts)
                    {
                        StateHandler.AvailableHosts.Remove(currentAvailableHost);
                    }
                    StateHandler.AvailableHosts.Add((targetIp, now, hostname));
                    break;
                }
                case 1:
                {
                    int index = StateHandler.AvailableHosts.IndexOf(currentAvailableHosts.First());
                    StateHandler.AvailableHosts[index] = (targetIp, now, hostname);
                    break;
                }
                default:
                    StateHandler.AvailableHosts.Add((targetIp, now, hostname));
                    break;
            }
            
            //remove stale hosts
            foreach ((IPAddress ipAddress, DateTime lastSeen, string hostname) removal in StateHandler.AvailableHosts.Where(a =>
                         a.lastSeen > now + NetworkManager.RemoveInterval))
            {
                StateHandler.AvailableHosts.Remove(removal);
            }
            
            StateHandler.TriggerShareFragmentRefresh();
        }
    }
#if ANDROID
    internal class MyNetworkCallback : ConnectivityManager.NetworkCallback
    {
        internal MyNetworkCallback(NetworkCallbackFlags flags) : base((int)flags)
        {
        }
        
        public override void OnCapabilitiesChanged(Android.Net.Network network, NetworkCapabilities networkCapabilities)
        {
            base.OnCapabilitiesChanged(network, networkCapabilities);
            if (networkCapabilities.HasCapability(NetCapability.NotMetered) && networkCapabilities.HasCapability(NetCapability.Trusted))
            {
                Dictionary<string, string> transportInfo = networkCapabilities.TransportInfo.ToDictionary();

                if (transportInfo.TryGetValue("IpAddress", out string ipString))
                {
                    if (long.TryParse(ipString, out long longIp))
                    {
                        byte[] x = BitConverter.GetBytes(longIp).Take(4).ToArray();
                        IPAddress ipAdd = new IPAddress(x);
                        string ip = ipAdd.ToString();
                        if (ValidateIPv4(ip) && ip != "0.0.0.0")
                        {
                            if (NetworkManager.Common.MyIp == null)
                            {
                                NetworkManager.Common.CanSend = NetworkManager.Common.GetConnectionInfo(ipAdd) ? CanSend.Test : CanSend.Rejected;
                            }
                            string ssid = transportInfo["SSID"];
#if DEBUG
                            MyConsole.WriteLine($"SSID: {ssid}");         
#endif
                            if (ssid != NetworkManager.Common.CurrentSsid)
                            {
                                NetworkManager.Common.CanSend = CanSend.Test;
                                NetworkManager.Common.CurrentSsid = ssid;
                                StateHandler.TriggerShareFragmentRefresh();
                            }
                            else if (NetworkManager.Common.CanSend == CanSend.Rejected)
                            {
                                NetworkManager.Common.CanSend = CanSend.Test;
                            }
                            return;
                        }
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
            NetworkManager.Common.CurrentSsid = string.Empty;
            StateHandler.TriggerShareFragmentRefresh();
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
#endif

    internal enum CanSend : byte
    {
        Rejected,
        Test,
        Allowed
    }
}