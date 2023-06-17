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
using static Android.Provider.DocumentsContract;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Xml.Serialization;
using Xamarin.Essentials;
using NetworkAccess = Xamarin.Essentials.NetworkAccess;
#if DEBUG
using Ass_Pain.Helpers;
#endif

//using Android.Net;

namespace Ass_Pain
{
    public class NetworkManager
    {
        private bool canSend = false;
        static Android.Net.Wifi.WifiManager wifiManager = (Android.Net.Wifi.WifiManager)Application.Context.GetSystemService(Service.WifiService);

        static Android.Net.DhcpInfo d = wifiManager.DhcpInfo;
        private IPAddress myIP;
        private List<IPAddress> connected;
        private IPAddress broadcast;

        static IPAddress GetBroadCastIP(IPAddress host, IPAddress mask)
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
            Connectivity.ConnectivityChanged += OnWiFiChange;
            GetConnectionInfo();
            
            
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Q) {         
                // Use new API here
            } else {
                // Use old API here
            }
            
            System.Timers.Timer aTimer = new System.Timers.Timer();
            aTimer.Interval = 20000;

            aTimer.Elapsed += SendBroadcast;

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
            EndPoint groupEP = (EndPoint)iep;
            byte[] buffer = new byte[256];

            try
            {
                Start:
                if (canSend == false)
                {
                    Thread.Sleep(20000);
                }

                while (canSend)
                {
#if DEBUG
                    MyConsole.WriteLine("Waiting for broadcast");
#endif
                    sock.ReceiveFrom(buffer, ref groupEP);


                    IPAddress target_ip = ((IPEndPoint)groupEP).Address;
                    bool isAlreadyConneted = false;
                    foreach(IPAddress ip in connected)
                    {
                        if (target_ip.Equals(ip))
                        {
                            Console.WriteLine($"Exit pls2");
                            isAlreadyConneted = true;
                            break;
                        }
                    }
                    if (isAlreadyConneted)
                    {
                        continue;
                    }
#if DEBUG
                    MyConsole.WriteLine($"Received broadcast from {groupEP}");
                    MyConsole.WriteLine($" {Encoding.UTF8.GetString(buffer)}");
#endif

                    sock.SendTo(Encoding.UTF8.GetBytes(Dns.GetHostName()), groupEP);

                    connected.Add(target_ip);

                    P2PDecide(groupEP, target_ip, sock);
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

        public void SendBroadcast(Object source = null, ElapsedEventArgs e = null)
        {
            
            if (canSend)
            {
#if DEBUG
                MyConsole.WriteLine("My IP IS: {0}", myIP.ToString());
                MyConsole.WriteLine("My MASK IS: {0}", new IPAddress(BitConverter.GetBytes(d.Netmask)).ToString());
                MyConsole.WriteLine("My BROADCAST IS: {0}", GetBroadCastIP(myIP, new IPAddress(BitConverter.GetBytes(d.Netmask))).ToString());
#endif



                int broadcastPort = 8008;
                IPEndPoint destinationEndpoint = new IPEndPoint(broadcast, broadcastPort);
                Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
                sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                sock.ReceiveTimeout = 2000;
                byte[] buffer = new Byte[256];

                int retries = 0;
                const int maxRetries = 3;

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
#if DEBUG
                    MyConsole.WriteLine("No reply");
#endif
                    return;
                }


                IPAddress targetIp = ((IPEndPoint)groupEP).Address;
                string remoteHostname = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

                connected.Add(targetIp);

                P2PDecide(groupEP, targetIp, sock);
                
                sock.Close();
            }
            else
            {
#if DEBUG
                MyConsole.WriteLine("No Wifi");
#endif
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
#if DEBUG
                MyConsole.WriteLine("Waiting for a connection... ");
#endif

                // Perform a blocking call to accept requests.
                // You could also use server.AcceptSocket() here.
                TcpClient client = server.AcceptTcpClient();
#if DEBUG
                MyConsole.WriteLine("Connected!");
#endif


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
#if DEBUG
                    MyConsole.WriteLine("Received: {0}", data);
#endif


                    //message = Encoding.UTF8.GetBytes("hi");

                    // Send back a response.
                    //networkStream.Write(message, 0, message.Length);
                    //Console.WriteLine("Sent: {0}", Encoding.UTF8.GetString(message, 0, message.Length));
                    switch (data)
                    {
                        case "autosync":
                            //ZABALIT POSIELANIE SUBOROV DO TRY KVOLI RANDOM DISCONNECTOM
                            //FileManager.AddSyncTarget("");
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
#if DEBUG
                            MyConsole.WriteLine(data);
#endif
                            break;
                    }
                }
            End:
#if DEBUG
                MyConsole.WriteLine("END");
#endif
                connected.Remove(target_ip);
                // Shutdown and end connection
                networkStream.Close();
                client.Close();
            }
            catch (SocketException ex)
            {
#if DEBUG
                MyConsole.WriteLine($"SocketException: {ex}");
#endif
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }
            server.Stop();
        }

