using Android.App;
using Android.Content;
using Android.Net.Wifi;
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

namespace Ass_Pain
{
    internal class NetworkManager
    {
        
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

        public void WifiTest(object sender, EventArgs e)
        {
            WifiManager wifiManager = (WifiManager)Application.Context.GetSystemService(Service.WifiService);

            var d = wifiManager.DhcpInfo;
            if (new IPAddress(d.IpAddress).ToString() != "0.0.0.0")
            {
                Console.WriteLine("My IP IS: {0}", new IPAddress(d.IpAddress).ToString());
                Console.WriteLine("My MASK IS: {0}", new IPAddress(d.Netmask).ToString());
                Console.WriteLine("My BROADCAST IS: {0}", GetBroadCastIP(new IPAddress(d.IpAddress), new IPAddress(d.Netmask)));

                int listenPort = 8009;
                TcpListener server;
                while (true)
                {
                    try
                    {
                        server = new TcpListener(new IPAddress(d.IpAddress), listenPort);
                        server.Start();
                        break;
                    }
                    catch
                    {
                        listenPort = new Random().Next(1024, 65535);
                    }
                }
                Console.WriteLine(listenPort);


                int broadcastPort = 8008;
                IPAddress broadcastIp = GetBroadCastIP(new IPAddress(d.IpAddress), new IPAddress(d.Netmask));
                IPEndPoint destinationEndpoint = new IPEndPoint(broadcastIp, broadcastPort);
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                sock.ReceiveTimeout = 2000;
                byte[] buffer = new Byte[256];

                int retries = 0;
                int maxRetries = 3;
                do {
                    sock.SendTo(BitConverter.GetBytes(listenPort), destinationEndpoint);
                    retries++;
                    try
                    {
                        sock.Receive(buffer);
                        break;
                    }
                    catch
                    {
                    }
                } while (retries < maxRetries);
                if(retries == maxRetries)
                {
                    sock.Close();
                    server.Stop();
                    Console.WriteLine("No reply");
                    return;
                }
                sock.Close();

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

                    int i;

                    // Loop to receive all the data sent by the client.
                    while ((i = networkStream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        data = Encoding.ASCII.GetString(bytes, 0, i);
                        Console.WriteLine("Received: {0}", data);


                        //byte[] message = Encoding.ASCII.GetBytes("hi");

                        // Send back a response.
                        //networkStream.Write(message, 0, message.Length);
                        //Console.WriteLine("Sent: {0}", Encoding.ASCII.GetString(message, 0, message.Length));
                        switch (data)
                        {
                            case "autosync":
                                //ZABALIT POSIELANIE SUBOROV DO TRY KVOLI RANDOM DISCONNECTOM
                                FileManager.AddSyncTarget(((IPEndPoint)client.Client.RemoteEndPoint).Address);
                                break;
                            case "file":


                                break;
                            case "end":
                                byte[] msg = Encoding.ASCII.GetBytes("end");
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
            else
            {
                Console.WriteLine("No Wifi");
            }
        }
    }
}