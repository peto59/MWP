using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MWP_Backend.BackEnd;
using MWP_Backend.DatatypesAndExtensions;
using Newtonsoft.Json;
#if DEBUG
using MWP_Backend.BackEnd.Helpers;
using MWP.Helpers;
#endif

namespace MWP.BackEnd.Network
{
    

internal static class NetworkManagerServer
{
    internal static (TcpListener server, int listenPort) StartServer(IPAddress ip)
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
            RSACryptoServiceProvider encryptor = new RSACryptoServiceProvider();
            RSACryptoServiceProvider decryptor = new RSACryptoServiceProvider();
            Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateKey();
            
#if ANDROID
            Notifications? notification = null;
#endif
            ConnectionState connectionState = new ConnectionState(true, songsToSend);
            
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
                        sock.SendTo(
                            local.TryGetValue(stateObject.Cnt, out byte value)
                                ? P2PState.Send(stateObject.Cnt, value)
                                : P2PState.Send(stateObject.Cnt, local[local.Keys.Last()]), eP);
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
            if (songsToSend.Count > 0)
            {
                networkStream.WriteCommand(CommandsArr.OnetimeSend);
#if ANDROID
                notification = new Notifications(NotificationTypes.OneTimeSend);
#endif
            }
            networkStream.WriteCommand(CommandsArr.Host, Encoding.UTF8.GetBytes(NetworkManager.DeviceName));

                

