using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Java.Lang;
using Xamarin.Essentials;
using Exception = System.Exception;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json;
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP.BackEnd.Network
{
    internal static class NetworkManagerCommonCommunication
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (CommandsEnum command, byte[]? data, long? length) Read(ref NetworkStream networkStream, ref RSACryptoServiceProvider decryptor, ref Aes aes, ref ConnectionState connectionState)
        {
            CommandsEnum command;
            byte[]? data = null;
            long? length = null;
            switch (connectionState.encryptionState)
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
                    else if (command != CommandsEnum.None && command != CommandsEnum.Wait)
                    {
                        throw new IllegalStateException($"wrong order to establish cypher, required step: {CommandsEnum.RsaExchange}");
                    }
                    break;
                case EncryptionState.AesSend:
                    if (connectionState.IsServer)
                    {
                        throw new InvalidOperationException("Server doesn't receive aes request");
                    }
                    (command, data, _, _) = networkStream.ReadCommand(ref decryptor);
                    if (command == CommandsEnum.AesSend)
                    {
                        if (data == null)
                        {
                            throw new IllegalStateException("Received empty aes key");
                        }
                    }
                    else if (command != CommandsEnum.None && command != CommandsEnum.Wait)
                    {
                        throw new IllegalStateException(
                            $"wrong order to establish cypher, required step: {CommandsEnum.AesSend}");
                    }
                    break;
                case EncryptionState.AesReceived:
                    if (connectionState.IsServer)
                    {
                        (command, _, _, _) = networkStream.ReadCommand(ref decryptor);
                        if (command != CommandsEnum.AesReceived && command != CommandsEnum.None && command != CommandsEnum.Wait)
                        {
                            throw new InvalidOperationException($"wrong order to establish cypher, required step: {CommandsEnum.AesReceived}");
                        }
                    }
                    else
                    {
                        throw new IllegalStateException("Client doesn't receive aes confirmation");
                    }
                    break;
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
            return (command, data, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Write(CommandsEnum command, ref NetworkStream networkStream, ref RSACryptoServiceProvider encryptor, ref Aes aes, ref ConnectionState connectionState, ref Notifications? notification)
        {
            if (connectionState.encryptionState != EncryptionState.Encrypted) return;
            
            if (connectionState is not ({ sentConnectionAcceptedCommand: false, UserAcceptedState: UserAcceptedState.Cancelled } or { gotSongInfoFlag: true, UserAcceptedState: UserAcceptedState.Cancelled }))
            {
                if (connectionState.Ending && command == CommandsEnum.None)
                {
#if DEBUG
                    MyConsole.WriteLine("Sending end!");
#endif
                    if (connectionState.encryptionState == EncryptionState.Encrypted)
                    {
                        networkStream.WriteCommand(CommandsArr.End, ref encryptor);
                    }
                    else
                    {
                        networkStream.WriteCommand(CommandsArr.End);
                    }
                    return;
                }
            }
            
            if (connectionState.ConnectionType == ConnectionType.OneTimeReceive)
            {
#if DEBUG
                MyConsole.WriteLine($"user state {connectionState.UserAcceptedState}");
#endif
                if (connectionState.UserAcceptedState is UserAcceptedState.Showed or UserAcceptedState.SongsShowed)
                {
                    networkStream.WriteCommand(CommandsArr.Wait, ref encryptor);
                }

                if (connectionState is { sentConnectionAcceptedCommand: false, UserAcceptedState: UserAcceptedState.ConnectionAccepted })
                {
                    connectionState.sentConnectionAcceptedCommand = true;
                    if (connectionState.encryptionState == EncryptionState.Encrypted)
                    {
                        networkStream.WriteCommand(CommandsArr.ConnectionAccepted, ref encryptor);
                    }
                    else
                    {
                        networkStream.WriteCommand(CommandsArr.ConnectionAccepted);
                    }
                    
                }
#if DEBUG
                if (connectionState.UserAcceptedState == UserAcceptedState.Cancelled)
                {
                    MyConsole.WriteLine("User rejected connection");
                    MyConsole.WriteLine($"sentConnectionAcceptedCommand {connectionState.sentConnectionAcceptedCommand}");
                }
#endif
                if (connectionState is { sentConnectionAcceptedCommand: false, UserAcceptedState: UserAcceptedState.Cancelled })
                {
                    connectionState.sentConnectionAcceptedCommand = true;
                    if (connectionState.encryptionState == EncryptionState.Encrypted)
                    {
                        networkStream.WriteCommand(CommandsArr.ConnectionRejected, ref encryptor);
                    }
                    else
                    {
                        networkStream.WriteCommand(CommandsArr.ConnectionRejected);
                    }
                }

                if (connectionState is { gotSongSendRequestCommand: true, UserAcceptedState: UserAcceptedState.ConnectionAccepted })
                {
                    connectionState.gotSongSendRequestCommand = false;
                    networkStream.WriteCommand(CommandsArr.SongRequestInfoRequest, ref encryptor);
                }
                
                if (connectionState is { gotSongInfoFlag: true, UserAcceptedState: UserAcceptedState.SongsAccepted })
                {
                    connectionState.gotSongInfoFlag = false;
                    networkStream.WriteCommand(CommandsArr.SongRequestAccepted, ref encryptor);
                    notification?.Stage2(connectionState);
                }

                if (connectionState is { gotSongInfoFlag: true, UserAcceptedState: UserAcceptedState.Cancelled })
                {
                    connectionState.gotSongInfoFlag = false;
                    networkStream.WriteCommand(CommandsArr.SongRequestRejected, ref encryptor);
                }
            }
            else if (connectionState.ConnectionType == ConnectionType.OneTimeSend)
            {
                if (connectionState.songsToSend.Count <= 0) return;
                if (!connectionState.connectionWasAccepted) return;
                
                if (connectionState.songSendRequestState == SongSendRequestState.None)
                {
                    networkStream.WriteCommand(CommandsArr.SongSendRequest, ref encryptor);
                    connectionState.songSendRequestState = SongSendRequestState.Sent;
                }

                if (!connectionState.CanSendFiles) return;

                if (connectionState.ackCount < 0) return;
                
                networkStream.WriteFile(connectionState.songsToSend[0].Path, ref encryptor, ref aes);
                connectionState.songsToSend.RemoveAt(0);
                connectionState.ackCount--;
                connectionState.oneTimeSentCount++;
                notification?.Stage2Update(connectionState);
            }
            else if (connectionState.ConnectionType == ConnectionType.Sync)
            {
                connectionState.isTrustedSyncTarget ??= FileManager.IsTrustedSyncTarget(connectionState.remoteHostname);
                if ((bool)!connectionState.isTrustedSyncTarget) return;
                if (connectionState is { SyncSendCountLeft: 0, fetchedSyncSongs: false })
                {
                    connectionState.SyncSongs = FileManager.GetTrustedSyncTargetSongs(connectionState.remoteHostname);
                    connectionState.fetchedSyncSongs = true;
                }
                if (connectionState.SyncSendCountLeft == 0) return;
                if (connectionState.syncRequestState == SyncRequestState.None)
                {
                    networkStream.WriteCommand(CommandsArr.SyncRequest, ref encryptor);
                    connectionState.syncRequestState = SyncRequestState.Sent;
                }

                if (!connectionState.CanSendFiles || connectionState.ackCount < 0 || connectionState.syncRequestState != SyncRequestState.Accepted) return;
                if (File.Exists(connectionState.SyncSongs[0].Path))
                {
#if DEBUG
                    MyConsole.WriteLine($"Sending {connectionState.SyncSongs[0]}");
#endif
                    networkStream.WriteFile(connectionState.SyncSongs[0].Path, ref encryptor, ref aes);
                    connectionState.ackCount--;
                    connectionState.syncSentCount++;
                }
                connectionState.SyncSongs.RemoveAt(0);
                notification?.Stage2Update(connectionState);
                FileManager.SetTrustedSyncTargetSongs(connectionState.remoteHostname, connectionState.SyncSongs);
#if DEBUG
                MyConsole.WriteLine("Finished sending song");
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Host(ref NetworkStream networkStream,
            ref RSACryptoServiceProvider decryptor, ref RSACryptoServiceProvider encryptor, ref Aes aes, ref ConnectionState connectionState, ref Notifications? notification)
        {
            connectionState.remoteHostname = Encoding.UTF8.GetString(networkStream.ReadData());
#if DEBUG
            MyConsole.WriteLine($"hostname {connectionState.remoteHostname}");
#endif
            connectionState.isTrustedSyncTarget = FileManager.IsTrustedSyncTarget(connectionState.remoteHostname);
            if (connectionState.isTrustedSyncTarget ?? false)
            {
                notification ??= new Notifications(NotificationTypes.Sync);
                notification.Stage1(connectionState.remoteHostname);
            }

            if (connectionState.sentOnetimeSendFlag)
            {
                notification ??= new Notifications(NotificationTypes.OneTimeSend);
                notification.Stage1(connectionState.remoteHostname);
            }
            if (connectionState.gotOneTimeSendFlag)
            {
                notification = new Notifications(NotificationTypes.OneTimeReceive);
                notification.Stage1(connectionState.remoteHostname);
                string rh = connectionState.remoteHostname;
                /*StateHandler.OneTimeSendStates[remoteHostname] = UserAcceptedState.ConnectionAccepted;
                if (connectionState)
                {
                    networkStream.WriteCommand(CommandsArr.SongRequestInfoRequest, ref encryptor);
                }*/
            
            
                /*
                 StateHandler.OneTimeSendStates[remoteHostname] = UserAcceptedState.Cancelled;

                    if (connectionState)
                    {
                        networkStream.WriteCommand(CommandsArr.SongRequestInfoRequest, ref encryptor);
                    }
                */
                MainActivity.StateHandler.view?.RunOnUiThread(() =>
                {
                    MainActivity.NewConnectionDialog(rh, MainActivity.StateHandler.view);
                });
                StateHandler.OneTimeSendStates.Add(connectionState.remoteHostname, UserAcceptedState.Showed);
            }
                        

            string? storedPrivKey = null;
            string? storedPubKey = null;

            if (connectionState.isTrustedSyncTarget ?? false)
            {
                Task<(string? storedPrivKey, string? storedPubKey)> task = NetworkManagerCommon.LoadKeys(connectionState.remoteHostname);
                (storedPrivKey, storedPubKey) = task.GetAwaiter().GetResult();
            }
                        
            if (connectionState.gotOneTimeSendFlag || storedPrivKey == null || storedPubKey == null)
            {
#if DEBUG
                MyConsole.WriteLine("generating keys");
#endif
                (string myPubKeyString, RSAParameters privKey) = NetworkManagerCommon.CreateKeyPair();
                decryptor.ImportParameters(privKey);
                networkStream.WriteCommand(CommandsArr.RsaExchange, Encoding.UTF8.GetBytes(myPubKeyString));
                connectionState.encryptionState = EncryptionState.RsaExchange;
            }
            else
            {
                encryptor.FromXmlString(storedPubKey);
                decryptor.FromXmlString(storedPrivKey);
                if (connectionState.IsServer)
                {
                    aes.GenerateKey();
                    networkStream.WriteCommand(CommandsArr.AesSend, aes.Key, ref encryptor);
                    connectionState.encryptionState = EncryptionState.AesReceived;
                }
                else
                {
                    connectionState.encryptionState = EncryptionState.AesSend;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RsaExchange(ref NetworkStream networkStream, ref byte[] data, ref RSACryptoServiceProvider decryptor, ref RSACryptoServiceProvider encryptor, ref Aes aes, ref ConnectionState connectionState)
        {
            string remotePubKeyString = Encoding.UTF8.GetString(data);
            encryptor.FromXmlString(remotePubKeyString);

            if (connectionState.isTrustedSyncTarget ?? false)
            {
                //store private and public key for later use
                _ = SecureStorage.SetAsync($"{connectionState.remoteHostname}_privkey", decryptor.ToXmlString(true));
                _ = SecureStorage.SetAsync($"{connectionState.remoteHostname}_pubkey", remotePubKeyString);
            }
            if (connectionState.IsServer)
            {
#if DEBUG
                MyConsole.WriteLine("Sending AES");
#endif
                networkStream.WriteCommand(CommandsArr.AesSend, aes.Key, ref encryptor);
                connectionState.encryptionState = EncryptionState.AesReceived;
            }
            else
            {
                connectionState.encryptionState = EncryptionState.AesSend;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SongRequestInfo(ref NetworkStream networkStream, ref ConnectionState connectionState, byte[] data, ref RSACryptoServiceProvider encryptor, ref Notifications? notification)
        {
            if (connectionState.UserAcceptedState != UserAcceptedState.ConnectionAccepted)
            {
                networkStream.WriteCommand(CommandsArr.SongRequestRejected, ref encryptor);
                return;
            }
            string json = Encoding.UTF8.GetString(data);
#if DEBUG
            MyConsole.WriteLine(json);
#endif
            SongJsonConverter customConverter = new SongJsonConverter(false);
            List<Song>? recSongs = JsonConvert.DeserializeObject<List<Song>>(json, customConverter);
            if (recSongs is { Count: > 0 })
            {
#if DEBUG
                /*foreach (Song s in recSongs)
                {
                    MyConsole.WriteLine(s.ToString());
                }*/
#endif
                connectionState.gotSongInfoFlag = true;
                connectionState.oneTimeReceiveCount = recSongs.Count;
                StateHandler.OneTimeReceiveSongs.Add(connectionState.remoteHostname, recSongs);
                notification?.Stage1Update(connectionState.remoteHostname, connectionState.oneTimeReceiveCount);
                string rh = connectionState.remoteHostname;
                MainActivity.StateHandler.view?.RunOnUiThread(() =>
                {
                    MainActivity.SongsSendDialog(rh, MainActivity.StateHandler.view);
                });
                StateHandler.OneTimeSendStates[connectionState.remoteHostname] = UserAcceptedState.SongsShowed;
            }
            else
            {
                networkStream.WriteCommand(CommandsArr.SongRequestRejected, ref encryptor);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SongSend(ref NetworkStream networkStream, ref RSACryptoServiceProvider encryptor, long length, ref Aes aes, ref ConnectionState connectionState, ref Notifications? notification)
        {
            switch (connectionState.ConnectionType)
            {
                case ConnectionType.OneTimeReceive:
                    connectionState.oneTimeReceivedCount++;
                    break;
                case ConnectionType.Sync:
                    connectionState.syncReceivedCount++;
                    break;
                case ConnectionType.None:
                    break;
                case ConnectionType.OneTimeSend:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!connectionState.CanReceiveFiles)
            {
#if DEBUG
                MyConsole.WriteLine("Trashing file");
#endif
                networkStream.ReadFileTrash(length, ref encryptor);
                return;
            }
            try
            {
                string songPath = FileManager.GetAvailableTempFile("receive", "mp3");
                networkStream.ReadFile(songPath, length, ref aes, ref encryptor);
#if DEBUG
                MyConsole.WriteLine("Read file to temp");
#endif
                (List<string> missingArtists, (string missingAlbum, string albumArtistPath)) =
                    FileManager.AddSong(songPath, true, true, connectionState.remoteHostname);
#if DEBUG
                MyConsole.WriteLine("Added file");
#endif
                foreach (string name in missingArtists)
                {
#if DEBUG
                    MyConsole.WriteLine($"Missing artist: {name}");
#endif
                    networkStream.WriteCommand(CommandsArr.ArtistImageRequest,
                        Encoding.UTF8.GetBytes(name), ref encryptor);
                    connectionState.artistImageRequests.Add(name);
                }

                if (!string.IsNullOrEmpty(missingAlbum))
                {
#if DEBUG
                    MyConsole.WriteLine($"Missing album: {missingAlbum}");
#endif
                    networkStream.WriteCommand(CommandsArr.AlbumImageRequest,
                        Encoding.UTF8.GetBytes(missingAlbum), ref encryptor);
                    connectionState.albumArtistPair.TryAdd(missingAlbum, albumArtistPath);
                    connectionState.albumImageRequests.Add(missingAlbum);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                //ignored
            }
            notification?.Stage2Update(connectionState);
            networkStream.WriteCommand(CommandsArr.Ack, ref encryptor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ArtistImageSend(ref NetworkStream networkStream, ref RSACryptoServiceProvider encryptor, ref Aes aes, long length, byte[] data, ref ConnectionState connectionState)
        {
            if (!connectionState.CanReceiveFiles)
            {
#if DEBUG
                MyConsole.WriteLine("Trashing file");
#endif
                networkStream.ReadFileTrash(length, ref encryptor);
                return;
            }
            try
            {
                string artist = Encoding.UTF8.GetString(data);
                connectionState.artistImageRequests.Remove(artist);
                string artistAlias = FileManager.GetAlias(artist);
                connectionState.artistImageRequests.Remove(artistAlias);
                string artistPath = FileManager.Sanitize(artist);
                string imagePath = FileManager.GetAvailableTempFile("networkImage", "image");
                networkStream.ReadFile(imagePath, length, ref aes, ref encryptor);
                string imageExtension = FileManager.GetImageFormat(imagePath);
                string artistImagePath =
                    $"{FileManager.MusicFolder}/{artistPath}/cover.{imageExtension.TrimStart('.')}";
                File.Move(imagePath, artistImagePath);
                List<Artist> artists = MainActivity.StateHandler.Artists.Search(artistAlias);
                if (artists.Count > 1)
                {
                    int artistIndex = MainActivity.StateHandler.Artists.IndexOf(artists[0]);
                    MainActivity.StateHandler.Artists[artistIndex] =
                        new Artist(artists[0], artistImagePath);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                //ignored
            }
            networkStream.WriteCommand(CommandsArr.Ack, ref encryptor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AlbumImageSend(ref NetworkStream networkStream, ref RSACryptoServiceProvider encryptor, ref Aes aes, long length, byte[] data, ref ConnectionState connectionState)
        {
            if (!connectionState.CanReceiveFiles)
            {
#if DEBUG
                MyConsole.WriteLine("Trashing file");
#endif
                networkStream.ReadFileTrash(length, ref encryptor);
                return;
            }
            try
            {
                string album = Encoding.UTF8.GetString(data);
                connectionState.albumImageRequests.Remove(album);
                string albumPath = FileManager.Sanitize(album);
                string imagePath = FileManager.GetAvailableTempFile("networkImage", "image");
                networkStream.ReadFile(imagePath, length, ref aes, ref encryptor);
                string imageExtension = FileManager.GetImageFormat(imagePath);
                string albumImagePath =
                    $"{FileManager.MusicFolder}/{connectionState.albumArtistPair[album]}/{albumPath}/cover.{imageExtension.TrimStart('.')}";
                File.Move(imagePath, albumImagePath);
                List<Album> albums = MainActivity.StateHandler.Albums.Search(album);
                if (albums.Count > 1)
                {
                    int albumIndex = MainActivity.StateHandler.Albums.IndexOf(albums[0]);
                    MainActivity.StateHandler.Albums[albumIndex] = new Album(albums[0], albumImagePath);
                }
                connectionState.albumArtistPair.Remove(album);
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                //ignored
            }
            networkStream.WriteCommand(CommandsArr.Ack, ref encryptor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ArtistImageRequest(ref NetworkStream networkStream, ref RSACryptoServiceProvider encryptor, ref Aes aes, byte[] data, ref ConnectionState connectionState)
        {
            if (!connectionState.CanSendFiles)
            {
                networkStream.WriteCommand(CommandsArr.ArtistImageNotFound, data, ref encryptor);
            }
            string artistName = Encoding.UTF8.GetString(data);
            List<Artist> artists = MainActivity.StateHandler.Artists.Search(artistName);
#if DEBUG
            MyConsole.WriteLine($"Got request for artist {artistName}");
#endif
            bool processed = false;
            foreach (Artist artist in artists.Where(artist => artist.ImgPath != "Default"))
            {
                networkStream.WriteFile(artist.ImgPath, ref encryptor, ref aes, CommandsArr.ArtistImageSend, data);
                connectionState.ackCount--;
                processed = true;
#if DEBUG
                MyConsole.WriteLine($"Sending image for artist {artistName}");
#endif
                break;
            }
            if (!processed)
            {
                networkStream.WriteCommand(CommandsArr.ArtistImageNotFound, data, ref encryptor);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AlbumImageRequest(ref NetworkStream networkStream, ref RSACryptoServiceProvider encryptor,
            ref Aes aes, byte[] data, ref ConnectionState connectionState)
        {
            if (!connectionState.CanSendFiles)
            {
                networkStream.WriteCommand(CommandsArr.ArtistImageNotFound, data, ref encryptor);
            }
            string albumName = Encoding.UTF8.GetString(data);
            List<Album> albums = MainActivity.StateHandler.Albums.Search(albumName);
#if DEBUG
            MyConsole.WriteLine($"Got request for album {albumName}");
#endif
            bool processed = false;
            foreach (Album album in albums.Where(album => album.ImgPath != "Default"))
            {
                networkStream.WriteFile(album.ImgPath, ref encryptor, ref aes, CommandsArr.AlbumImageSend, data);
                connectionState.ackCount--;
                processed = true;
#if DEBUG
                MyConsole.WriteLine($"Sending image for album {albumName}");
#endif
                break;
            }
            if (!processed)
            {
                networkStream.WriteCommand(CommandsArr.AlbumImageNotFound, data, ref encryptor);
            }
        }
    }
}