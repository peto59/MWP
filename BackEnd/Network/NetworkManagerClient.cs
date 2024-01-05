using System;
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
            EncryptionState encryptionState = EncryptionState.None;
            SyncRequestState syncRequestState = SyncRequestState.None;
            SongSendRequestState songSendRequestState = SongSendRequestState.None;
            Dictionary<string, string> albumArtistPair = new Dictionary<string, string>();
            bool ending = false;
            string remoteHostname = string.Empty;
            bool? isTrustedSyncTarget = null;
            List<Song> syncSongs = new List<Song>();
            int ackCount = 0;
            RSACryptoServiceProvider encryptor = new RSACryptoServiceProvider();
            RSACryptoServiceProvider decryptor = new RSACryptoServiceProvider();
            Aes aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
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
                    (command, data, length) = NetworkManagerCommonCommunication.Read(encryptionState, ref networkStream, ref decryptor, ref aes, false);
                }
                else
                {
                    command = CommandsEnum.None;
                }

#if DEBUG
                MyConsole.WriteLine($"Received command: {command}");
#endif
                #endregion

                #region Writing

                NetworkManagerCommonCommunication.Write(ref ending, command, encryptionState, ref networkStream,
                    ref encryptor, ref aes, ref songsToSend, ref syncSongs, ref syncRequestState,
                    ref songSendRequestState, ref isTrustedSyncTarget, ref ackCount, remoteHostname);

                #endregion
                
                switch (command)
                {
                    case CommandsEnum.Host: //host
                        NetworkManagerCommonCommunication.Host(ref remoteHostname, ref networkStream, ref decryptor, ref encryptor, ref aes, ref encryptionState, false);
                        break;
                    case CommandsEnum.RsaExchange:
                        if (data != null)
                        {
                            NetworkManagerCommonCommunication.RsaExchange(ref networkStream, ref data, remoteHostname,
                                ref encryptionState, ref decryptor, ref encryptor, ref aes, false);
                        }
                        break;
                    case CommandsEnum.AesSend:
                        if (data != null)
                        {
                            aes.Key = data;
                            networkStream.WriteCommand(CommandsArr.AesReceived, ref encryptor);
                            encryptionState = EncryptionState.Encrypted;
#if DEBUG
                            MyConsole.WriteLine("encrypted");
#endif
                        }
                        break;
                    case CommandsEnum.AesReceived:
                        throw new IllegalStateException("Client doesn't receive aes confirmation");
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
                                ref albumArtistPair, (bool)isTrustedSyncTarget);
                        }
                        break;
                    case CommandsEnum.ArtistImageSend:
                        if (data != null && length != null && isTrustedSyncTarget != null)
                        {
                            NetworkManagerCommonCommunication.ArtistImageSend(ref networkStream, ref encryptor, ref aes,
                                (long)length, data, (bool)isTrustedSyncTarget);
                        }
                        break;
                    case CommandsEnum.AlbumImageSend:
                        if (data != null && length != null && isTrustedSyncTarget != null)
                        {
                            NetworkManagerCommonCommunication.AlbumImageSend(ref networkStream, ref encryptor, ref aes,
                                (long)length, data, ref albumArtistPair, (bool)isTrustedSyncTarget);
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
                        networkStream.Close();
                        client.Close();
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
            encryptor.Dispose();
            decryptor.Dispose();
            aes.Dispose();
            networkStream.Dispose();
            client.Dispose();
            NetworkManagerCommon.Connected.Remove(server);
        }
    }
}