using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xamarin.Essentials;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP.BackEnd.Network
{
    

internal static class NetworkManagerServer
{
    internal static (TcpListener, int) StartServer(IPAddress ip)
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
        MyConsole.WriteLine($"{listenPort}");
#endif
        return (server, listenPort);
    }
    
    internal static void Server(TcpListener server, IPAddress targetIp, List<Song> songsToSend, ref EndPoint endPoint, ref Socket sock, Dictionary<byte, byte> local)
    {
        try
        {
            bool ending = false;
            EncryptionState encryptionState = EncryptionState.None;
            SyncRequestState syncRequestState = SyncRequestState.None;
            SongSendRequestState songSendRequestState = SongSendRequestState.None;
            string remoteHostname = string.Empty;

            RSACryptoServiceProvider encryptor = new RSACryptoServiceProvider();
            RSACryptoServiceProvider decryptor = new RSACryptoServiceProvider();
            Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateKey();
            int ackCount = 0;
            List<Song> syncSongs = new List<Song>();
            bool? isTrustedSyncTarget = null;
            int timeoutCounter = 0;

            List<string> files = new List<string>();
            Dictionary<string, string> albumArtistPair = new Dictionary<string, string>();
            List<string> artistImageRequests = new List<string>();
            List<string> albumImageRequests = new List<string>();

            // Enter the listening loop.
#if DEBUG
            MyConsole.WriteLine("Waiting for a connection... ");
#endif
            

            // Perform a blocking call to accept requests.
            Task<TcpClient> task = server.AcceptTcpClientAsync();
            
            while (!task.IsCompleted)
            {
                EndPoint eP = endPoint;
                if (sock.Available >= 4)
                {
                    byte[] buffer = new byte[4];
                    sock.ReceiveFrom(buffer, 4, SocketFlags.None, ref endPoint);
                    P2PState stateObject = new P2PState(buffer);
                    if (stateObject is { IsValid: true, Type: P2PStateTypes.Request })
                    {
                        sock.SendTo( P2PState.Send(stateObject.Cnt, local[stateObject.Cnt]), eP);
                    }
                }
                else
                {
                    Thread.Sleep(5);
                    sock.SendTo( P2PState.Send(local.Keys.Last(), local[local.Keys.Last()]), eP);
                }
            }
            
            sock.Dispose();

            TcpClient client = task.Result;
#if DEBUG
            MyConsole.WriteLine("Connected!");
#endif
            


            // Get a stream object for reading and writing
            NetworkStream networkStream = client.GetStream();
            
            networkStream.WriteCommand(CommandsArr.Host, Encoding.UTF8.GetBytes(DeviceInfo.Name));

                

            Thread.Sleep(100);
            while (true)
            {
                CommandsEnum command;
                byte[]? data = null;
                long? length = null;

                #region Reading

                if (networkStream.DataAvailable)
                {
                    (command, data, length) = NetworkManagerCommonCommunication.Read(encryptionState, ref networkStream, ref decryptor, ref aes, true);
                    timeoutCounter = 0;
                }
                else
                {
                    timeoutCounter++;
                    if (timeoutCounter > NetworkManager.MaxTimeoutCounter)
                    {
                        goto EndServer;
                    }
                    command = CommandsEnum.None;
                }

#if DEBUG
                MyConsole.WriteLine($"Received command: {command}");
#endif
                #endregion
                

                #region Writing

                NetworkManagerCommonCommunication.Write(ref ending, command, encryptionState, ref networkStream,
                    ref encryptor, ref aes, ref songsToSend, ref syncSongs, ref syncRequestState,
                    ref songSendRequestState, ref isTrustedSyncTarget, ref ackCount, remoteHostname, ref artistImageRequests, ref albumImageRequests);

                #endregion

                switch (command)
                {
                    case CommandsEnum.Host:
                        NetworkManagerCommonCommunication.Host(ref remoteHostname, ref networkStream, ref decryptor, ref encryptor, ref aes, ref encryptionState, true);
                        break;
                    case CommandsEnum.RsaExchange:
                        if (data != null)
                        {
                            NetworkManagerCommonCommunication.RsaExchange(ref networkStream, ref data, remoteHostname,
                                ref encryptionState, ref decryptor, ref encryptor, ref aes, true);
                        }
                        break;
                    case CommandsEnum.AesSend:
                        throw new InvalidOperationException("Server doesn't receive aes request");
                    case CommandsEnum.AesReceived:
                        encryptionState = EncryptionState.Encrypted;
#if DEBUG
                        MyConsole.WriteLine("encrypted");
#endif
                        break;
                    case CommandsEnum.SyncRequest:
                        if (FileManager.IsTrustedSyncTarget(remoteHostname))
                        {
                            networkStream.WriteCommand(CommandsArr.SyncAccepted, ref encryptor);
                        }
                        else
                        {
                            networkStream.WriteCommand(CommandsArr.SyncRejected, ref encryptor);
                        }
                        break;
                    case CommandsEnum.SyncAccepted:
                        syncRequestState = SyncRequestState.Accepted;
                        break;
                    case CommandsEnum.SyncRejected:
                        syncRequestState = SyncRequestState.Rejected;
                        break;
                    case CommandsEnum.SongRequest:
                        break;
                    case CommandsEnum.SongRequestInfoRequest:
                        //get data
                        string json = JsonConvert.SerializeObject(files);
#if DEBUG
                        MyConsole.WriteLine(json);
#endif
                        byte[] msg = Encoding.UTF8.GetBytes(json);
                        networkStream.WriteCommand(CommandsArr.SongRequestInfo, msg, ref encryptor, ref aes);
                        break;
                    case CommandsEnum.SongRequestInfo:
                        break;
                    case CommandsEnum.SongRequestAccepted:
                        songSendRequestState = SongSendRequestState.Accepted;
                        break;
                    case CommandsEnum.SongRequestRejected:
                        songSendRequestState = SongSendRequestState.Rejected;
                        break;
                    case CommandsEnum.SongSend:
                        if (length != null && isTrustedSyncTarget != null)
                        {
#if DEBUG
                            MyConsole.WriteLine($"file length: {length}");
#endif
                            NetworkManagerCommonCommunication.SongSend(ref networkStream, ref encryptor, (long)length, ref aes,
                                ref albumArtistPair, (bool)isTrustedSyncTarget, remoteHostname, ref artistImageRequests, ref albumImageRequests);
                        }
                        break;
                    case CommandsEnum.ArtistImageSend:
                        if (data != null && length != null && isTrustedSyncTarget != null)
                        {
                            NetworkManagerCommonCommunication.ArtistImageSend(ref networkStream, ref encryptor, ref aes,
                                (long)length, data, (bool)isTrustedSyncTarget, ref artistImageRequests);
                        }
                        break;
                    case CommandsEnum.AlbumImageSend:
                        if (data != null && length != null && isTrustedSyncTarget != null)
                        {
                            NetworkManagerCommonCommunication.AlbumImageSend(ref networkStream, ref encryptor, ref aes,
                                (long)length, data, ref albumArtistPair, (bool)isTrustedSyncTarget, ref albumImageRequests);
                        }
                        break;
                    case CommandsEnum.ArtistImageRequest:
                        if (data != null)
                        {
                            NetworkManagerCommonCommunication.ArtistImageRequest(ref networkStream, ref encryptor, ref aes,
                                ref ackCount, data);
                        }
                        break;
                    case CommandsEnum.AlbumImageRequest:
                        if (data != null)
                        {
                            NetworkManagerCommonCommunication.AlbumImageRequest(ref networkStream, ref encryptor, ref aes,
                                ref ackCount, data);
                        }
                        break;
                    case CommandsEnum.ArtistImageNotFound:
                        if (data != null)
                        {
                            string artistName = Encoding.UTF8.GetString(data);
                            artistImageRequests.Remove(artistName);
                            string artistAlias = FileManager.GetAlias(artistName);
                            artistImageRequests.Remove(artistAlias);
                        }
                        break;
                    case CommandsEnum.AlbumImageNotFound:
                        if (data != null)
                        {
                            string albumName = Encoding.UTF8.GetString(data);
                            albumImageRequests.Remove(albumName);
                        }
                        break;
                    case CommandsEnum.Ack:
                        ackCount++;
                        break;
                    case CommandsEnum.End: //end
#if DEBUG
                        MyConsole.WriteLine("got end");
#endif
                        if (!ending || ackCount < 0)//if work to do
                        {
#if DEBUG
                            MyConsole.WriteLine("Still work to do");
#endif
                            continue;
                        }
                        try
                        {
                            if (encryptionState == EncryptionState.Encrypted)
                                networkStream.WriteCommand(CommandsArr.End, ref encryptor);
                            else
                                networkStream.WriteCommand(CommandsArr.End);
                            Thread.Sleep(100);
                        }
                        catch
                        {
#if DEBUG
                            MyConsole.WriteLine("Disconnected");
#endif
                        }
                        goto EndServer;
                    case CommandsEnum.Wait:
                        Thread.Sleep(25);
                        break;
                    case CommandsEnum.None:
                    default: //wait or unimplemented
#if DEBUG
                        MyConsole.WriteLine($"default: {command}");
#endif
                        Thread.Sleep(100);
                        break;
                }
            }
            EndServer:
            // Shutdown and end connection
#if DEBUG
            MyConsole.WriteLine("END");
#endif
            networkStream.Close();
            client.Close();
            encryptor.Dispose();
            decryptor.Dispose();
            aes.Dispose();
            networkStream.Close();
            client.Close();
            NetworkManagerCommon.Connected.Remove(targetIp);
            //GC.Collect();
        }
        catch (SocketException ex)
        {
#if DEBUG
            MyConsole.WriteLine(ex);
#endif
        }
        server.Stop();
        }   
    }
}
