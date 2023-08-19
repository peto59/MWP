
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Newtonsoft.Json;
using Xamarin.Essentials;
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
            byte command = Commands.None;
            bool ending = false;
            string remoteHostname = string.Empty;
            bool canSend = false;
            bool encrypted = false;

            RSACryptoServiceProvider encryptor = new RSACryptoServiceProvider();
            RSACryptoServiceProvider decryptor = new RSACryptoServiceProvider();
            Aes aes = Aes.Create();

            List<string> files = new List<string>();
            List<string> sent = new List<string>();

            byte[] host = Encoding.UTF8.GetBytes(DeviceInfo.Name);
            networkStream.Write(BitConverter.GetBytes(10), 0, 4);
            networkStream.Write(BitConverter.GetBytes(host.Length), 0, 4);
            networkStream.Write(host, 0, host.Length);

            while (true)
            {
                Thread.Sleep(50);

                if (networkStream.DataAvailable)
                {
                    command = encrypted ? networkStream.ReadCommand(ref decryptor) : networkStream.ReadCommand();
                }
#if DEBUG
                MyConsole.WriteLine($"Received command: {command}");
#endif
                if (files.Count > 0)
                {

                }
                else if (ending && !networkStream.DataAvailable)
                {
                    //TODO: check encrypted?
                    networkStream.WriteCommand(CommandsArr.End, ref encryptor);
#if DEBUG
                    MyConsole.WriteLine("send end");
#endif
                }
                else
                {
                    if (command != Commands.Host)
                    {
                        try
                        {
                            networkStream.WriteCommand(CommandsArr.None, ref encryptor);
                        }
                        catch
                        {
#if DEBUG
                            MyConsole.WriteLine("shut");
#endif
                            Thread.Sleep(100);
                        }
                    }
                }

                int length = 0;
                switch (command)
                {
                    case Commands.Host: //host
                        byte[] data = networkStream.ReadData();
                        remoteHostname = Encoding.UTF8.GetString(data, 0, length);
#if DEBUG
                        MyConsole.WriteLine($"hostname {remoteHostname}");
#endif
                        //SecureStorage.RemoveAll();

                        (string storedPrivKey, string storedPubKey) = await LoadKeys(remoteHostname);
                        if (storedPrivKey == null || storedPubKey == null)
                        {
                            GenerateRsaKeys(ref networkStream, ref decryptor, ref encryptor, remoteHostname);
                        }
                        else
                        {
                            encryptor.FromXmlString(storedPubKey);
                            decryptor.FromXmlString(storedPrivKey);
                        }

                        GetAesKey(ref networkStream, ref decryptor, ref encryptor, ref aes);
                        encrypted = true;
#if DEBUG
                        MyConsole.WriteLine("encrypted");
#endif

                        (bool exists, List<string> songs) = FileManager.GetSyncSongs(remoteHostname);
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
                        }
                        break;
                    case Commands.SyncRequest: //sync
                        if (FileManager.GetTrustedHost(remoteHostname))
                        {
                            networkStream.WriteCommand(CommandsArr.SyncAccepted, ref encryptor);
                        }
                        else
                        {
                            networkStream.WriteCommand(CommandsArr.SyncInfoRequest, ref encryptor);
                            
                            (command, data) = networkStream.ReadCommand(ref decryptor, ref aes);
                            //TODO: move all commands to switch to avoid throwing exception on wait
                            if (command != Commands.SyncInfo)
                            {
                                throw new Exception($"Request SyncInfo({Commands.SyncInfo} got {command})");
                            }
                            string json = Encoding.UTF8.GetString(data);
#if DEBUG
                            MyConsole.WriteLine(json);
#endif
                            List<string> recSongs = JsonConvert.DeserializeObject<List<string>>(json);
                            foreach(string s in recSongs)
                            {
#if DEBUG
                                MyConsole.WriteLine(s);
#endif
                            }
                            bool x = true;
                            if (x) //present some form of user check if they really want to receive files
                            {
                                //TODO: Stupid! Need to ask before connecting
                                networkStream.WriteCommand(CommandsArr.SyncAccepted, ref encryptor);
                                FileManager.AddTrustedHost(remoteHostname);
                            }
                            else
                            {
                                networkStream.WriteCommand(CommandsArr.SyncRejected, ref encryptor);
                            }
                        }
                        break;
                    case Commands.FileSend: //file
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
                    case Commands.End: //end
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
                            if (encrypted)
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
                    default: //wait or uninplemented
#if DEBUG
                        MyConsole.WriteLine($"default: {command}");
#endif
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
     
        private static async Task<(string storedPrivKey, string storedPubKey)> LoadKeys(string remoteHostname)
        {
            string storedPrivKey, storedPubKey;
            try
            {
                storedPrivKey = await SecureStorage.GetAsync($"{remoteHostname}_privkey");
                storedPubKey = await SecureStorage.GetAsync($"{remoteHostname}_pubkey");
            }
            catch
            {
                throw new Exception("You're fucked, boy. Go buy something else than Nokia 3310");
            }

            return (storedPrivKey, storedPubKey);
        }

        private static void GenerateRsaKeys(ref NetworkStream networkStream, ref RSACryptoServiceProvider decryptor,
            ref RSACryptoServiceProvider encryptor, string remoteHostname)
        {
#if DEBUG
            MyConsole.WriteLine("generating keys");
#endif
            (string pubKeyString, RSAParameters privKey) = NetworkManagerCommon.CreateKeyPair();
            decryptor.ImportParameters(privKey);
            byte[] data = Encoding.UTF8.GetBytes(pubKeyString);
                            
            networkStream.WriteCommand(CommandsArr.RsaExchange, data);


            byte command;
            (command, data) = networkStream.ReadCommandCombined();
#if DEBUG
            MyConsole.WriteLine($"command for enc {command}");
#endif
            if (command != Commands.RsaExchange)
            {
                throw new Exception("wrong order to establish cypher");
            }
                            
            pubKeyString = Encoding.UTF8.GetString(data, 0, data.Length);
            encryptor.FromXmlString(pubKeyString);

            //store private and public key for later use
            SecureStorage.SetAsync($"{remoteHostname}_privkey", decryptor.ToXmlString(true));
            SecureStorage.SetAsync($"{remoteHostname}_pubkey", pubKeyString);
        }

        private static void GetAesKey(ref NetworkStream networkStream, ref RSACryptoServiceProvider decryptor, ref RSACryptoServiceProvider encryptor, ref Aes aes)
        {
            aes.KeySize = 256;
            byte command;
            (command, aes.Key, _) = networkStream.ReadCommandCombined(ref decryptor);
#if DEBUG
            MyConsole.WriteLine($"command for AES {command}");
#endif
            if (command != Commands.AesSend)
            {
                throw new Exception("wrong order to establish cypher AES");
            }
            networkStream.WriteCommand(CommandsArr.AesReceived, ref encryptor);
        }
    }
}