using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Android.App;
using Java.Lang;
using Newtonsoft.Json;
using Xamarin.Essentials;
using Thread = System.Threading.Thread;
#if DEBUG
using Ass_Pain.Helpers;
#endif

namespace Ass_Pain.BackEnd.Network
{
    internal static class NetworkManagerClient
    {
        internal static async void Client(IPAddress server, int port)
        {
#if DEBUG
            MyConsole.WriteLine($"Connecting to: {server}:{port}");
#endif
            TcpClient client = new TcpClient(server.ToString(), port);
            NetworkStream networkStream = client.GetStream();
            EncryptionState encryptionState = EncryptionState.None;
            bool ending = false;
            string remoteHostname = string.Empty;
            bool canSend = false;

            RSACryptoServiceProvider encryptor = new RSACryptoServiceProvider();
            RSACryptoServiceProvider decryptor = new RSACryptoServiceProvider();
            Aes aes = Aes.Create();
            aes.KeySize = 256;

            List<string> files = new List<string>();
            //List<string> sent = new List<string>();
            
            networkStream.WriteCommand(CommandsArr.Host, Encoding.UTF8.GetBytes(DeviceInfo.Name));

            while (true)
            {
                Thread.Sleep(50);

                CommandsEnum command;
                byte[] data = null;
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
                            byte[] iv;
                            long? length;
                            (command, data, iv, length) = networkStream.ReadCommand(ref decryptor);
                            if (Commands.IsLong(command))
                            {
                                if (iv == null || length == null)
                                {
                                    throw new IllegalStateException("Received empty IV or length on long data");
                                }
                                aes.IV = iv;
                                data = networkStream.ReadEncrypted(ref aes, (long)length);
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

                #endregion
                
                switch (command)
                {
                    case CommandsEnum.Host: //host
                        remoteHostname = Encoding.UTF8.GetString(networkStream.ReadData());
#if DEBUG
                        MyConsole.WriteLine($"hostname {remoteHostname}");
#endif
                        //SecureStorage.RemoveAll();
                        
                        (string storedPrivKey, string storedPubKey) = await NetworkManagerCommon.LoadKeys(remoteHostname);
                        
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
                        
                        //TODO: here
                        /*(bool exists, List<string> songs) = FileManager.GetSyncSongs(remoteHostname);
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
                        }*/
                        break;
                    case CommandsEnum.RsaExchange:
                        string remotePubKeyString = Encoding.UTF8.GetString(data);
                        encryptor.FromXmlString(remotePubKeyString);

                        //store private and public key for later use
                        _ = SecureStorage.SetAsync($"{remoteHostname}_privkey", decryptor.ToXmlString(true));
                        _ = SecureStorage.SetAsync($"{remoteHostname}_pubkey", remotePubKeyString);
                        encryptionState = EncryptionState.AesSend;
                        break;
                    case CommandsEnum.AesSend:
                        aes.Key = data;
                        networkStream.WriteCommand(CommandsArr.AesReceived, ref encryptor);
                        encryptionState = EncryptionState.Encrypted;
#if DEBUG
                        MyConsole.WriteLine("encrypted");
#endif
                        //TODO: here
                        ending = true;
                        break;
                    case CommandsEnum.AesReceived:
                        throw new IllegalStateException("Client doesn't receive aes confirmation");
                    case CommandsEnum.SyncRequest: //sync
                        if (FileManager.GetTrustedHost(remoteHostname))
                        {
                            networkStream.WriteCommand(CommandsArr.SyncAccepted, ref encryptor);
                        }
                        else
                        {
                            networkStream.WriteCommand(CommandsArr.SyncInfoRequest, ref encryptor);
                        }
                        break;
                    case CommandsEnum.SyncAccepted:
                        canSend = true;
                        break;
                    case CommandsEnum.SyncInfoRequest:
                        //TODO: copy from server
                        break;
                    case CommandsEnum.SyncRejected:
                        ending = true;
                        break;
                    case CommandsEnum.SyncInfo:
                        string json = Encoding.UTF8.GetString(data);
#if DEBUG
                        MyConsole.WriteLine(json);
#endif
                        //TODO: move to song object
                        List<string> recSongs = JsonConvert.DeserializeObject<List<string>>(json);
#if DEBUG
                        foreach(string s in recSongs)
                        {
                            MyConsole.WriteLine(s);
                        }
#endif
                        bool x = true;
                        if (x) //present some form of user check if they really want to receive files
                        {
                            //TODO: Stupid! Need to ask before syncing
                            networkStream.WriteCommand(CommandsArr.SyncAccepted, ref encryptor);
                            FileManager.AddTrustedHost(remoteHostname);
                        }
                        else
                        {
                            networkStream.WriteCommand(CommandsArr.SyncRejected, ref encryptor);
                        }
                        break;
                    case CommandsEnum.FileSend: //file
                        int i = FileManager.GetAvailableFile("receive");
                        string musicPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMusic).AbsolutePath;
                        string path = $"{Application.Context.GetExternalFilesDir(null).AbsolutePath}/tmp/receive{i}.mp3";

                        networkStream.ReadFile(path, ref decryptor, ref aes);
                        
                        //TODO: move to song object
                        //TODO: offload to file manager
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
                    case CommandsEnum.End: //end
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
                    case CommandsEnum.None:
                    default: //wait or unimplemented
#if DEBUG
                        MyConsole.WriteLine($"default: {command}");
#endif
                        Thread.Sleep(25);
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
            networkStream.Close();
            client.Close();
            await networkStream.DisposeAsync();
            client.Dispose();
            NetworkManagerCommon.Connected.Remove(server);
        }
    }
}