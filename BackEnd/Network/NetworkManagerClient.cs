using System;
using System.Collections.Generic;
using System.Linq;
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
        internal static async void Client(IPAddress server, int port, List<Song> songsToSend)
        {
#if DEBUG
            MyConsole.WriteLine($"Connecting to: {server}:{port}");
#endif
            TcpClient client = new TcpClient(server.ToString(), port);
            NetworkStream networkStream = client.GetStream();
            EncryptionState encryptionState = EncryptionState.None;
            SyncRequestState syncRequestState = SyncRequestState.None;
            SongSendRequestState songSendRequestState = SongSendRequestState.None;
            bool ending = false;
            string remoteHostname = string.Empty;
            bool? isTrustedSyncTarget = null;
            List<Song> syncSongs = new List<Song>();
            int ackCount = 0;

            RSACryptoServiceProvider encryptor = new RSACryptoServiceProvider();
            RSACryptoServiceProvider decryptor = new RSACryptoServiceProvider();
            Aes aes = Aes.Create();
            aes.KeySize = 256;
            
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
                    switch (encryptionState)
                    {
                        case EncryptionState.None:
                            command = networkStream.ReadCommand();
                            if (Commands.IsEncryptedOnlyCommand(command))
                                throw new IllegalStateException("Received encrypted only command on unencrypted channel");
                            break;
                        case EncryptionState.RsaExchange:
                            (command, data) = networkStream.ReadCommandCombined();
                            if (command == CommandsEnum.RsaExchange)
                            {
                                if (data == null)
                                {
                                    throw new IllegalStateException("Received empty public key");
                                }
                            }
                            else if (command != CommandsEnum.None)
                            {
                                throw new IllegalStateException($"wrong order to establish cypher, required step: {CommandsEnum.RsaExchange}");
                            }
                            break;
                        case EncryptionState.AesSend:
                            (command, data, _, _) = networkStream.ReadCommand(ref decryptor);
                            if (command == CommandsEnum.AesSend)
                            {
                                if (data == null)
                                {
                                    throw new IllegalStateException("Received empty aes key");
                                }
                            }
                            else if (command != CommandsEnum.None)
                            {
                                throw new IllegalStateException($"wrong order to establish cypher, required step: {CommandsEnum.AesSend}");
                            }
                            break;
                        case EncryptionState.AesReceived:
                            throw new IllegalStateException("Client doesn't receive aes confirmation");
                        case EncryptionState.Encrypted:
                            byte[]? iv;
                            (command, data, iv, length) = networkStream.ReadCommand(ref decryptor);
                            if (Commands.IsLong(command))
                            {
                                if (iv == null || length == null)
                                {
                                    throw new IllegalStateException("Received empty IV or length on long data");
                                }
                                aes.IV = iv;
                                if (!Commands.IsFileCommand(command))
                                {
                                    data = networkStream.ReadEncrypted(ref aes, (long)length);
                                }
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
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

                if (ending && command == CommandsEnum.None)
                {
                    if (encryptionState == EncryptionState.Encrypted)
                    {
                        networkStream.WriteCommand(CommandsArr.End, ref encryptor);
                    }
                    else
                    {
                        networkStream.WriteCommand(CommandsArr.End);
                    }
                }
                else if (encryptionState == EncryptionState.Encrypted)
                {
                    switch (songsToSend.Count)
                    {
                        case > 0 when songSendRequestState == SongSendRequestState.None:
                            networkStream.WriteCommand(CommandsArr.SongRequest, ref encryptor);
                            songSendRequestState = SongSendRequestState.Sent;
                            break;
                        case > 0 when songSendRequestState == SongSendRequestState.Accepted:
                            if (ackCount >= 0)
                            {
                                networkStream.WriteFile(songsToSend[0].Path, ref encryptor, ref aes);
                                songsToSend.RemoveAt(0);
                                ackCount--;
                            }
                            break;
                        default:
                        {
                            if(isTrustedSyncTarget == null)
                            {
                                isTrustedSyncTarget = FileManager.IsTrustedSyncTarget(remoteHostname);
#if DEBUG
                                MyConsole.WriteLine($"Is trusted sync target? {isTrustedSyncTarget}");                                                                    
#endif
                                if ((bool)isTrustedSyncTarget)
                                {
                                    syncSongs = FileManager.GetTrustedSyncTargetSongs(remoteHostname);
                                }
                            }
                            switch (syncSongs.Count)
                            {
                                case > 0 when syncRequestState == SyncRequestState.None:
                                    networkStream.WriteCommand(CommandsArr.SyncRequest, ref encryptor);
                                    syncRequestState = SyncRequestState.Sent;
                                    break;
                                case > 0 when syncRequestState == SyncRequestState.Accepted:
#if DEBUG
                                    MyConsole.WriteLine($"Ack count: {ackCount}");
#endif
                                    if (ackCount >= 0)
                                    {
#if DEBUG
                                        MyConsole.WriteLine(syncSongs[0].ToString());
#endif
                                        networkStream.WriteFile(syncSongs[0].Path, ref encryptor, ref aes);
                                        syncSongs.RemoveAt(0);
                                        ackCount--;
                                        //TODO: remove from FileManager.GetTrustedSyncTargetSongs
                                    }
                                    break;
                                default:
                                    if (!ending)
                                    {
                                        ending =
                                        (songsToSend.Count == 0 || 
                                         (songsToSend.Count > 0 && songSendRequestState == SongSendRequestState.Rejected))
                                        &&
                                        (syncSongs.Count == 0 || 
                                         (syncSongs.Count > 0 && syncRequestState == SyncRequestState.Rejected))
                                        && ackCount >= 0;
#if DEBUG
                                        MyConsole.WriteLine($"isEnding? {ending}");                               
#endif
                                    }
                                    break;
                            }
                            break;
                        }
                    }
                }

                #endregion
                
                switch (command)
                {
                    case CommandsEnum.Host: //host
                        remoteHostname = Encoding.UTF8.GetString(networkStream.ReadData());
#if DEBUG
                        MyConsole.WriteLine($"hostname {remoteHostname}");
#endif
                        //SecureStorage.RemoveAll();
                        
                        (string? storedPrivKey, string? storedPubKey) = await NetworkManagerCommon.LoadKeys(remoteHostname);
                        
                        if (storedPrivKey == null || storedPubKey == null)
                        {
#if DEBUG
                            MyConsole.WriteLine("generating keys");
#endif
                            (string myPubKeyString, RSAParameters privKey) = NetworkManagerCommon.CreateKeyPair();
                            decryptor.ImportParameters(privKey);
                            networkStream.WriteCommand(CommandsArr.RsaExchange, Encoding.UTF8.GetBytes(myPubKeyString));
                            encryptionState = EncryptionState.RsaExchange;
                        }
                        else
                        {
                            encryptor.FromXmlString(storedPubKey);
                            decryptor.FromXmlString(storedPrivKey);
                            encryptionState = EncryptionState.AesSend;
                        }
                        break;
                    case CommandsEnum.RsaExchange:
                        if (data != null)
                        {
                            string remotePubKeyString = Encoding.UTF8.GetString(data);
                            encryptor.FromXmlString(remotePubKeyString);

                            //store private and public key for later use
                            _ = SecureStorage.SetAsync($"{remoteHostname}_privkey", decryptor.ToXmlString(true));
                            _ = SecureStorage.SetAsync($"{remoteHostname}_pubkey", remotePubKeyString);
                            encryptionState = EncryptionState.AesSend;
                        }
                        break;
                    case CommandsEnum.AesSend:
                        aes.Key = data;
                        networkStream.WriteCommand(CommandsArr.AesReceived, ref encryptor);
                        encryptionState = EncryptionState.Encrypted;
#if DEBUG
                        MyConsole.WriteLine("encrypted");
#endif
                        break;
                    case CommandsEnum.AesReceived:
                        throw new IllegalStateException("Client doesn't receive aes confirmation");
                    case CommandsEnum.SyncRequest: //sync
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
                    case CommandsEnum.SongSend: //file
                        if (length != null)
                        {
                            //TODO: update
                            string path = FileManager.GetAvailableTempFile("receive", "mp3");
                            networkStream.ReadFile(path, (long)length, ref aes);
                            (List<string> missingArtists, (string missingAlbum, string albumArtistPath)) = FileManager.AddSong(path, true);
                            foreach (string name in missingArtists)
                            {
                                networkStream.WriteCommand(CommandsArr.ArtistImageRequest, Encoding.UTF8.GetBytes(name), ref encryptor);
                            }
                            if (!string.IsNullOrEmpty(missingAlbum))
                            {
                                networkStream.WriteCommand(CommandsArr.AlbumImageRequest, Encoding.UTF8.GetBytes(missingAlbum), ref encryptor);
                            }
                        }
                        break;
                    case CommandsEnum.ArtistImageSend:
                        break;
                    case CommandsEnum.AlbumImageSend:
                        break;
                    case CommandsEnum.ArtistImageRequest:
                        if (data != null)
                        {
                            string? artistName = Encoding.UTF8.GetString(data);
                            List<Artist> artists = MainActivity.stateHandler.Artists.Search(artistName);
                            foreach (Artist artist in artists.Where(artist => artist.ImgPath != "Default"))
                            {
                                networkStream.WriteFile(artist.ImgPath, ref encryptor, ref aes, CommandsArr.ArtistImageSend, Encoding.UTF8.GetBytes(artists[0].Title));
                                ackCount--;
                                break;
                            }
                        }
                        break;
                    case CommandsEnum.AlbumImageRequest:
                        if (data != null)
                        {
                            string? albumName = Encoding.UTF8.GetString(data);
                            List<Album> albums = MainActivity.stateHandler.Albums.Search(albumName);
                            foreach (Album album in albums.Where(album => album.ImgPath != "Default"))
                            {
                                networkStream.WriteFile(album.ImgPath, ref encryptor, ref aes, CommandsArr.AlbumImageSend, Encoding.UTF8.GetBytes(albums[0].Title));
                                ackCount--;
                                break;
                            }
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