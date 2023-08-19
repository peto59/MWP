//using Android.Net.Wifi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Android.App;
#if DEBUG
using Ass_Pain.Helpers;
#endif
using Newtonsoft.Json;
using Xamarin.Essentials;
using NetworkAccess = Xamarin.Essentials.NetworkAccess;

//using Android.Net;

namespace Ass_Pain.BackEnd.Network
{
    public class NetworkManager
    {
        internal static NetworkManagerCommon Common = new NetworkManagerCommon();

        public void Listener()
        {
            Connectivity.ConnectivityChanged += Common.OnWiFiChange;
            Common.GetConnectionInfo();
            
            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Interval = 20000;

            aTimer.Elapsed += delegate { Common.SendBroadcast(); };

            aTimer.AutoReset = true;
            if (false) //trusted network
            {
                aTimer.Enabled = true;
            }
            /*Thread.Sleep(5000);
            SendBroadcast();*/

            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint iep = new IPEndPoint(IPAddress.Any, 8008);
            sock.Bind(iep);
            sock.EnableBroadcast = true;
            EndPoint groupEp = iep;
            byte[] buffer = new byte[256];

            try
            {
                Start:
                if (Common.CanSend == false)
                {
                    Thread.Sleep(20000);
                }

                while (Common.CanSend)
                {
#if DEBUG
                    MyConsole.WriteLine("Waiting for broadcast");
#endif
                    sock.ReceiveFrom(buffer, ref groupEp);


                    IPAddress targetIp = ((IPEndPoint)groupEp).Address;
                    if (Enumerable.Contains(NetworkManagerCommon.Connected, targetIp))
                    {
                        Console.WriteLine($"Exit pls2");
                        continue;
                    }
#if DEBUG
                    MyConsole.WriteLine($"Received broadcast from {groupEp}");
                    MyConsole.WriteLine($" {Encoding.UTF8.GetString(buffer)}");
#endif

                    sock.SendTo(Encoding.UTF8.GetBytes(Dns.GetHostName()), groupEp);

                    NetworkManagerCommon.Connected.Add(targetIp);

                    NetworkManagerCommon.P2PDecide(groupEp, targetIp, sock);
                }
                goto Start;
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
            }
        }
    }
}