        private async Task Client(IPAddress server, int port)
        {
#if DEBUG
            MyConsole.WriteLine($"Connecting to: {server}:{port}");
#endif
            TcpClient client = new TcpClient(server.ToString(), port);
            NetworkStream networkStream = client.GetStream();
            int command = 0;
            byte[] recCommand = new byte[256];
            byte[] sendCommand = new byte[256];
            byte[] recLength = new byte[256];
            byte[] sendLength = new byte[256];
            int length = 0;
            bool ending = false;
            string remoteHostname = String.Empty;
            bool canSend = false;
            bool encrypted = false;

            RSACryptoServiceProvider encryptor = new RSACryptoServiceProvider();
            RSACryptoServiceProvider decryptor = new RSACryptoServiceProvider();
            Aes aes = Aes.Create();

            List<string> files = new List<string>();
            List<string> sent = new List<string>();

            byte[] host = Encoding.UTF8.GetBytes(DeviceInfo.Name);
            networkStream.Write(BitConverter.GetBytes(10), 0, 4);
            networkStream.Write(BitConverter.GetBytes(host.Length), 0, 4);
            networkStream.Write(host, 0, host.Length);

            while (true)
            {
                Thread.Sleep(50);

                if (networkStream.DataAvailable)
                {
                    if (encrypted)
                    {
                        networkStream.Read(recCommand, 0, 256);
                        command = BitConverter.ToInt32(decryptor.Decrypt(recCommand, true), 0);
                    }
                    else
                    {
                        networkStream.Read(recCommand, 0, 4);
                        command = BitConverter.ToInt32(recCommand, 0);
                    }
                }
#if DEBUG
                MyConsole.WriteLine($"Received command: {command}");
#endif
                if (files.Count > 0)
                {

                }
                else if (ending && !networkStream.DataAvailable)
                {
                    sendCommand = BitConverter.GetBytes(100);
                    sendCommand = encryptor.Encrypt(sendCommand, true);
                    networkStream.Write(sendCommand, 0, sendCommand.Length);
#if DEBUG
                    MyConsole.WriteLine("send end");
#endif
                }
                else
                {
                    if (command != 10)
                    {
                        try
                        {
                            sendCommand = BitConverter.GetBytes(0);
                            sendCommand = encryptor.Encrypt(sendCommand, true);
                            networkStream.Write(sendCommand, 0, sendCommand.Length);
                        }
                        catch
                        {
#if DEBUG
                            MyConsole.WriteLine("shut");
#endif
                            Thread.Sleep(100);
                        }
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
#if DEBUG
                        MyConsole.WriteLine($"hostname {remoteHostname}");
#endif
                        //SecureStorage.RemoveAll();
                        //add some statement to retrieve existing key;
                        string storedPrivKey, storedPubKey;
                        try
                        {
                            storedPrivKey = await SecureStorage.GetAsync($"{remoteHostname}_privkey");
                            storedPubKey = await SecureStorage.GetAsync($"{remoteHostname}_pubkey");
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("You're fucked, boy. Go buy something else than Nokia 3310");
                        }
                        if (storedPrivKey != null && storedPubKey != null)
                        {
                            encryptor.FromXmlString(storedPubKey);
                            decryptor.FromXmlString(storedPrivKey);
                        }
                        else
                        {
#if DEBUG
                            MyConsole.WriteLine("generating keys");
#endif
                            (string pubKeyString, RSAParameters privKey) = CreateKeyPair();
                            networkStream.Write(BitConverter.GetBytes(11), 0, 4);
                            data = Encoding.UTF8.GetBytes(pubKeyString);
                            networkStream.Write(BitConverter.GetBytes(data.Length), 0, 4);
                            networkStream.Write(data, 0, data.Length);

                            while (!networkStream.DataAvailable)
                            {
                                Thread.Sleep(10);
                            }

                            networkStream.Read(recCommand, 0, 4);
                            command = BitConverter.ToInt32(recCommand);
#if DEBUG
                            MyConsole.WriteLine($"command for enc {command}");
#endif
                            if (command != 11)
                            {
                                throw new Exception("wrong order to establish cypher");
                            }
                            networkStream.Read(recLength, 0, 4);
                            length = BitConverter.ToInt32(recLength, 0);
                            data = new byte[length];
                            //networkStream.Read(data, 0, length);
#if DEBUG
                            MyConsole.WriteLine($"Read {networkStream.Read(data, 0, length)}");
#endif
                            decryptor.ImportParameters(privKey);
                            pubKeyString = Encoding.UTF8.GetString(data, 0, length);
                            encryptor.FromXmlString(pubKeyString);

                            //store private and public key for later use
                            SecureStorage.SetAsync($"{remoteHostname}_privkey", decryptor.ToXmlString(true)).Wait();
                            SecureStorage.SetAsync($"{remoteHostname}_pubkey", pubKeyString).Wait();
                        }
#if DEBUG
                        //MyConsole.WriteLine($"{decryptor.ToXmlString(true)}\n{encryptor.ToXmlString(false)}");
                        MyConsole.WriteLine("1");
#endif
                        while (!networkStream.DataAvailable)
                        {
#if DEBUG
                            MyConsole.WriteLine("wt");
#endif
                            Thread.Sleep(10);
                        }
#if DEBUG
                        MyConsole.WriteLine("2");
#endif
                        networkStream.Read(recCommand, 0, 256);
#if DEBUG
                        MyConsole.WriteLine("21");
#endif
                        byte[] a = decryptor.Decrypt(recCommand, true);
#if DEBUG
                        MyConsole.WriteLine("22");
#endif
                        command = BitConverter.ToInt32(a, 0);
#if DEBUG
                        MyConsole.WriteLine($"command for AES {command}");
                        MyConsole.WriteLine("23");
#endif
                        if (command != 12)
                        {
                            throw new Exception("wrong order to establish cypher AES");
                        }
#if DEBUG
                        MyConsole.WriteLine("3");
#endif
                        aes.KeySize = 256;
#if DEBUG
                        MyConsole.WriteLine("waiting");
#endif
                        while (!networkStream.DataAvailable)
                        {
#if DEBUG
                            MyConsole.WriteLine("wt2");
#endif
                            Thread.Sleep(10);
                        }
                        networkStream.Read(recLength, 0, 256);
                        aes.Key = decryptor.Decrypt(recLength, true);

                        sendCommand = BitConverter.GetBytes(13);
                        sendCommand = encryptor.Encrypt(sendCommand, true);
                        networkStream.Write(sendCommand, 0, sendCommand.Length);

                        encrypted = true;
#if DEBUG
                        MyConsole.WriteLine("ecnrypted");
#endif

                        (bool exists, List<string> songs) = FileManager.GetSyncSongs(remoteHostname);
                        if (exists)
                        {
                            if(songs.Count > 0)
                            {
                                files = songs;
                            }
                            else
                            {
                                ending = true;
                            }
                        }
                        else
                        {
                            ending = true;
                        }
                        break;
                    case 20: //sync
                        if (FileManager.GetTrustedHost(remoteHostname))
                        {
                            sendCommand = BitConverter.GetBytes(21);
                            sendCommand = encryptor.Encrypt(sendCommand, true);
                            networkStream.Write(sendCommand, 0, sendCommand.Length);
                        }
                        else
                        {
                            sendCommand = BitConverter.GetBytes(22);
                            sendCommand = encryptor.Encrypt(sendCommand, true);
                            networkStream.Write(sendCommand, 0, sendCommand.Length);

                            while (!networkStream.DataAvailable)
                            {
                                Thread.Sleep(10);
                            }
                            byte[] ivBuffer;
                            do
                            {
                                networkStream.Read(recLength, 0, 256);
                                ivBuffer = decryptor.Decrypt(recLength, true);
                            } while (ivBuffer.Length == 4);
#if DEBUG
                            MyConsole.WriteLine($"length IV {ivBuffer.Length}");
#endif
                            aes.IV = ivBuffer;


                            while (!networkStream.DataAvailable)
                            {
                                Thread.Sleep(10);
                            }

                            networkStream.Read(recLength, 0, 256);
                            length = BitConverter.ToInt32(decryptor.Decrypt(recLength, true), 0);

                            byte[] recList = new byte[length];


                            while (!networkStream.DataAvailable)
                            {
                                Thread.Sleep(10);
                            }
                            networkStream.Read(recList, 0, length);

                            byte[] decBuffer = new byte[length];
                            using (MemoryStream msDecrypt = new MemoryStream(recList))
                            {
                                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read))
                                {
                                    csDecrypt.Read(decBuffer, 0, length);
                                }
                            }
                            string json = Encoding.UTF8.GetString(decBuffer);


#if DEBUG
                            MyConsole.WriteLine(json);
#endif
                            List<string> recSongs = JsonConvert.DeserializeObject<List<string>>(json);
                            bool x = false;
                            foreach(string s in recSongs)
                            {
#if DEBUG
                                MyConsole.WriteLine(s);
#endif
                            }
                            x = true;
                            if (x) //present some form of user check if they really want to receive files
                            {
                                sendCommand = BitConverter.GetBytes(21);
                                sendCommand = encryptor.Encrypt(sendCommand, true);
                                networkStream.Write(sendCommand, 0, sendCommand.Length);
                                FileManager.AddTrustedHost(remoteHostname);
                            }
                            else
                            {
                                sendCommand = BitConverter.GetBytes(23);
                                sendCommand = encryptor.Encrypt(sendCommand, true);
                                networkStream.Write(sendCommand, 0, sendCommand.Length);
                            }
                        }
                        break;
                    case 30: //file
                        int i = FileManager.GetAvailableFile("receive");
                        string musicPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                        string path = $"{Application.Context.GetExternalFilesDir(null).AbsolutePath}/tmp/receive{i}.mp3";

                        networkStream.Read(recLength, 0, 256);
                        aes.IV = decryptor.Decrypt(recLength, true);

                        byte[] recFileLength = new byte[256];
                        networkStream.Read(recFileLength, 0, 256);
#if DEBUG
                        //MyConsole.WriteLine($"rec ;en {decryptor.Decrypt(recFileLength, true).Length}");
#endif
                        Int64 fileLength = BitConverter.ToInt64(decryptor.Decrypt(recFileLength, true), 0);
                        if(fileLength > 4000000000){
                            throw new Exception("You can't receive files larger than 4GB on Android");
                        }
                        int readLength;
#if DEBUG
                        MyConsole.WriteLine($"File size {fileLength}");
#endif

                        using(MemoryStream msDecrypt = new MemoryStream())
                        {

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
                                msDecrypt.Write(file, 0, minus);
                                Console.WriteLine($"Writing {minus} bytes");
                            }
                            msDecrypt.Position = 0;
                            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read))
                            {
                                using (FileStream stream = new FileStream(path, FileMode.Append))
                                {
                                    csDecrypt.CopyTo(stream);
                                }
                            }
                        }


                        string name = FileManager.Sanitize(FileManager.GetSongTitle(path));
                        string artist = FileManager.Sanitize(FileManager.GetAlias(FileManager.GetSongArtist(path)[0]));
                        string unAlbum = FileManager.GetSongAlbum(path);
                        if(unAlbum == null)
                        {
                            Directory.CreateDirectory($"{musicPath}/{artist}");
                            if (!File.Exists($"{musicPath}/{artist}/{name}.mp3"))
                            {
                                File.Move(path, $"{musicPath}/{artist}/{name}.mp3");
                            }
                            else
                            {
                                File.Delete(path);
                            }
                        }
                        else
                        {
                            string album = FileManager.Sanitize(unAlbum);
                            Directory.CreateDirectory($"{musicPath}/{artist}/{album}");
                            if (!File.Exists($"{musicPath}/{artist}/{album}/{name}.mp3"))
                            {
                                File.Move(path, $"{musicPath}/{artist}/{album}/{name}.mp3");
                            }
                            else
                            {
                                File.Delete(path);
                            }
                        }
                        break;
                    case 100: //end
