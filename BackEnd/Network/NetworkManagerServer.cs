using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP.BackEnd.Network
{
    internal static class NetworkManagerServer
    {
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
        
        internal static void Server(TcpListener server, IPAddress targetIp, List<Song> songsToSend)
        {
            try
            {
                // Buffer for reading data
                byte[] bytes = new byte[256];

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
                    string data = Encoding.UTF8.GetString(bytes, 0, i);
#if DEBUG
                    MyConsole.WriteLine($"Received: {data}");
#endif

                    // Send back a response.
                    //networkStream.Write(message, 0, message.Length);
                    //Console.WriteLine("Sent: {0}", Encoding.UTF8.GetString(message, 0, message.Length));
                    switch (data)
                    {
                        case "autosync":
                            //TODO: ZABALIT POSIELANIE SUBOROV DO TRY KVOLI RANDOM DISCONNECTOM
                            //FileManager.AddSyncTarget("");
                            break;
                        case "file":


                            break;
                        case "end":
                            byte[] msg = Encoding.UTF8.GetBytes("end");
                            networkStream.Write(msg, 0, msg.Length);
                            networkStream.Close();
                            client.Close();
                            goto EndServer;
                        //break;
                        default:
#if DEBUG
                            MyConsole.WriteLine(data);
#endif
                            break;
                    }
                }
                EndServer:
#if DEBUG
                MyConsole.WriteLine("END");
#endif
                NetworkManagerCommon.Connected.Remove(targetIp);
                // Shutdown and end connection
                networkStream.Close();
                client.Close();
            }
            catch (SocketException ex)
            {
#if DEBUG
                MyConsole.WriteLine("SocketException: ");
                MyConsole.WriteLine(ex);
#endif
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }
            server.Stop();
        }
    }
}