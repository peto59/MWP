using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP.BackEnd.Network
{
    /// <summary>
    /// Class that manages network connections
    /// </summary>
    public static class NetworkManager
    {
        internal static readonly NetworkManagerCommon Common = new NetworkManagerCommon();
        /// <summary>
        /// Interval between broadcasts
        /// </summary>
        private const int BroadcastInterval = NetworkManagerCommon.BroadcastInterval;

        //TODO: move to settings
        private const int numberOfMissedBroadcastsToRemoveHost = 3;
        internal const int MaxTimeoutCounter = 10000;
        internal const int DefaultBuffSize = 8096;
        internal const int P2PMaxResponseCounter = 6;

        internal static readonly TimeSpan RemoveInterval = new TimeSpan(BroadcastInterval*numberOfMissedBroadcastsToRemoveHost*TimeSpan.TicksPerSecond);

        /// <summary>
        /// Starts listening for broadcasts, sending broadcasts and managing connections. Entry point for NetworkManager.
        /// </summary>
        public static void Listener()
        {
            //TODO: unknown SSID
            NetworkManagerCommon.BroadcastTimer.Interval = BroadcastInterval;
            NetworkManagerCommon.BroadcastTimer.Elapsed += delegate { Common.SendBroadcast(); };
            NetworkManagerCommon.BroadcastTimer.AutoReset = true;
            
            if (Android.OS.Build.VERSION.SdkInt <= Android.OS.BuildVersionCodes.SV2) {
                Connectivity.ConnectivityChanged += delegate(object _, ConnectivityChangedEventArgs args) { Common.OnWiFiChange(args); };
                Common.GetWifiSsid();
                if (Common.MyIp == null)
                {
                    Common.CanSend = Common.GetConnectionInfo() ? CanSend.Test : CanSend.Rejected;
                }
            }

            
            //Thread.Sleep(5000);
            //Common.SendBroadcast();

            while (true)
            {
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint iep = new IPEndPoint(IPAddress.Any, NetworkManagerCommon.BroadcastPort);
                sock.Bind(iep);
                sock.EnableBroadcast = true;
                byte[] buffer = new byte[256];

                try
                {
                    while (true)
                    {
                        switch (Common.CanSend)
                        {
                            case CanSend.Test:
                                NetworkManagerCommon.TestNetwork();
                                break;
                            case CanSend.Allowed:
                                while (Common.CanSend == CanSend.Allowed)
                                {
#if DEBUG
                                    MyConsole.WriteLine("Waiting for broadcast");
#endif
                                    EndPoint groupEp = iep;
                                    sock.ReceiveFrom(buffer, ref groupEp);
                                    if (Common.CanSend != CanSend.Allowed) continue;


                                    IPAddress targetIp = ((IPEndPoint)groupEp).Address;
                                    if (targetIp.Equals(Common.MyIp) || Enumerable.Contains(NetworkManagerCommon.Connected, targetIp))
                                    {
#if DEBUG
                                        MyConsole.WriteLine("Exit pls2");
#endif
                                        continue;
                                    }
                                    string remoteHostname = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
#if DEBUG
                                    MyConsole.WriteLine($"Received broadcast from {groupEp}, hostname: {remoteHostname}");
#endif
                                    sock.SendTo(Encoding.UTF8.GetBytes(DeviceInfo.Name), groupEp);
                                    
                                    NetworkManagerCommon.AddAvailableHost(targetIp, remoteHostname);
                                    

                                    //TODO: add to available targets. Don't connect directly, check if sync is allowed.
                                    //TODO: doesn't work with one time sends....
                                    if (!FileManager.IsTrustedSyncTarget(remoteHostname)) continue;
                                    
                                    NetworkManagerCommon.Connected.Add(targetIp);
                                    new Thread(() =>
                                    {
                                        if (!NetworkManagerCommon.P2PDecide(targetIp))
                                        {
                                            NetworkManagerCommon.Connected.Remove(targetIp);
                                        }
                                    }).Start();
                                }
                                break;
                            case CanSend.Rejected:
                            default:
                                Thread.Sleep(20000);
                                break;
                        }
                    }
                }
                catch (SocketException e)
                {
#if DEBUG
                    MyConsole.WriteLine(e);
#endif
                }
                finally
                {
                    sock.Close();
                    sock.Dispose();
                }
            }
        }

        /// <summary>
        /// Get combination of all trusted and available hosts
        /// </summary>
        /// <returns>List of tuple with hostname, lastSeen, and whether this host is trusted</returns>
        public static List<(string hostname, DateTime? lastSeen, bool state)> GetAllHosts()
        {
            List<string> trusted = FileManager.GetTrustedSyncTargets();
            List<(string hostname, DateTime? lastSeen, bool state)> output = new List<(string hostname, DateTime? lastSeen, bool state)>();
            foreach ((IPAddress ipAddress, DateTime lastSeen, string hostname) stateHandlerAvailableHost in MainActivity.StateHandler.AvailableHosts)
            {
                if (stateHandlerAvailableHost.hostname == "localhost")
                {
                    continue;
                }
                if (trusted.Contains(stateHandlerAvailableHost.hostname))
                {
                    output.Add((stateHandlerAvailableHost.hostname, stateHandlerAvailableHost.lastSeen, true));
                    trusted.Remove(stateHandlerAvailableHost.hostname);
                }
                else
                {
                    output.Add((stateHandlerAvailableHost.hostname, stateHandlerAvailableHost.lastSeen, false));
                }
            }
            output.AddRange(trusted.Select(s => ((string hostname, DateTime? lastSeen, bool state))(s, null, true)));
            return output;
        }
    }
}