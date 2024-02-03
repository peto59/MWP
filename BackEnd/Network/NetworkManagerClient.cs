using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Java.Lang;
using Newtonsoft.Json;
using Exception = Java.Lang.Exception;
using Thread = System.Threading.Thread;
#if DEBUG
using AndroidX.AppCompat.App;
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

            Notifications? notification = null;
            ConnectionState connectionState = new ConnectionState(false, songsToSend);
            SongJsonConverter customConverter = new SongJsonConverter(false);

            if (songsToSend.Count > 0)
            {
                networkStream.WriteCommand(CommandsArr.OnetimeSend);
                notification = new Notifications(NotificationTypes.OneTimeSend);
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
                            notification?.Stage3(false, connectionState);
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
                        ref encryptor, ref aes, ref connectionState, ref notification);

                    #endregion
                
                    switch (command)
                    {
                        case CommandsEnum.OnetimeSend:
                            connectionState.gotOneTimeSendFlag = true;
                            break;
                        case CommandsEnum.Host:
                            NetworkManagerCommonCommunication.Host(ref networkStream, ref decryptor, ref encryptor, ref aes, ref connectionState, ref notification);
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
                            notification?.Stage2(connectionState);
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
                        case CommandsEnum.SongRequest:
                            networkStream.WriteCommand(CommandsArr.SongRequestInfoRequest, ref encryptor);
                            break;
                        case CommandsEnum.SongRequestInfoRequest:
                            networkStream.WriteCommand(CommandsArr.SongRequestInfo,
                                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(connectionState.songsToSend,
                                    customConverter)),
                                ref encryptor,
                                ref aes);
                            break;
                        case CommandsEnum.SongRequestInfo:
                            if (data != null)
                            {
                                if (connectionState.UserAcceptedState != UserAcceptedState.ConnectionAccepted)
                                {
                                    networkStream.WriteCommand(CommandsArr.SongRequestRejected, ref encryptor);
                                    //TODO: move to function
                                    return;
                                }
                                string json = Encoding.UTF8.GetString(data);
#if DEBUG
                                MyConsole.WriteLine(json);
#endif
                                List<Song>? recSongs = JsonConvert.DeserializeObject<List<Song>>(json, customConverter);
                                if (recSongs is { Count: > 0 })
                                {
#if DEBUG
                                    /*foreach (Song s in recSongs)
                                    {
                                        MyConsole.WriteLine(s.ToString());
                                    }*/
#endif
                                    //TODO: dialogy daj do funkcie aby sme nemali boiler plate
                                    connectionState.oneTimeReceiveCount = recSongs.Count;
                                    StateHandler.OneTimeSendSongs.Add(connectionState.remoteHostname, recSongs);
                                    notification?.Stage1Update(connectionState.remoteHostname, connectionState.oneTimeReceiveCount);
                                    string rh = connectionState.remoteHostname;
                                    int cnt = connectionState.oneTimeReceiveCount;
                                    MainActivity.StateHandler.view?.RunOnUiThread(() =>
                                    {
                                        AlertDialog.Builder builder = new AlertDialog.Builder(MainActivity.StateHandler.view);
                                        builder.SetTitle("New connection");
                                        builder.SetMessage($"{rh} wants to send you {cnt} songs");
                                        builder.SetCancelable(false);

                                        builder.SetPositiveButton("Accept", delegate
                                        {
                                            StateHandler.OneTimeSendStates[rh] = UserAcceptedState.SongsAccepted;
                                        });

                                        builder.SetNegativeButton("Reject", delegate
                                        {
                                            StateHandler.OneTimeSendStates[rh] = UserAcceptedState.Cancelled;
                                        });
                                        StateHandler.OneTimeSendStates[rh] = UserAcceptedState.SongsShowed;
                                    });
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
                            notification?.Stage2(connectionState);
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
                                NetworkManagerCommonCommunication.SongSend(ref networkStream, ref encryptor, (long)length, ref aes, ref connectionState);
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
                            notification?.Stage3(true, connectionState);
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
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                notification?.Stage3(false, connectionState);
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
            StateHandler.OneTimeSendSongs.Remove(connectionState.remoteHostname);
            StateHandler.OneTimeSendStates.Remove(connectionState.remoteHostname);
            NetworkManagerCommon.Connected.Remove(server);
        }
    }
}