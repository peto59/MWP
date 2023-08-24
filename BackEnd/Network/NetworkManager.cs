using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Xamarin.Essentials;
#if DEBUG
using Ass_Pain.Helpers;
#endif

namespace Ass_Pain.BackEnd.Network
{
    /// <summary>
    /// Class that manages network connections
    /// </summary>
    public static class NetworkManager
    {
        //gpg test
        internal static readonly NetworkManagerCommon Common = new NetworkManagerCommon();
        /// <summary>
        /// Interval between broadcasts
        /// </summary>
        private const int BroadcastInterval = NetworkManagerCommon.BroadcastInterval;

        private const int numberOfMissedBroadcastsToRemoveHost = 3;

        private static readonly TimeSpan RemoveInterval = new TimeSpan(BroadcastInterval*numberOfMissedBroadcastsToRemoveHost*TimeSpan.TicksPerSecond);

        /// <summary>
        /// Starts listening for broadcasts, sending broadcasts and managing connections. Entry point for NetworkManager.
        /// </summary>
        public static void Listener()
        {
            //TODO: unknown SSID
            //TODO: broadcast is sent only once
            NetworkManagerCommon.BroadcastTimer.Interval = BroadcastInterval;
            NetworkManagerCommon.BroadcastTimer.Elapsed += delegate { Common.SendBroadcast(); };
            NetworkManagerCommon.BroadcastTimer.AutoReset = true;

            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.S) {
                Connectivity.ConnectivityChanged += delegate(object _, ConnectivityChangedEventArgs args) { Common.OnWiFiChange(args); };
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
                                    EndPoint groupEp = iep;
#endif
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
                                    string hostname = Encoding.UTF8.GetString(buffer);
#if DEBUG
                                    MyConsole.WriteLine($"Received broadcast from {groupEp}, hostname: {hostname}");
#endif
                                    sock.SendTo(Encoding.UTF8.GetBytes(Dns.GetHostName()), groupEp);

                                    DateTime now = DateTime.Now;
                                    MainActivity.stateHandler.AvailableHosts.Add((targetIp, now, hostname));
                                    //remove stale hosts
                                    foreach ((IPAddress ipAddress, DateTime lastSeen, string hostname) removal in MainActivity.stateHandler.AvailableHosts.Where(a =>
                                                 a.lastSeen > now + RemoveInterval))
                                    {
                                        MainActivity.stateHandler.AvailableHosts.Remove(removal);
                                    }

                                    //TODO: add to available targets. Don't connect directly, check if sync is allowed.
                                    NetworkManagerCommon.Connected.Add(targetIp);
                                    if (!NetworkManagerCommon.P2PDecide(groupEp, targetIp, ref sock))
                                    {
                                        NetworkManagerCommon.Connected.Remove(targetIp);
                                    }
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
                    MyConsole.WriteLine(e.ToString());
#endif
                }
                finally
                {
                    sock.Close();
                    sock.Dispose();
                }
            }
        }
    }
}