#if DEBUG
                        MyConsole.WriteLine("got end");
#endif
                        if (files.Count > 0)//if work to do
                        {
#if DEBUG
                            MyConsole.WriteLine("Still work to do");
#endif
                            continue;
                        }
                        try
                        {
                            sendCommand = BitConverter.GetBytes(100);
                            sendCommand = encryptor.Encrypt(sendCommand, true);
                            networkStream.Write(sendCommand, 0, sendCommand.Length);
                            Thread.Sleep(100);
                        }
                        catch
                        {
#if DEBUG
                            MyConsole.WriteLine("Disconnected");
#endif
                        }
                        networkStream.Close();
                        client.Close();
                        goto End;
                    //break;
                    default: //wait or uninplemented
#if DEBUG
                        MyConsole.WriteLine($"default: {command}");
#endif
                        break;
                }
            }
        End:
            // Close everything.
#if DEBUG
            MyConsole.WriteLine("END");
#endif
            encryptor.Dispose();
            decryptor.Dispose();
            aes.Dispose();
            networkStream.Close();
            client.Close();
            networkStream.Dispose();
            client.Dispose();
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
#if DEBUG
                MyConsole.WriteLine($"sending {state} to {((IPEndPoint)groupEP).Address}");
#endif
                sock.ReceiveFrom(buffer, ref endPoint);