            Thread.Sleep(100);
            try
            {
                while (true)
                {
                    CommandsEnum command;
                    byte[]? data = null;
                    long? length = null;

                    #region Reading

                    if (networkStream.DataAvailable)
                    {
                        (command, data, length) = NetworkManagerCommonCommunication.Read(ref networkStream, ref decryptor, ref aes, ref connectionState);
                        connectionState.timeoutCounter = 0;
                    }
                    else
                    {
                        connectionState.timeoutCounter++;
                        if (connectionState.timeoutCounter > NetworkManager.MaxTimeoutCounter)
                        {
#if ANDROID
                            notification?.Stage3(false, connectionState);
#endif
                            goto EndServer;
                        }
                        command = CommandsEnum.None;
                    }

                    connectionState.messagesCount++;

#if DEBUG
                    MyConsole.WriteLine($"Received command: {command}");
#endif
                    #endregion
                

                    #region Writing
#if ANDROID
                    NetworkManagerCommonCommunication.Write(command, ref networkStream,
                        ref encryptor, ref aes, ref connectionState, ref notification);
#elif LINUX
                    NetworkManagerCommonCommunication.Write(command, ref networkStream,
                        ref encryptor, ref aes, ref connectionState);
#endif

                    #endregion

                    switch (command)
                    {
                        case CommandsEnum.OnetimeSend:
#if DEBUG
                            MyConsole.WriteLine($"GOT ONE TIME SEND !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!{Environment.NewLine}!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!{Environment.NewLine}!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!{Environment.NewLine}!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!{Environment.NewLine}!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!{Environment.NewLine}");
#endif
                            connectionState.gotOneTimeSendFlag = true;
                            break;
                        case CommandsEnum.ConnectionAccepted:
                            connectionState.connectionWasAccepted = true;
                            break;
                        case CommandsEnum.ConnectionRejected:
                            connectionState.songSendRequestState = SongSendRequestState.Rejected;
                            break;
                        case CommandsEnum.Host:
#if ANDROID
                            NetworkManagerCommonCommunication.Host(ref networkStream, ref decryptor, ref encryptor, ref aes, ref connectionState, ref notification);
#elif LINUX
                            NetworkManagerCommonCommunication.Host(ref networkStream, ref decryptor, ref encryptor, ref aes, ref connectionState);
#endif
                            break;
                        case CommandsEnum.RsaExchange:
                            if (data != null)
                            {
                                NetworkManagerCommonCommunication.RsaExchange(ref networkStream, ref data, ref decryptor, ref encryptor, ref aes, ref connectionState);
                            }
#if DEBUG
                            else
                            {
                                MyConsole.WriteLine("Empty data on RsaExchange");
                            }
#endif
                            break;
                        case CommandsEnum.AesSend:
                            throw new InvalidOperationException("Server doesn't receive aes request");
                        case CommandsEnum.AesReceived:
                            connectionState.encryptionState = EncryptionState.Encrypted;
#if DEBUG
                            MyConsole.WriteLine("encrypted");
#endif
                            break;
                        case CommandsEnum.SyncRequest:
                            if (connectionState.isTrustedSyncTarget ?? false)
                            {
                                networkStream.WriteCommand(CommandsArr.SyncAccepted, ref encryptor);
                            }
                            else
                            {
                                networkStream.WriteCommand(CommandsArr.SyncRejected, ref encryptor);
                            }
                            break;
                        case CommandsEnum.SyncAccepted:
                            connectionState.syncRequestState = SyncRequestState.Accepted;
                            networkStream.WriteCommand(CommandsArr.SyncCount, BitConverter.GetBytes(connectionState.SyncSendCount), ref encryptor);
#if ANDROID
                            notification ??= new Notifications(NotificationTypes.Sync);
                            notification.Stage2(connectionState);
#endif
                            break;
                        case CommandsEnum.SyncRejected:
                            connectionState.syncRequestState = SyncRequestState.Rejected;
                            break;
                        case CommandsEnum.SyncCount:
                            if (data != null)
                            {
                                connectionState.syncReceiveCount = BitConverter.ToInt32(data);
                            }
                            break;
                        case CommandsEnum.SongSendRequest:
                            if (connectionState.UserAcceptedState == UserAcceptedState.ConnectionAccepted)
                            {
                                networkStream.WriteCommand(CommandsArr.SongRequestInfoRequest, ref encryptor);
                            }
                            else
                            {
                                connectionState.gotSongSendRequestCommand = true;
                            }
                            break;
                        case CommandsEnum.SongRequestInfoRequest:
                            SongJsonConverter customConverter = new SongJsonConverter(false);
                            networkStream.WriteCommand(CommandsArr.SongRequestInfo,
                                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(connectionState.songsToSend,
                                    customConverter)),
                                ref encryptor,
                                ref aes);
                            break;
                        case CommandsEnum.SongRequestInfo:
                            if (data != null)
                            {
#if ANDROID
                                NetworkManagerCommonCommunication.SongRequestInfo(ref networkStream, ref connectionState, data, ref encryptor, ref notification);
#elif LINUX
                                NetworkManagerCommonCommunication.SongRequestInfo(ref networkStream, ref connectionState, data, ref encryptor);
#endif
                            }
                            break;
                        case CommandsEnum.SongRequestAccepted:
#if ANDROID
                            notification?.Stage2(connectionState);
#endif
                            connectionState.songSendRequestState = SongSendRequestState.Accepted;
                            break;
                        case CommandsEnum.SongRequestRejected:
                            connectionState.songSendRequestState = SongSendRequestState.Rejected;
                            break;
                        case CommandsEnum.SongSend:
                            if (length != null)
                            {
#if DEBUG
                                MyConsole.WriteLine($"file length: {length}");
#endif
#if ANDROID
                                NetworkManagerCommonCommunication.SongSend(ref networkStream, ref encryptor, (long)length, ref aes, ref connectionState, ref notification);
#elif LINUX
                                NetworkManagerCommonCommunication.SongSend(ref networkStream, ref encryptor, (long)length, ref aes, ref connectionState);
#endif
                            }
                            break;
                        case CommandsEnum.ArtistImageSend:
                            if (data != null && length != null)
                            {
                                NetworkManagerCommonCommunication.ArtistImageSend(ref networkStream, ref encryptor, ref aes,
                                    (long)length, data, ref connectionState);
                            }
                            break;
                        case CommandsEnum.AlbumImageSend:
                            if (data != null && length != null)
                            {
                                NetworkManagerCommonCommunication.AlbumImageSend(ref networkStream, ref encryptor, ref aes,
                                    (long)length, data, ref connectionState);
                            }
                            break;
                        case CommandsEnum.ArtistImageRequest:
                            if (data != null)
                            {
                                NetworkManagerCommonCommunication.ArtistImageRequest(ref networkStream, ref encryptor, ref aes, data, ref connectionState);
                            }
                            break;
                        case CommandsEnum.AlbumImageRequest:
                            if (data != null)
                            {
                                NetworkManagerCommonCommunication.AlbumImageRequest(ref networkStream, ref encryptor, ref aes, data, ref connectionState);
                            }
                            break;
                        case CommandsEnum.ArtistImageNotFound:
                            if (data != null)
                            {
                                string artistName = Encoding.UTF8.GetString(data);
                                connectionState.artistImageRequests.Remove(artistName);
                                connectionState.artistImageRequests.Remove(FileManager.GetAlias(artistName));
                            }
                            break;
                        case CommandsEnum.AlbumImageNotFound:
                            if (data != null)
                            {
                                string albumName = Encoding.UTF8.GetString(data);
                                connectionState.albumImageRequests.Remove(albumName);
                            }
                            break;
                        case CommandsEnum.Ack:
                            connectionState.ackCount++;
                            break;
                        case CommandsEnum.End:
#if DEBUG
                            MyConsole.WriteLine("got end");
#endif
                            if (!connectionState.Ending)//if work to do
                            {
#if DEBUG
                                MyConsole.WriteLine("Still work to do");
#endif
                                continue;
                            }
                            try
                            {
                                if (connectionState.encryptionState == EncryptionState.Encrypted)
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
#if ANDROID
                            notification?.Stage3(true, connectionState);
#endif
                            goto EndServer;
                        case CommandsEnum.Wait:
                            Thread.Sleep(100);
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
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
#if ANDROID
                notification?.Stage3(connectionState.Ending, connectionState);
#endif
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
#if ANDROID
            notification?.Dispose();
#endif
            StateHandler.OneTimeReceiveSongs.Remove(connectionState.remoteHostname);
            StateHandler.OneTimeSendStates.Remove(connectionState.remoteHostname);
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
