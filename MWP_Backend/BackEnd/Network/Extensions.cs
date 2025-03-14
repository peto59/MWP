using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using MWP_Backend.DatatypesAndExtensions;
#if DEBUG
using MWP.BackEnd.Helpers;
using MWP.Helpers;
#endif

namespace MWP.BackEnd.Network
{
    internal static class ReadExtensions
    {
        /// <summary>
        /// Ensures correct number of <see cref="byte"/>s is read and waits for more if not
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="length">number of <see cref="byte"/>s to read from <paramref name="stream"/></param>
        /// <returns> <see cref="T:byte[]" /> of specified <paramref name="length"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] SafeRead(this NetworkStream stream, int length)
        {
            byte[] data = new byte[length];
            int offset = 0;
            while (length > 0)
            {
                while (!stream.DataAvailable)
                {
                    Thread.Sleep(10);
                }
                int read = stream.Read(data, offset, length);
                length -= read;
                offset += read;
            }
            return data;
        }
        
        /// <summary>
        /// Ensures correct number of <see cref="byte"/>s is read and waits for more if not
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="length">number of <see cref="byte"/>s to read from <paramref name="stream"/></param>
        /// <param name="networkStream">stream from which crypto stream reads</param>
        /// <returns> <see cref="T:byte[]" /> of specified <paramref name="length"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] SafeRead(this CryptoStream stream, int length, ref NetworkStream networkStream)
        {
            byte[] data = new byte[length];
            int offset = 0;
            while (length > 0)
            {
                /*while (!networkStream.DataAvailable)
                {
#if DEBUG
                    MyConsole.WriteLine($"Waiting for data");
#endif
                    Thread.Sleep(10);
                }*/
                int read = stream.Read(data, offset, length);
                length -= read;
                offset += read;
            }

            return data;
        }
        
        /// <summary>
        /// Ensures correct number of <see cref="byte"/>s is read and waits for more if not
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="length">number of <see cref="byte"/>s to read from <paramref name="stream"/></param>
        /// <returns> <see cref="T:byte[]" /> of specified <paramref name="length"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte[] SafeRead(this NetworkStream stream, long length)
        {
            byte[] retArr = new byte[length];
            long totalRead = 0;
            while (length > 0)
            {
                int readThisCycle = length > int.MaxValue ? int.MaxValue : Convert.ToInt32(length);
                Array.Copy(stream.SafeRead(readThisCycle), 0, retArr, totalRead, readThisCycle);
                length -= readThisCycle;
                totalRead = +readThisCycle;
            }

            return retArr;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte[] SafeRead(this Stream stream, int length)
        {
            byte[] retArr = new byte[length];
            long totalRead = 0;
            while (length > 0)
            {
                int readThisCycle = Convert.ToInt32(length);
                Array.Copy(stream.SafeRead(readThisCycle), 0, retArr, totalRead, readThisCycle);
                length -= readThisCycle;
                totalRead = +readThisCycle;
            }

            return retArr;
        }

        /// <summary>
        /// Read single unencrypted <see cref="CommandsEnum"/> from <paramref name="stream"/> 
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <returns><see cref="CommandsEnum"/> read from <paramref name="stream"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static CommandsEnum ReadCommand(this NetworkStream stream)
        {
            return (CommandsEnum)stream.SafeRead(1)[0];
        }


        /// <summary>
        /// Read single encrypted <see cref="CommandsEnum"/> from stream 
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="decryptor">decryptor to be used</param>
        /// <returns><see cref="CommandsEnum"/> read from <paramref name="stream"/></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (CommandsEnum command, byte[]? data, byte[]? iv, long? length) ReadCommand(this NetworkStream stream, ref RSACryptoServiceProvider decryptor)
        {
            byte[] buff = decryptor.Decrypt(stream.SafeRead(NetworkManagerCommon.RsaDataSize), true);
            if (buff.Length == 1) return ((CommandsEnum)buff[0], null, null, null);
            
            CommandsEnum command = (CommandsEnum)buff[0];
            byte[] restOfData = buff[1..];
            if (Commands.IsLong(command))
            {
                byte[] iv = new byte[16];
                Array.Copy(restOfData, iv, 16);
                long longLength = BitConverter.ToInt64(restOfData, 16);
                if (restOfData.Length <= 24) // 16 for iv, 8 for int64
                    return (command, null, iv, longLength);
                byte[] buffer = restOfData[24..]; // 24 = 16 for iv, 8 for int64
                //Array.Copy(restOfData, 24, buffer, 0, buffer.Length);
                return (command, buffer, iv, longLength);
            }

            int length = BitConverter.ToInt32(restOfData);
            byte[] data = new byte[length];
            Array.Copy(restOfData, 4, data, 0, length);
            return (command, data,  null, null);
        }

        /// <summary>
        /// Reads <see cref="CommandsEnum"/>, <see cref="long">long</see> data length and long data from <paramref name="stream"/> and decrypts them
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="decryptor">rsa decryptor to be used</param>
        /// <param name="aes">aes decryptor to be used</param>
        /// <returns><see cref="CommandsEnum"/>, <see cref="T:byte[]" /> read data</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (byte command, byte[] retArr) ReadCommand(this NetworkStream stream,
            ref RSACryptoServiceProvider decryptor, ref Aes aes)
        {
            const int len = 1 + 16 + 8; //command(1), aes IV (16), encrypted data length as long(8)
            byte[] arr = stream.SafeRead(len);
            arr = decryptor.Decrypt(arr, true);
            byte command = arr[0];
            long readLength = BitConverter.ToInt64(arr, 17);
            Array.Copy(arr, 1, aes.IV, 0, 16);
            return (command, stream.ReadEncrypted(ref aes, readLength));
        }
        
