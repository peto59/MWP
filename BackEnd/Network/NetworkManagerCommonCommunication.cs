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
#if DEBUG
using MWP.Helpers;
#endif

namespace MWP.BackEnd.Network
{
    internal static class NetworkManagerCommonCommunication
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (CommandsEnum command, byte[]? data, long? length) Read(EncryptionState encryptionState, ref NetworkStream networkStream, ref RSACryptoServiceProvider decryptor, ref Aes aes, bool isServer)
        {
            CommandsEnum command;
            byte[]? data = null;
            long? length = null;
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
                    if (isServer)
                    {
                        throw new InvalidOperationException("Server doesn't receive aes request");
                    }
                    else
                    {
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
                            throw new IllegalStateException(
                                $"wrong order to establish cypher, required step: {CommandsEnum.AesSend}");
                        }
                    }
                    break;
                case EncryptionState.AesReceived:
                    if (isServer)
                    {
                        (command, _, _, _) = networkStream.ReadCommand(ref decryptor);
                        if (command != CommandsEnum.AesReceived && command != CommandsEnum.None)
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
        internal static void Write(ref bool ending, CommandsEnum command, EncryptionState encryptionState, ref NetworkStream networkStream, ref RSACryptoServiceProvider encryptor, ref Aes aes, ref List<Song> songsToSend, ref List<Song> syncSongs, ref SyncRequestState syncRequestState, ref SongSendRequestState songSendRequestState, ref bool? isTrustedSyncTarget, ref int ackCount, string remoteHostname)
        {
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
                                    ackCount--;
                                    FileManager.DeleteTrustedSyncTargetSongs(remoteHostname, syncSongs[0]);
                                    syncSongs.RemoveAt(0);
                                }
                                break;
                            default:
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
                                break;
                        }
                        break;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Host(ref string remoteHostname, ref NetworkStream networkStream, ref RSACryptoServiceProvider decryptor, ref RSACryptoServiceProvider encryptor, ref Aes aes, ref EncryptionState encryptionState, bool isServer)
        {
            remoteHostname = Encoding.UTF8.GetString(networkStream.ReadData());
#if DEBUG
            MyConsole.WriteLine($"hostname {remoteHostname}");
#endif
                        
            Task<(string? storedPrivKey, string? storedPubKey)> task = NetworkManagerCommon.LoadKeys(remoteHostname);
            while (!task.IsCompleted)
            {
                Thread.Sleep(10);
            }

            (string? storedPrivKey, string? storedPubKey) = task.Result;
                        
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
            else if (isServer)
            {
                encryptor.FromXmlString(storedPubKey);
                decryptor.FromXmlString(storedPrivKey);
                networkStream.WriteCommand(CommandsArr.AesSend, aes.Key, ref encryptor);
                encryptionState = EncryptionState.AesReceived;
            }
            else
            {
                encryptor.FromXmlString(storedPubKey);
                decryptor.FromXmlString(storedPrivKey);
                encryptionState = EncryptionState.AesSend;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RsaExchange(ref NetworkStream networkStream, ref byte[] data, string remoteHostname, ref EncryptionState encryptionState, ref RSACryptoServiceProvider decryptor, ref RSACryptoServiceProvider encryptor, ref Aes aes, bool isServer)
        {
            string remotePubKeyString = Encoding.UTF8.GetString(data);
            encryptor.FromXmlString(remotePubKeyString);

            //store private and public key for later use
            _ = SecureStorage.SetAsync($"{remoteHostname}_privkey", decryptor.ToXmlString(true));
            _ = SecureStorage.SetAsync($"{remoteHostname}_pubkey", remotePubKeyString);
            if (isServer)
            {
                networkStream.WriteCommand(CommandsArr.AesSend, aes.Key, ref encryptor);
                encryptionState = EncryptionState.AesReceived;
            }
            else
            {
                encryptionState = EncryptionState.AesSend;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SongSend(ref NetworkStream networkStream, ref RSACryptoServiceProvider encryptor, long length, ref Aes aes, ref Dictionary<string, string> albumArtistPair, bool isTrustedSyncTarget, string remoteHostname)
        {
            if (!isTrustedSyncTarget)
            {
#if DEBUG
                MyConsole.WriteLine("Trashing file");
#endif
                string trashPath = FileManager.GetAvailableTempFile("trash", "trash");
                networkStream.ReadFile(trashPath, length, ref aes);
                File.Delete(trashPath);
                return;
            }
            try
            {
                string songPath = FileManager.GetAvailableTempFile("receive", "mp3");
                networkStream.ReadFile(songPath, length, ref aes);
#if DEBUG
                MyConsole.WriteLine("Read file to temp");
#endif
                (List<string> missingArtists, (string missingAlbum, string albumArtistPath)) =
                    FileManager.AddSong(songPath, true, true, remoteHostname);
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
                }

                if (!string.IsNullOrEmpty(missingAlbum))
                {
#if DEBUG
                    MyConsole.WriteLine($"Missing album: {missingAlbum}");
#endif
                    networkStream.WriteCommand(CommandsArr.AlbumImageRequest,
                        Encoding.UTF8.GetBytes(missingAlbum), ref encryptor);
                    albumArtistPair.TryAdd(missingAlbum, albumArtistPath);
                }
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
            }
            networkStream.WriteCommand(CommandsArr.Ack, ref encryptor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ArtistImageSend(ref NetworkStream networkStream, ref RSACryptoServiceProvider encryptor, ref Aes aes, long length, byte[] data, bool isTrustedSyncTarget)
        {
            if (!isTrustedSyncTarget)
            {
                string trashPath = FileManager.GetAvailableTempFile("trash", "trash");
                networkStream.ReadFile(trashPath, length, ref aes);
                File.Delete(trashPath);
                return;
            }
            try
            {
                string artist = FileManager.GetAlias(Encoding.UTF8.GetString(data));
                string artistPath = FileManager.Sanitize(artist);
                string imagePath = FileManager.GetAvailableTempFile("networkImage", "image");
                networkStream.ReadFile(imagePath, length, ref aes);
                string imageExtension = FileManager.GetImageFormat(imagePath);
                string artistImagePath =
                    $"{FileManager.MusicFolder}/{artistPath}/cover.{imageExtension.TrimStart('.')}";
                File.Move(imagePath, artistImagePath);
                List<Artist> artists = MainActivity.StateHandler.Artists.Search(artist);
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
            }
            networkStream.WriteCommand(CommandsArr.Ack, ref encryptor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AlbumImageSend(ref NetworkStream networkStream, ref RSACryptoServiceProvider encryptor, ref Aes aes, long length, byte[] data, ref Dictionary<string,string> albumArtistPair, bool isTrustedSyncTarget)
        {
            if (!isTrustedSyncTarget)
            {
                string trashPath = FileManager.GetAvailableTempFile("trash", "trash");
                networkStream.ReadFile(trashPath, length, ref aes);
                File.Delete(trashPath);
                return;
            }
            try
            {
                string album = Encoding.UTF8.GetString(data);
                string albumPath = FileManager.Sanitize(album);
                string imagePath = FileManager.GetAvailableTempFile("networkImage", "image");
                networkStream.ReadFile(imagePath, length, ref aes);
                string imageExtension = FileManager.GetImageFormat(imagePath);
                string albumImagePath =
                    $"{FileManager.MusicFolder}/{albumArtistPair[album]}/{albumPath}/cover.{imageExtension.TrimStart('.')}";
                File.Move(imagePath, albumImagePath);
                List<Album> albums = MainActivity.StateHandler.Albums.Search(album);
                if (albums.Count > 1)
                {
                    int albumIndex = MainActivity.StateHandler.Albums.IndexOf(albums[0]);
                    MainActivity.StateHandler.Albums[albumIndex] = new Album(albums[0], albumImagePath);
                }
                albumArtistPair.Remove(album);
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
            }
            networkStream.WriteCommand(CommandsArr.Ack, ref encryptor);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ArtistImageRequest(ref NetworkStream networkStream, ref RSACryptoServiceProvider encryptor, ref Aes aes, ref int ackCount, byte[] data)
        {
            string artistName = Encoding.UTF8.GetString(data);
            List<Artist> artists = MainActivity.StateHandler.Artists.Search(artistName);
#if DEBUG
            MyConsole.WriteLine($"Got request for artist {artistName}");
#endif
            foreach (Artist artist in artists.Where(artist => artist.ImgPath != "Default"))
            {
                networkStream.WriteFile(artist.ImgPath, ref encryptor, ref aes, CommandsArr.ArtistImageSend, Encoding.UTF8.GetBytes(artists[0].Title));
                ackCount--;
#if DEBUG
                MyConsole.WriteLine($"Sending image for artist {artistName}");
#endif
                break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AlbumImageRequest(ref NetworkStream networkStream, ref RSACryptoServiceProvider encryptor,
            ref Aes aes, ref int ackCount, byte[] data)
        {
            string albumName = Encoding.UTF8.GetString(data);
            List<Album> albums = MainActivity.StateHandler.Albums.Search(albumName);
#if DEBUG
            MyConsole.WriteLine($"Got request for album {albumName}");
#endif
            foreach (Album album in albums.Where(album => album.ImgPath != "Default"))
            {
                networkStream.WriteFile(album.ImgPath, ref encryptor, ref aes, CommandsArr.AlbumImageSend, Encoding.UTF8.GetBytes(albums[0].Title));
                ackCount--;
#if DEBUG
                MyConsole.WriteLine($"Sending image for album {albumName}");
#endif
                break;
            }
        }
    }
}