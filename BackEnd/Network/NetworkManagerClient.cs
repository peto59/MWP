using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Java.Lang;
using Newtonsoft.Json;
using Xamarin.Essentials;
using Thread = System.Threading.Thread;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP.BackEnd.Network
{
    internal static class NetworkManagerClient
    {
        internal static void Client(IPAddress server, int port, List<Song> songsToSend)
        {
#if DEBUG
            MyConsole.WriteLine($"Connecting to: {server}:{port}");
#endif
            TcpClient client = new TcpClient(server.ToString(), port);
            NetworkStream networkStream = client.GetStream();
            RSACryptoServiceProvider encryptor = new RSACryptoServiceProvider();
            RSACryptoServiceProvider decryptor = new RSACryptoServiceProvider();
            Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            if (songsToSend.Count > 0)
            {
                networkStream.WriteCommand(CommandsArr.OnetimeSend);
            }
            networkStream.WriteCommand(CommandsArr.Host, Encoding.UTF8.GetBytes(DeviceInfo.Name));

            ConnectionState connectionState = new ConnectionState(false, songsToSend);

            Thread.Sleep(100);
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
                        goto EndClient;
                    }
                    command = CommandsEnum.None;
                }

#if DEBUG
                MyConsole.WriteLine($"Received command: {command}");
#endif
                #endregion

                #region Writing

                NetworkManagerCommonCommunication.Write(command, ref networkStream,
                    ref encryptor, ref aes, ref connectionState);

                #endregion
                
                switch (command)
                {
                    case CommandsEnum.OnetimeSend:
                        connectionState.gotOneTimeSendFlag = true;
                        break;
                    case CommandsEnum.Host:
                        NetworkManagerCommonCommunication.Host(ref networkStream, ref decryptor, ref encryptor, ref aes, ref connectionState);
                        break;
                    case CommandsEnum.RsaExchange:
                        if (data != null)
                        {
                            NetworkManagerCommonCommunication.RsaExchange(ref networkStream, ref data, ref decryptor, ref encryptor, ref aes, ref connectionState);
                        }
                        break;
                    case CommandsEnum.AesSend:
                        if (data != null)
                        {
                            aes.Key = data;
                            networkStream.WriteCommand(CommandsArr.AesReceived, ref encryptor);
                            connectionState.encryptionState = EncryptionState.Encrypted;
#if DEBUG
                            MyConsole.WriteLine("encrypted");
#endif
                        }
                        break;
                    case CommandsEnum.AesReceived:
                        throw new IllegalStateException("Client doesn't receive aes confirmation");
                    case CommandsEnum.SyncRequest:
                        if (FileManager.IsTrustedSyncTarget(connectionState.remoteHostname))
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
                        break;
                    case CommandsEnum.SyncRejected:
                        connectionState.syncRequestState = SyncRequestState.Rejected;
                        break;
                    case CommandsEnum.SongRequest:
                        break;
                    case CommandsEnum.SongRequestInfoRequest:
                        //TODO: copy from server
                        break;
                    case CommandsEnum.SongRequestInfo:
                        if (data != null)
                        {
                            string json = Encoding.UTF8.GetString(data);
#if DEBUG
                            MyConsole.WriteLine(json);
#endif
                            SongJsonConverter customConverter = new SongJsonConverter(false);
                            List<Song>? recSongs = JsonConvert.DeserializeObject<List<Song>>(json, customConverter);
                            if (recSongs != null)
                            {
#if DEBUG
                                foreach (Song s in recSongs)
                                {
                                    MyConsole.WriteLine(s.ToString());
                                }
#endif
                                bool x = true;
                                if (x) //present some form of user check if they really want to receive files
                                {
                                    //TODO: Stupid! Need to ask before syncing
                                    networkStream.WriteCommand(CommandsArr.SongRequestAccepted, ref encryptor);
                                }
                                else
                                {
                                    networkStream.WriteCommand(CommandsArr.SongRequestRejected, ref encryptor);
                                }
                            }
                            else
                            {
                                networkStream.WriteCommand(CommandsArr.SongRequestRejected, ref encryptor);
                            }
                        }
                        break;
                    case CommandsEnum.SongRequestAccepted:
                        connectionState.songSendRequestState = SongSendRequestState.Accepted;
                        break;
                    case CommandsEnum.SongRequestRejected:
                        connectionState.songSendRequestState = SongSendRequestState.Rejected;
                        break;
                    case CommandsEnum.SongSend:
                        if (length != null && connectionState.isTrustedSyncTarget != null)
                        {
#if DEBUG
                            MyConsole.WriteLine($"file length: {length}");
#endif
                            NetworkManagerCommonCommunication.SongSend(ref networkStream, ref encryptor, (long)length, ref aes, ref connectionState);
                        }
                        break;
                    case CommandsEnum.ArtistImageSend:
                        if (data != null && length != null && connectionState.isTrustedSyncTarget != null)
                        {
                            NetworkManagerCommonCommunication.ArtistImageSend(ref networkStream, ref encryptor, ref aes,
                                (long)length, data, ref connectionState);
                        }
                        break;
                    case CommandsEnum.AlbumImageSend:
                        if (data != null && length != null && connectionState.isTrustedSyncTarget != null)
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
                        if (!connectionState.ending || connectionState.ackCount < 0)//if work to do
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
                        goto EndClient;
                    //break;
                    case CommandsEnum.Wait:
                        Thread.Sleep(25);
                        break;
                    case CommandsEnum.None:
                    default: //unimplemented
#if DEBUG
                        MyConsole.WriteLine($"default: {command}");
#endif
                        Thread.Sleep(100);
                        break;
                }
            }
            EndClient:
            // Close everything.
#if DEBUG
            MyConsole.WriteLine("END");
#endif
            networkStream.Close();
            client.Close();
            encryptor.Dispose();
            decryptor.Dispose();
            aes.Dispose();
            networkStream.Dispose();
            client.Dispose();
            NetworkManagerCommon.Connected.Remove(server);
        }
    }
}