        /// <summary>
        /// reads <see cref="CommandsEnum"/> and it's data
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <returns><see cref="CommandsEnum"/> and <see cref="T:byte[]" /> read data</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (CommandsEnum command, byte[] data) ReadCommandCombined(this NetworkStream stream)
        {
            CommandsEnum command = stream.ReadCommand();
            return Commands.IsLong(command) ? (command, stream.ReadData(true)) : (command, stream.ReadData());
        }
        
        /// <summary>
        /// reads encrypted <see cref="CommandsEnum"/>, <see cref="T:byte[]" /> data from <paramref name="stream"/> and decrypts them if they are <see cref="int"/>, otherwise returns <see cref="Aes.IV"/> and decryption is needed to be done manually
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="decryptor">decryptor to be used</param>
        /// <returns>read <see cref="CommandsEnum"/>, <see cref="T:byte[]" /> data and <see cref="Aes.IV"/> if data is <see cref="long">long</see> based on <see cref="CommandsEnum"/> type</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (byte command, byte[]? data, byte[]? iv, long? length) ReadCommandCombined(this NetworkStream stream, ref RSACryptoServiceProvider decryptor)
        {
            byte[] arr = stream.SafeRead(NetworkManagerCommon.RsaDataSize);
            arr = decryptor.Decrypt(arr, true);
            byte command = arr[0];
            if (Commands.IsLong(command))
            {
                byte[] iv = new byte[16];
                Array.Copy(arr, 1, iv, 0, 16);
                long longLength = BitConverter.ToInt64(arr, 17);
                return (command, null, iv, longLength);
            }
            int length = BitConverter.ToInt32(arr, 1);
            byte[] data = new byte[length];
            Array.Copy(arr, 5, data, 0, length);
            return (command, data, null, null);
        }

        /// <summary>
        /// reads <see cref="T:byte[]" /> data from <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="isLong">whether data is long</param>
        /// <returns>read <see cref="T:byte[]" /> data</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte[] ReadData(this NetworkStream stream, bool isLong = false)
        {
            byte[] len;
            if (isLong)
            {
                len = stream.SafeRead(8);
                long lengthLong = BitConverter.ToInt64(len, 0);
                return stream.SafeRead(lengthLong);
            }

            len = stream.SafeRead(4);
            int length = BitConverter.ToInt32(len, 0);
            return stream.SafeRead(length);
        }
        
