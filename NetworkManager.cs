using Android.App;
using Android.Content;
//using Android.Net.Wifi;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YoutubeExplode.Playlists;
using YoutubeExplode;
using System.Net;
using Android.Locations;
using System.Threading.Tasks;
using Xamarin.Essentials;
using System.Net.Sockets;
using YoutubeExplode.Common;
using System.Net.NetworkInformation;
using System.Threading;
using Org.Apache.Http.Protocol;
using System.Net.Http;
using System.IO;
using System.Timers;
using Android.Annotation;
using Java.Nio;
using Android.Bluetooth;
using Org.Apache.Http.Authentication;
//using Android.Net;

namespace Ass_Pain
{
    internal class NetworkManager
    {
        static Android.Net.Wifi.WifiManager wifiManager = (Android.Net.Wifi.WifiManager)Application.Context.GetSystemService(Service.WifiService);

        static Android.Net.DhcpInfo d = wifiManager.DhcpInfo;
        static IPAddress myIP = new IPAddress(d.IpAddress);
        List<IPAddress> connected = new List<IPAddress>{ { myIP } };
        IPAddress GetBroadCastIP(IPAddress host, IPAddress mask)
        {
            byte[] broadcastIPBytes = new byte[4];
            byte[] hostBytes = host.GetAddressBytes();
            byte[] maskBytes = mask.GetAddressBytes();
            for (int i = 0; i < 4; i++)
            {
                broadcastIPBytes[i] = (byte)(hostBytes[i] | (byte)~maskBytes[i]);
            }
            return new IPAddress(broadcastIPBytes);
        }

        public void Listener()
        {
            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Interval = 20000;

            aTimer.Elapsed += SendBroadcast;

            aTimer.AutoReset = true;

            aTimer.Enabled = true;


            Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint iep = new IPEndPoint(IPAddress.Any, 8008);
            sock.Bind(iep);
            sock.EnableBroadcast = true;
            EndPoint groupEP = (EndPoint)iep;
            byte[] buffer = new byte[256];

            try
            {
                while (true)
                {
                    Console.WriteLine("Waiting for broadcast");
                    sock.ReceiveFrom(buffer, ref groupEP);


                    IPAddress target_ip = ((IPEndPoint)groupEP).Address;
                    bool isAlreadyConneted = false;
                    foreach(IPAddress ip in connected)
                    {
                        if (target_ip.Equals(ip))
                        {
                            Console.WriteLine($"Exit pls2");
                            isAlreadyConneted = true;
                        }
                    }
                    if (isAlreadyConneted)
                    {
                        continue;
                    }

                    Console.WriteLine($"Received broadcast from {groupEP}");
                    Console.WriteLine($" {Encoding.UTF8.GetString(buffer)}");

                    sock.SendTo(Encoding.UTF8.GetBytes(Dns.GetHostName()), groupEP);

                    P2PDecide(groupEP, target_ip, sock);

                    connected.Add(target_ip);


                }
            }
            catch (SocketException e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                sock.Close();
            }
        }

        public void SendBroadcast(Object source, ElapsedEventArgs e)
        {
            
            if (new IPAddress(d.IpAddress).ToString() != "0.0.0.0")
            {
                Console.WriteLine("My IP IS: {0}", new IPAddress(d.IpAddress).ToString());
                Console.WriteLine("My MASK IS: {0}", new IPAddress(d.Netmask).ToString());
                Console.WriteLine("My BROADCAST IS: {0}", GetBroadCastIP(new IPAddress(d.IpAddress), new IPAddress(d.Netmask)));



                int broadcastPort = 8008;
                IPAddress broadcastIp = GetBroadCastIP(new IPAddress(d.IpAddress), new IPAddress(d.Netmask));
                IPEndPoint destinationEndpoint = new IPEndPoint(broadcastIp, broadcastPort);
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                sock.ReceiveTimeout = 2000;
                byte[] buffer = new Byte[256];

                int retries = 0;
                int maxRetries = 3;

                IPEndPoint iep = new IPEndPoint(IPAddress.Any, 8008);
                EndPoint groupEP = (EndPoint)iep;
                do
                {
                    sock.SendTo(Encoding.UTF8.GetBytes(Dns.GetHostName()), destinationEndpoint);
                    retries++;
                    try
                    {
                        sock.ReceiveFrom(buffer, ref groupEP);
                        break;
                    }
                    catch { }
                } while (retries < maxRetries);
                if (retries == maxRetries)
                {
                    sock.Close();
                    Console.WriteLine("No reply");
                    return;
                }


                IPAddress target_ip = ((IPEndPoint)groupEP).Address;
                string remoteHostname = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

                P2PDecide(groupEP, target_ip, sock);

                connected.Add(target_ip);
                
                sock.Close();
            }
            else
            {
                Console.WriteLine("No Wifi");
            }
        }