#if DEBUG
                MyConsole.WriteLine($"received {BitConverter.ToInt32(buffer)} from {((IPEndPoint)endPoint).Address}");
                MyConsole.WriteLine($"debug: {((IPEndPoint)endPoint).Address}  {target_ip}  {!((IPEndPoint)endPoint).Address.Equals(target_ip)}");
#endif
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
#if DEBUG
                            MyConsole.WriteLine("Server");
#endif
                            (TcpListener server, int listenPort) = StartServer(myIP);
                            sock.SendTo(BitConverter.GetBytes(listenPort), groupEP);
                            new Thread(() => { Server(server, target_ip); }).Start();
                        }
                        else
                        {
                            //client
#if DEBUG
                            MyConsole.WriteLine("Client");
#endif
                            sock.ReceiveFrom(buffer, ref groupEP);
                            int sendPort = BitConverter.ToInt32(buffer);
                            new Thread(() => { _ = Client(((IPEndPoint)groupEP).Address, sendPort); }).Start();
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
                    break;
                }
                catch
                {
                    listenPort = new Random().Next(1024, 65535);
                }
            }
#if DEBUG
            MyConsole.WriteLine(listenPort.ToString());
#endif
            return (server, listenPort);
        }

        private (string pubKeyString, RSAParameters privKey) CreateKeyPair()
        {
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(2048);
            string pubKey = csp.ToXmlString(false);
            RSAParameters privKey = csp.ExportParameters(true);
            csp.Dispose();
            return (pubKey, privKey);
        }

        public void OnWiFiChange(object sender, ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess == NetworkAccess.Internet || e.NetworkAccess == NetworkAccess.Local)
            {
                GetConnectionInfo();
                canSend = true;
            }
            else
            {
                canSend = false;
            }
        }

        private void GetConnectionInfo()
        {
            connected = new List<IPAddress>{ { myIP } };
            myIP = new IPAddress(BitConverter.GetBytes(d.IpAddress));
            broadcast = GetBroadCastIP(myIP, new IPAddress(BitConverter.GetBytes(d.Netmask)));
        }
    }
}