        /// <summary>
        /// Reads encrypted <see cref="T:byte[]" /> data and decrypts them
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="decryptor">decryptor to be used</param>
        /// <returns>decrypted <see cref="T:byte[]" /></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte[] ReadData(this NetworkStream stream, ref RSACryptoServiceProvider decryptor)
        {
            return decryptor.Decrypt(stream.SafeRead(NetworkManagerCommon.RsaDataSize), true);
        }
        
        
        /// <summary>
        /// Reads encrypted <see cref="long">long</see> <see cref="T:byte[]" /> data and decrypts them
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="decryptor">decryptor to be used</param>
        /// <param name="aes">aes to decrypt with</param>
        /// <returns>decrypted <see cref="T:byte[]" /></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte[] ReadData(this NetworkStream stream, ref RSACryptoServiceProvider decryptor, ref Aes aes)
        {
            byte[] buffer = decryptor.Decrypt(stream.SafeRead(NetworkManagerCommon.RsaDataSize), true);
            Array.Copy(buffer, aes.IV, 16);
            long length = BitConverter.ToInt64(buffer, 16);
            
            return stream.ReadEncrypted(ref aes, length);
            
        }

        /// <summary>
        /// Reads encrypted <see cref="long">long</see> <see cref="T:byte[]" /> data and decrypts them using  <paramref name="aes"/> assuming  <paramref name="readLength"/> is known
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="aes">aes to decrypt with</param>
        /// <param name="readLength">number of bytes to read</param>
        /// <returns>decrypted <see cref="T:byte[]" /></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte[] ReadEncrypted(this NetworkStream stream, ref Aes aes, long readLength)
        {
            using MemoryStream msEncrypted = new MemoryStream();
            while (readLength > 0)
            {
                int readThisCycle = readLength > NetworkManager.DefaultBuffSize ? NetworkManager.DefaultBuffSize : Convert.ToInt32(readLength);
                byte[] read = stream.SafeRead(readThisCycle);
                msEncrypted.Write(read);
                readLength -= readThisCycle;
            }

            if (msEncrypted.CanSeek)
            {
                msEncrypted.Seek(0, SeekOrigin.Begin);
            }
            CryptoStream csDecrypt = new CryptoStream(msEncrypted, aes.CreateDecryptor(), CryptoStreamMode.Read, true);
            using MemoryStream msDecrypt = new MemoryStream();
            msDecrypt.WriteData(csDecrypt);
            csDecrypt.Flush();
            //csDecrypt.FlushFinalBlock();
            try
            {
                csDecrypt.Dispose();
            }
            catch (Exception e)
            {
#if DEBUG
                MyConsole.WriteLine(e);
#endif
                //ignored
            }
            return msDecrypt.ToArray();
        }