        private void Server(TcpListener server)
        {
            try
            {
                // Buffer for reading data
                Byte[] bytes = new Byte[256];
                String data = null;

                // Enter the listening loop.
                Console.Write("Waiting for a connection... ");

                // Perform a blocking call to accept requests.
                // You could also use server.AcceptSocket() here.
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Connected!");


                // Get a stream object for reading and writing
                NetworkStream networkStream = client.GetStream();


                byte[] message = Encoding.UTF8.GetBytes("host");
                networkStream.Write(message, 0, message.Length);
                message = Encoding.UTF8.GetBytes(Dns.GetHostName());
                networkStream.Write(message, 0, message.Length);

                int i;

                // Loop to receive all the data sent by the client.
                while ((i = networkStream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    // Translate data bytes to a UTF8 string.
                    data = Encoding.UTF8.GetString(bytes, 0, i);
                    Console.WriteLine("Received: {0}", data);


                    //message = Encoding.UTF8.GetBytes("hi");

                    // Send back a response.
                    //networkStream.Write(message, 0, message.Length);
                    //Console.WriteLine("Sent: {0}", Encoding.UTF8.GetString(message, 0, message.Length));
                    switch (data)
                    {
                        case "autosync":
                            //ZABALIT POSIELANIE SUBOROV DO TRY KVOLI RANDOM DISCONNECTOM
                            FileManager.AddSyncTarget(((IPEndPoint)client.Client.RemoteEndPoint).Address);
                            break;
                        case "file":


                            break;
                        case "end":
                            byte[] msg = Encoding.UTF8.GetBytes("end");
                            networkStream.Write(msg, 0, msg.Length);
                            networkStream.Close();
                            client.Close();
                            goto End;
                        //break;
                        default:
                            Console.WriteLine(data);
                            break;
                    }
                }
            End:
                Console.WriteLine("END");

                // Shutdown and end connection
                networkStream.Close();
                client.Close();
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException: {0}", ex);
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }
            server.Stop();
        }

        private void Client(IPAddress server, int port)
        {
            Console.WriteLine($"Connecting to: {server}:{port}");
            TcpClient client = new TcpClient(server.ToString(), port);
            byte[] data = Encoding.ASCII.GetBytes("end");
            NetworkStream networkStream = client.GetStream();
            string remoteHostname = String.Empty;
            while (true)
            {
                networkStream.Write(data, 0, data.Length);
                data = new byte[256];
                String responseData = String.Empty;
                Int32 bytes = networkStream.Read(data, 0, data.Length);
                responseData = Encoding.ASCII.GetString(data, 0, bytes);
                Console.WriteLine("Received: {0}", responseData);
                switch (responseData)
                {
                    case "host":
                        bytes = networkStream.Read(data, 0, data.Length);
                        remoteHostname = Encoding.ASCII.GetString(data, 0, bytes);
                        break;
                    case "autosync":
                        //FileManager.AddSyncTarget(remoteHostname);
                        break;
                    case "file":
                        string f = $"{AppContext.BaseDirectory}/music/Mori Calliope Ch. hololive-EN/[Original Rap] DEAD BEATS - Calliope Mori holoMyth hololiveEnglish.mp3";
                        //(TcpListener receiveServer, int listenPort) = StartServer();


                        break;
                    case "end":
                        byte[] message = Encoding.ASCII.GetBytes("end");
                        networkStream.Write(message, 0, message.Length);
                        networkStream.Close();
                        client.Close();
                        goto End;
                    //break;
                    default:
                        Console.WriteLine(responseData);
                        break;
                }
            }
            End:
            // Close everything.
            networkStream.Close();
            client.Close();
        }

        private void P2PDecide(EndPoint groupEP, IPAddress target_ip, Socket sock)
        {
            int state;
            EndPoint endPoint = groupEP;
            byte[] buffer = new Byte[32];
            while (true)
            {
                state = new Random().Next(0, 1);
                sock.SendTo(BitConverter.GetBytes(state), groupEP);
                sock.ReceiveFrom(buffer, ref endPoint);
                while (((IPEndPoint)endPoint).Address != target_ip)
                {
                    sock.ReceiveFrom(buffer, ref endPoint);
                }
                int resp = BitConverter.ToInt32(buffer);
                if (resp == 0 || resp == 1)
                {
                    if (state != resp)
                    {
                        if (state == 0)
                        {
                            //server
                            Console.WriteLine("Server");
                            (TcpListener server, int listenPort) = StartServer(new IPAddress(d.IpAddress));
                            sock.SendTo(BitConverter.GetBytes(listenPort), groupEP);
                            new Thread(() => { Server(server); }).Start();
                        }
                        else
                        {
                            //client
                            Console.WriteLine("Client");
                            sock.ReceiveFrom(buffer, ref groupEP);
                            int sendPort = BitConverter.ToInt32(buffer);
                            new Thread(() => { Client(((IPEndPoint)groupEP).Address, sendPort); }).Start();
                        }
                    }
                }
            }
        }

        public static (TcpListener, int) StartServer(IPAddress ip)
        {
            int listenPort = new Random().Next(1024, 65535);
            TcpListener server;
            while (true)
            {
                try
                {
                    server = new TcpListener(ip, listenPort);
                    server.Start();
                    MemoryStream memoryStream = new MemoryStream();
                    break;
                }
                catch
                {
                    listenPort = new Random().Next(1024, 65535);
                }
            }
            Console.WriteLine(listenPort);
            return (server, listenPort);
        }
    }
}