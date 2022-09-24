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
using CliWrap;
using static Android.Provider.DocumentsContract;
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

        public async void Listener()
        {
            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Interval = 20000;

            aTimer.Elapsed += SendBroadcast;

            aTimer.AutoReset = true;

            //aTimer.Enabled = true;

            Thread.Sleep(1500);
            Console.WriteLine(Android.OS.Environment.ExternalStorageDirectory);
            var status = await Permissions.RequestAsync<Permissions.StorageRead>();
            Console.WriteLine(status);
            status = await Permissions.RequestAsync<Permissions.StorageWrite>();
            Console.WriteLine(status);
            var mp3Files = Directory.EnumerateFiles(Android.OS.Environment.GetExternalStoragePublicDirectory(null).AbsolutePath, "*.mp3", SearchOption.AllDirectories);
            foreach (string currentFile in mp3Files)
            {
                Console.WriteLine(currentFile);
            }
            Console.WriteLine("files emd");
            SendBroadcast();

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

                    connected.Add(target_ip);

                    P2PDecide(groupEP, target_ip, sock);



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

        public void SendBroadcast(Object source = null, ElapsedEventArgs e = null)
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
                    sock.SendTo(Encoding.UTF8.GetBytes(DeviceInfo.Name), destinationEndpoint);
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

                connected.Add(target_ip);

                P2PDecide(groupEP, target_ip, sock);
                
                sock.Close();
            }
            else
            {
                Console.WriteLine("No Wifi");
            }
        }

        private void Server(TcpListener server, IPAddress target_ip)
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
                connected.Remove(target_ip);
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
            NetworkStream networkStream = client.GetStream();
            string remoteHostname = String.Empty;
            int command = 0;
            byte[] recCommand = new byte[4];
            byte[] sendCommand = new byte[4];
            byte[] recLength = new byte[4];
            int length = 0;
            bool ending = false;
            bool fileToSend = false;
            while (true)
            {
                
                if (networkStream.DataAvailable)
                {
                    networkStream.Read(recCommand, 0, 4);
                    command = BitConverter.ToInt32(recCommand, 0);
                }
                else
                {
                    command = 0;
                }
                Console.WriteLine("Received command: {0}", command);
                if (fileToSend)
                {

                }
                else if (ending)
                {
                    sendCommand = BitConverter.GetBytes(100);
                    networkStream.Write(sendCommand, 0, sendCommand.Length);
                    Console.WriteLine("send end");
                }
                else
                {
                    try
                    {
                        sendCommand = BitConverter.GetBytes(0);
                        networkStream.Write(sendCommand, 0, sendCommand.Length);
                    }
                    catch
                    {
                        Console.WriteLine("shut");
                        Thread.Sleep(100);
                    }
                }
                switch (command)
                {
                    case 10: //host
                        networkStream.Read(recLength, 0, 4);
                        length = BitConverter.ToInt32(recLength, 0);
                        byte[] data = new byte[length];
                        networkStream.Read(data, 0, length);
                        remoteHostname = Encoding.UTF8.GetString(data, 0, length);
                        Console.WriteLine($"hostname {remoteHostname}");
                        ending = true;
                        break;
                    case 20: //autosync
                        //FileManager.AddSyncTarget(remoteHostname);
                        break;
                    case 30: //file
                        int i = FileManager.GetAvailableFile("receive");
                        string root = Application.Context.GetExternalFilesDir(null).AbsolutePath;
                        string path = $"{root}/tmp/receive{i}.mp3";
                        byte[] recFileLength = new byte[8];
                        networkStream.Read(recFileLength, 0, 8);
                        Int64 fileLength = BitConverter.ToInt64(recFileLength, 0);
                        if(fileLength > 4000000000){
                            throw new Exception("You can't receive files larger than 4GB on Android");
                        }
                        int readLength = 0;
                        Console.WriteLine($"File size {fileLength}");
                        while (fileLength > 0)
                        {
                            if (fileLength > int.MaxValue)
                            {
                                readLength = int.MaxValue;
                            }
                            else
                            {
                                readLength = Convert.ToInt32(fileLength);
                            }
                            byte[] file = new byte[readLength];
                            int minus = networkStream.Read(file, 0, readLength);
                            fileLength -= minus;
                            using (var stream = new FileStream(path, FileMode.Append))
                            {
                                Console.WriteLine($"Writing {minus} bytes");
                                stream.Write(file, 0, minus);
                            }
                        }

                        string name = FileManager.Sanitize(FileManager.GetSongTitle(path));
                        string artist = FileManager.GetAlias(FileManager.Sanitize(FileManager.GetSongArtist(path)[0]));
                        string unAlbum = FileManager.GetSongAlbum(path);
                        if(unAlbum == null)
                        {
                            Directory.CreateDirectory($"{root}/music/{artist}");
                            if (!File.Exists($"{root}/music/{artist}/{name}.mp3"))
                            {
                                File.Move(path, $"{root}/music/{artist}/{name}.mp3");
                            }
                            else
                            {
                                File.Delete(path);
                            }
                        }
                        else
                        {
                            string album = FileManager.Sanitize(unAlbum);
                            Directory.CreateDirectory($"{root}/music/{artist}/{album}");
                            if (!File.Exists($"{root}/music/{artist}/{album}/{name}.mp3"))
                            {
                                File.Move(path, $"{root}/music/{artist}/{album}/{name}.mp3");
                            }
                            else
                            {
                                File.Delete(path);
                            }
                        }
                        break;
                    case 100: //end
                        byte[] message = BitConverter.GetBytes(100);
                        Console.WriteLine("got end");
                        try
                        {
                            networkStream.Write(message, 0, message.Length);
                        }
                        catch
                        {
                            Console.WriteLine("Disconnected");
                        }
                        networkStream.Close();
                        client.Close();
                        goto End;
                    //break;
                    default: //wait or uninplemented
                        Console.WriteLine($"default: {command}");
                        break;
                }
            }
        End:
            // Close everything.
            Console.WriteLine("END");
            networkStream.Close();
            client.Close();
            connected.Remove(server);
        }

        private void P2PDecide(EndPoint groupEP, IPAddress target_ip, Socket sock)
        {
            int state;
            EndPoint endPoint = groupEP;
            byte[] buffer = new byte[4];
            while (true)
            {
                state = new Random().Next(0, 2);
                sock.SendTo(BitConverter.GetBytes(state), groupEP);
                Console.WriteLine($"sending {state} to {((IPEndPoint)groupEP).Address}");
                sock.ReceiveFrom(buffer, ref endPoint);
                Console.WriteLine($"received {BitConverter.ToInt32(buffer)} from {((IPEndPoint)endPoint).Address}");
                Console.WriteLine($"debug: {((IPEndPoint)endPoint).Address}  {target_ip}  {!((IPEndPoint)endPoint).Address.Equals(target_ip)}");
                while (!((IPEndPoint)endPoint).Address.Equals(target_ip))
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
                            new Thread(() => { Server(server, target_ip); }).Start();
                        }
                        else
                        {
                            //client
                            Console.WriteLine("Client");
                            sock.ReceiveFrom(buffer, ref groupEP);
                            int sendPort = BitConverter.ToInt32(buffer);
                            new Thread(() => { Client(((IPEndPoint)groupEP).Address, sendPort); }).Start();
                        }
                        return;
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