        /// <summary>
        /// Reads encrypted <see cref="File" /> from stream to <see cref="File" /> specified at <see cref="Path" /> (this file <see cref="File" /> be created/overwritten)
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="path">path to write to</param>
        /// <param name="decryptor">rsa decryptor to be used</param>
        /// <param name="aes">aes decryptor to be used</param>
        /// <exception cref="Exception">Cannot receive files larger than 4GB on Android</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ReadFile(this NetworkStream stream, string path, ref RSACryptoServiceProvider decryptor,
            ref Aes aes)
        {
            const int len = 16 + 8; //aes IV (16), encrypted data length as long(8)
            byte[] arr = stream.SafeRead(len);
            arr = decryptor.Decrypt(arr, true);
            long length = BitConverter.ToInt64(arr, 16);
            if(length > 4000000000){
                throw new Exception("You can't receive files larger than 4GB on Android");
            }
            Array.Copy(arr, 1, aes.IV, 0, 16);
            using FileStream fileStream = new FileStream(path, FileMode.Create);

            CryptoStream csDecrypt = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Read, true);
            long readLength = length;
            while (readLength > 0)
            {
                if (!stream.DataAvailable)
                {
                    Thread.Sleep(10);
                }

                long toRead = stream.Length < readLength ? stream.Length : readLength;
                int readThisCycle = readLength > int.MaxValue ? int.MaxValue : Convert.ToInt32(toRead);
                csDecrypt.CopyTo(fileStream, readThisCycle);
                readLength -= readThisCycle;
            }
            csDecrypt.Dispose();
        }

        /// <summary>
        /// Reads encrypted <see cref="File" /> from stream to <see cref="File" /> specified at <see cref="Path" /> (this file <see cref="File" /> be created/overwritten)
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="path">path to write to</param>
        /// <param name="length">length of encrypted data</param>
        /// <param name="aes">aes decryptor to be used</param>
        /// <param name="encryptor">rsa encryptor to be used for wait messages</param>
        /// <exception cref="Exception">Cannot receive files larger than 4GB on Android</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ReadFile(this NetworkStream stream, string path, long length,
            ref Aes aes, ref RSACryptoServiceProvider encryptor)
        {
            if(length > 4000000000){
                throw new Exception("You can't receive files larger than 4GB on Android");
            }
            FileStream fileStream = new FileStream(path, FileMode.Create);
            string encryptedFilePath = FileManager.GetAvailableTempFile("encrypted", "file");
            FileStream encryptedFileStream = new FileStream(encryptedFilePath, FileMode.Create);
            while (length > 0)
            {
                int readThisCycle = length > NetworkManager.DefaultBuffSize ? NetworkManager.DefaultBuffSize : Convert.ToInt32(length);
                byte[] buffer = stream.SafeRead(readThisCycle);
                encryptedFileStream.Write(buffer);
                length -= readThisCycle;
            }
            stream.WriteCommand(CommandsArr.Wait, ref encryptor);
            encryptedFileStream.Seek(0, SeekOrigin.Begin);
            CryptoStream csDecrypt = new CryptoStream(encryptedFileStream, aes.CreateDecryptor(), CryptoStreamMode.Read, false);
            fileStream.WriteData(csDecrypt);
            csDecrypt.Dispose();
            encryptedFileStream.Dispose();
            fileStream.Dispose();
            File.Delete(encryptedFilePath);
        }
        
        /// <summary>
        /// Trashes current file in buffer
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="length">length of encrypted data</param>
        /// <param name="encryptor">rsa encryptor to be used for wait messages</param>
        /// <exception cref="Exception">Cannot receive files larger than 4GB on Android</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ReadFileTrash(this NetworkStream stream, long length, ref RSACryptoServiceProvider encryptor)
        {
            if(length > 4000000000){
                throw new Exception("You can't receive files larger than 4GB on Android");
            }
            string encryptedFilePath = FileManager.GetAvailableTempFile("trash", "file");
            FileStream encryptedFileStream = new FileStream(encryptedFilePath, FileMode.Create);
            while (length > 0)
            {
                int readThisCycle = length > NetworkManager.DefaultBuffSize ? NetworkManager.DefaultBuffSize : Convert.ToInt32(length);
                byte[] buffer = stream.SafeRead(readThisCycle);
                encryptedFileStream.Write(buffer);
                length -= readThisCycle;
            }
            encryptedFileStream.Dispose();
            File.Delete(encryptedFilePath);
        }
    }
    
    internal static class WriteExtensions
    {
        /// <summary>
        /// Writes unencrypted <see cref="CommandsEnum" /> to <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">stream to write to</param>
        /// <param name="command"><see cref="CommandsEnum" /> to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteCommand(this NetworkStream stream, byte[] command)
        {
            stream.Write(command, 0, 1);
        }

        /// <summary>
        /// Writes encrypted <see cref="CommandsEnum" /> to <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">stream to write to</param>
        /// <param name="command"><see cref="CommandsEnum" /> to write</param>
        /// <param name="encryptor">encryptor to be used</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteCommand(this NetworkStream stream, byte[] command,
            ref RSACryptoServiceProvider encryptor)
        {
            byte[] enc = encryptor.Encrypt(command, true);
            stream.Write(enc, 0, enc.Length);
            
        }
        
        /// <summary>
        /// Writes <see cref="CommandsEnum" />, <see cref="int" /> data length and <see cref="T:byte[]" /> data as one unencrypted <see cref="T:byte[]" /> to <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">stream to write to</param>
        /// <param name="command"><see cref="CommandsEnum" /> to write</param>
        /// <param name="data"><see cref="T:byte[]" />data to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteCommand(this NetworkStream stream, byte[] command, byte[] data)
        {
            int len = 1 + 4 + data.Length;
            byte[] rv = new byte[len];
            Buffer.BlockCopy(command, 0, rv, 0, 1);
            Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, rv, 1, 4);
            Buffer.BlockCopy(data, 0, rv, 5, data.Length);
            stream.Write(rv, 0, len);
        }
        
        /// <summary>
        /// Writes <see cref="CommandsEnum" />, <see cref="int" /> data length and <see cref="T:byte[]" /> data as one encrypted <see cref="T:byte[]" /> to <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">stream to write to</param>
        /// <param name="command"><see cref="CommandsEnum" /> to write</param>
        /// <param name="data"><see cref="T:byte[]" />data to write</param>
        /// <param name="encryptor">encryptor to be used</param>
        /// <exception cref="InvalidDataException"><see cref="RSACryptoServiceProvider" /> with key length of 2048 has max data length of 190 bytes. 5 bytes are reserved for command and data length leaving us 185 bytes for data </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteCommand(this NetworkStream stream, byte[] command, byte[] data,
            ref RSACryptoServiceProvider encryptor)
        {
            int len = 1 + 4 + data.Length; //command(1), data Length as int(4)
            if (len > 190)
            {
                throw new InvalidDataException("Data cannot exceed 185 bytes"); //185 == 190 - command(1 byte) - data.Length(4 bytes)
            }
            byte[] rv = new byte[len];
            Buffer.BlockCopy(command, 0, rv, 0, 1);
            Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, rv, 1, 4);
            Buffer.BlockCopy(data, 0, rv, 5, data.Length);
            byte[] enc = encryptor.Encrypt(rv, true);
            stream.Write(enc, 0, enc.Length);
        }
        
        /// <summary>
        /// Writes <see cref="CommandsEnum" />, <see cref="Aes.IV" />, encrypted <see cref="long">long</see> data length as one encrypted <see cref="T:byte[]" /> to <paramref name="stream"/>, afterwards writes <see cref="long">long</see> encrypted <see cref="T:byte[]" /> data to <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">stream to write to</param>
        /// <param name="command"><see cref="CommandsEnum" /> to write</param>
        /// <param name="data">long data to write</param>
        /// <param name="encryptor">rsa encryptor to be used</param>
        /// <param name="aes">aes encryptor to be used</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteCommand(this NetworkStream stream, byte[] command, byte[] data,
            ref RSACryptoServiceProvider encryptor, ref Aes aes)
        {
            const int len = 1 + 16 + 8; //command(1), aes IV (16), encrypted data length as long(8)
            long encryptedDataLength = data.LongLength + (16 - data.LongLength % 16);
            byte[] rv = new byte[len];
            aes.GenerateIV();
            
            Buffer.BlockCopy(command, 0, rv, 0, 1);
            Buffer.BlockCopy(aes.IV, 0, rv, 1, 16);
            Buffer.BlockCopy(BitConverter.GetBytes(encryptedDataLength), 0, rv, 17, 8);
            rv = encryptor.Encrypt(rv, true);
            stream.Write(rv, 0, rv.Length);

            CryptoStream csEncrypt = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write, true);
            csEncrypt.WriteLongData(data);
            csEncrypt.Flush();
            csEncrypt.FlushFinalBlock();
            csEncrypt.Dispose();
            stream.Flush();
        }
        
        /// <summary>
        /// Writes <see cref="CommandsEnum" />, <see cref="long">long</see> data length as one encrypted <see cref="T:byte[]" /> to <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">stream to write to</param>
        /// <param name="data">data to write</param>
        /// <param name="encryptor">encryptor to be used</param>
        /// <exception cref="InvalidDataException"><see cref="RSACryptoServiceProvider" /> with key length of 2048 has max data length of 190 bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteData(this NetworkStream stream, byte[] data,
            ref RSACryptoServiceProvider encryptor)
        {
            if (data.Length > 190)
            {
                throw new InvalidDataException("Data cannot exceed 190 bytes");
            }
            byte[] enc = encryptor.Encrypt(data, true);
            stream.Write(enc, 0, enc.Length);
        }
        
        /// <summary>
        /// Writes <see cref="CommandsEnum" />, <see cref="Aes.IV" />, encrypted <see cref="long">long</see> data length as one encrypted <see cref="T:byte[]" /> to <paramref name="stream"/>, afterwards writes <see cref="long">long</see> encrypted <see cref="T:byte[]" /> data to <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">stream to write to</param>
        /// <param name="data">long data to write</param>
        /// <param name="encryptor">rsa encryptor to be used</param>
        /// <param name="aes">aes encryptor to be used</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteData(this NetworkStream stream, byte[] data,
            ref RSACryptoServiceProvider encryptor, ref Aes aes)
        {
            const int len =  16 + 8; //aes IV (16), encrypted data length as long(8)
            long encryptedDataLength = data.LongLength + (16 - data.LongLength % 16);
            byte[] rv = new byte[len];
            aes.GenerateIV();
            
            Buffer.BlockCopy(aes.IV, 0, rv, 0, 16);
            Buffer.BlockCopy(BitConverter.GetBytes(encryptedDataLength), 0, rv, 16, 8);
            rv = encryptor.Encrypt(rv, true);
            stream.Write(rv, 0, rv.Length);

            CryptoStream csEncrypt = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write, true);
            csEncrypt.WriteLongData(data);
            csEncrypt.Dispose();
        }
        
        /// <summary>
        /// Writes <see cref="CommandsEnum" />, <see cref="Aes.IV" />, encrypted <see cref="long">long</see> data length as one encrypted <see cref="T:byte[]" /> to <paramref name="stream"/>, afterwards writes serialized <see cref="List{Song}" /> to <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">stream to write to</param>
        /// <param name="songs">list of songs to serialize</param>
        /// <param name="encryptor">rsa encryptor to be used</param>
        /// <param name="aes">aes encryptor to be used</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteData(this NetworkStream stream, List<Song> songs,
            ref RSACryptoServiceProvider encryptor, ref Aes aes)
        {
            const int len =  1 + 16 + 8; //command (1), aes IV (16), encrypted data length as long(8)
            byte[] rv = new byte[len];
            aes.GenerateIV();
            
            SongJsonConverter customConverter = new SongJsonConverter(false);
            byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(songs, customConverter));
            
            Buffer.BlockCopy(CommandsArr.SongRequestInfo, 0, rv, 0, 1);
            Buffer.BlockCopy(aes.IV, 0, rv, 1, 16);
            Buffer.BlockCopy(BitConverter.GetBytes(data.LongLength), 0, rv, 17, 8);

            rv = encryptor.Encrypt(rv, true);
            stream.Write(rv, 0, rv.Length);
            
            CryptoStream csEncrypt = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write, true);
            csEncrypt.WriteLongData(data);
            csEncrypt.Dispose();
        }

        /// <summary>
        /// Copies data from <paramref name="source"/> to <paramref name="destination"/>
        /// </summary>
        /// <param name="destination">Destination of copy</param>
        /// <param name="source">Source of copy</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteData(this Stream destination, Stream source)
        {
            if (source.CanSeek)
            {
                // Set the position of source to the beginning
                source.Seek(0, SeekOrigin.Begin);
            }
            source.CopyTo(destination);
            /*long writeLength = source.Length;
            
#if DEBUG
            MyConsole.WriteLine($"File size {writeLength}");
#endif
            while (writeLength > 0)
            {
                int writeThisCycle = writeLength > NetworkManager.DefaultBuffSize ? NetworkManager.DefaultBuffSize : Convert.ToInt32(writeLength);
                byte[] read = source.SafeRead(writeThisCycle);
                destination.Write(read);
#if DEBUG
                MyConsole.WriteLine($"rem {writeLength}");
#endif
                writeLength -= writeThisCycle;
            }*/
            
            source.Flush();
            destination.Flush();
        }
        
        

        /// <summary>
        /// Writes long <paramref name="data"/> to <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">stream to be written to</param>
        /// <param name="data">data to be written</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void WriteLongData(this Stream stream, byte[] data)
        {
            long writeLength = data.LongLength;
            long totalWritten = 0;
            while (writeLength > 0)
            {
                int writeThisCycle = writeLength > NetworkManager.DefaultBuffSize ? NetworkManager.DefaultBuffSize : Convert.ToInt32(writeLength);
                writeLength -= writeThisCycle;
                byte[] toBeWritten = new byte[writeThisCycle];
                Array.Copy(data, totalWritten, toBeWritten, 0, writeThisCycle);
                stream.Write(toBeWritten, 0, writeThisCycle);
                totalWritten += writeThisCycle;
            }
        }

        /// <summary>
        /// Encrypts and writes <see cref="File" /> to <paramref name="stream"/>
        /// </summary>
        /// <param name="stream">stream to be written to</param>
        /// <param name="path">path to <see cref="File" /> that's to be written to <paramref name="stream"/></param>
        /// <param name="encryptor">rsa encryptor to be used</param>
        /// <param name="aes">aes encryptor to be used</param>
        /// <param name="command">command to write, default is <see cref="CommandsEnum.SongSend" /></param>
        /// <param name="data">optional extra data to be written</param>
        /// <exception cref="InvalidDataException"><see cref="RSACryptoServiceProvider" /> with key length of 2048 has max data length of 190 bytes.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteFile(this NetworkStream stream, string path,
            ref RSACryptoServiceProvider encryptor, ref Aes aes, byte[]? command = null, byte[]? data = null)
        {
            if (!File.Exists(path))
            {
                return;
            }
            int len = 1 + 16 + 8 + (data?.Length ?? 0); //command(1), aes IV (16), encrypted data length as long(8)
            if (len > 190 )
            {
                //TODO: send extra data as long?
                throw new InvalidDataException("Data cannot exceed 190 bytes");
            }
            FileInfo fi = new FileInfo(path);
            long encryptedDataLength = fi.Length + (16 - fi.Length % 16);
            byte[] rv = new byte[len];
            aes.GenerateIV();
            
            Buffer.BlockCopy(command ?? CommandsArr.SongSend, 0, rv, 0, 1);
            Buffer.BlockCopy(aes.IV, 0, rv, 1, 16);
            Buffer.BlockCopy(BitConverter.GetBytes(encryptedDataLength), 0, rv, 17, 8);
#if DEBUG
            MyConsole.WriteLine($"write length: {encryptedDataLength}");
            MyConsole.WriteLine($"file length: {fi.Length}");
#endif
            if (data != null)
            {
                Buffer.BlockCopy(data, 0, rv, 25, data.Length);
            }
#if DEBUG
            MyConsole.WriteLine($"rv {rv.Length}");
#endif
            rv = encryptor.Encrypt(rv, true);
            stream.Write(rv, 0, rv.Length);
            /*using MemoryStream ms = new MemoryStream();
            using CryptoStream csEncrypt = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write, true);
            using FileStream fs = fi.Open(FileMode.Open);
            csEncrypt.WriteData(fs);
            //ms.CopyTo(stream);
            csEncrypt.FlushFinalBlock();
            csEncrypt.Flush();
            csEncrypt.Close();
            csEncrypt.Clear();
            csEncrypt.Dispose();
            byte[] enc = ms.ToArray();
            stream.Write(enc);
            if (enc.LongLength != encryptedDataLength)
            {
                throw new Java.Lang.Exception("Invalid data size");
            }*/
#if DEBUG
            MyConsole.WriteLine("rv written");
#endif
            CryptoStream csEncrypt = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write, true);
#if DEBUG
            MyConsole.WriteLine("crypto opened");
#endif
            using FileStream fs = fi.Open(FileMode.Open, FileAccess.Read);
#if DEBUG
            MyConsole.WriteLine("fs opened");
#endif
            byte[] buffer = new byte[NetworkManager.DefaultBuffSize];
            int bytesRead;
            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                csEncrypt.Write(buffer, 0, bytesRead);
            }
#if DEBUG
            MyConsole.WriteLine("flushing file");
#endif
            csEncrypt.FlushFinalBlock();
            csEncrypt.Flush();
            csEncrypt.Close();
            csEncrypt.Clear();
            csEncrypt.Dispose();
            
            stream.Flush();
#if DEBUG
            MyConsole.WriteLine("Finished writing file");
#endif
        }
    }
}