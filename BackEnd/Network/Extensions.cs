#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;

namespace Ass_Pain.BackEnd.Network
{
    

    internal static class ReadExtensions
    {
        /// <summary>
        /// Ensures correct number of bytes is read and waits for more if not
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="length">number of bytes to read from stream</param>
        /// <returns>byte array of specified length</returns>
        private static byte[] SafeRead(this Stream stream, int length)
        {
            byte[] data = new byte[length];
            int offset = 0;
            while (length > 0)
            {
                offset += stream.Read(data, offset, length);
                length -= offset;
                Thread.Sleep(10);
            }

            return data;
        }
        
        /// <summary>
        /// Ensures correct number of bytes is read and waits for more if not
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="length">number of bytes to read from stream</param>
        /// <returns>byte array of specified length</returns>
        private static byte[] SafeRead(this Stream stream, long length)
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

        [Obsolete]
        internal static byte[] SafeReadObsolete(this Stream stream, long length)
        {
            byte[] retArr = new byte[length];
            uint arrLength = (uint)Math.Ceiling((double)length / int.MaxValue);
            byte[][] tmp = new byte[arrLength][];
            long readLength = length;
            bool shouldIncrement = false;
            uint index = 0;
            int lastArrLength = 0;
            while (readLength > 0)
            {
                int readThisCycle;
                if (readLength > int.MaxValue)
                {
                    if (shouldIncrement)
                    {
                        index++;
                    }

                    readThisCycle = int.MaxValue;
                    shouldIncrement = true;
                }
                else
                {
                    readThisCycle = Convert.ToInt32(readLength);
                    lastArrLength = readThisCycle;
                }

                tmp[index] = stream.SafeRead(readThisCycle);
                readLength -= readThisCycle;
            }

            for (int i = 0; i < arrLength - 1; i++)
            {
                Array.Copy(tmp[index], 0, retArr, i * (long)int.MaxValue, int.MaxValue);
            }

            Array.Copy(tmp[arrLength - 1], 0, retArr, arrLength - 1 * (long)int.MaxValue, lastArrLength);
            return retArr;
        }

        /// <summary>
        /// Read single unencrypted command from stream 
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <returns>Command read from stream</returns>
        internal static CommandsEnum ReadCommand(this NetworkStream stream)
        {
            return (CommandsEnum)stream.SafeRead(1)[0];
        }

        /// <summary>
        /// Read single encrypted command from stream 
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="decryptor">decryptor to be used</param>
        /// <returns>Command read from stream</returns>
        internal static (CommandsEnum command, byte[]? data, byte[]? iv, long? length) ReadCommand(this NetworkStream stream, ref RSACryptoServiceProvider decryptor)
        {
            byte[] buff = decryptor.Decrypt(stream.SafeRead(NetworkManagerCommon.RsaDataSize), true);
            if (buff.Length == 1) return ((CommandsEnum)buff[0], null, null, null);
            
            CommandsEnum command = (CommandsEnum)buff[0];
            byte[] restOfData = buff.TakeLast(buff.Length - 1).ToArray();
            if (Commands.IsLong(command))
            {
                byte[] iv = new byte[16];
                Array.Copy(restOfData, iv, 16);
                long longLength = BitConverter.ToInt64(restOfData, 16);
                return (command, null, iv, longLength);
            }

            int length = BitConverter.ToInt32(restOfData);
            byte[] data = new byte[length];
            Array.Copy(restOfData, 4, data, 0, length);
            return (command, data,  null, null);
        }

        /// <summary>
        /// Reads command, long data length and long data from stream and decrypts them
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="decryptor">rsa decryptor to be used</param>
        /// <param name="aes">aes decryptor to be used</param>
        /// <returns>command, length of read data, read data</returns>
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

        [Obsolete]
        internal static (byte command, long length, byte[] retArr) ReadCommandSemiObsolete(this NetworkStream stream,
            ref RSACryptoServiceProvider decryptor, ref Aes aes)
        {
            const int len = 1 + 16 + 8; //command(1), aes IV (16), encrypted data length as long(8)
            byte[] arr = stream.SafeRead(len);
            arr = decryptor.Decrypt(arr, true);
            byte command = arr[0];
            long length = BitConverter.ToInt64(arr, 17);
            byte[] retArr = new byte[length];
            Array.Copy(arr, 1, aes.IV, 0, 16);

            uint arrLength = (uint)Math.Ceiling((double)length / int.MaxValue);
            byte[][] tmp = new byte[arrLength][];
            using CryptoStream csDecrypt = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Read, true);
            long readLength = length;
            bool shouldIncrement = false;
            uint index = 0;
            int lastArrLength = 0;
            while (readLength > 0)
            {
                int readThisCycle;
                if (readLength > int.MaxValue)
                {
                    if (shouldIncrement)
                    {
                        index++;
                    }

                    readThisCycle = int.MaxValue;
                    shouldIncrement = true;
                }
                else
                {
                    readThisCycle = Convert.ToInt32(readLength);
                    lastArrLength = readThisCycle;
                }

                tmp[index] = csDecrypt.SafeRead(readThisCycle);
                readLength -= readThisCycle;
            }

            for (int i = 0; i < arrLength - 1; i++)
            {
                Array.Copy(tmp[index], 0, retArr, i * (long)int.MaxValue, int.MaxValue);
            }

            Array.Copy(tmp[arrLength - 1], 0, retArr, arrLength - 1 * (long)int.MaxValue, lastArrLength);
            return (command, length, retArr);
        }

        [Obsolete]
        internal static (byte command, long length, byte[] retArr) ReadCommandObsolete(this NetworkStream stream,
            ref RSACryptoServiceProvider decryptor, ref Aes aes)
        {
            const int len = 1 + 16 + 8; //command(1), aes IV (16), encrypted data length as long(8)
            byte[] arr = stream.SafeRead(len);
            arr = decryptor.Decrypt(arr, true);
            byte command = arr[0];
            long length = BitConverter.ToInt64(arr, 17);
            byte[] retArr = new byte[length];
            Array.Copy(arr, 1, aes.IV, 0, 16);

            while (!stream.DataAvailable)
            {
                Thread.Sleep(10);
            }

            arr = stream.SafeRead(length);
            uint arrLength = (uint)Math.Ceiling((double)length / int.MaxValue);
            byte[][] tmp = new byte[arrLength][];
            using MemoryStream msDecrypt = new MemoryStream(arr);
            using CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Read, true);
            long readLength = length;
            bool shouldIncrement = false;
            uint index = 0;
            int lastArrLength = 0;
            while (readLength > 0)
            {
                int readThisCycle;
                if (readLength > int.MaxValue)
                {
                    if (shouldIncrement)
                    {
                        index++;
                    }

                    readThisCycle = int.MaxValue;
                    shouldIncrement = true;
                }
                else
                {
                    readThisCycle = Convert.ToInt32(readLength);
                    lastArrLength = readThisCycle;
                }

                tmp[index] = csDecrypt.SafeRead(readThisCycle);
                readLength -= readThisCycle;
            }

            for (int i = 0; i < arrLength - 1; i++)
            {
                Array.Copy(tmp[index], 0, retArr, i * (long)int.MaxValue, int.MaxValue);
            }

            Array.Copy(tmp[arrLength - 1], 0, retArr, arrLength - 1 * (long)int.MaxValue, lastArrLength);
            return (command, length, retArr);
        }
        
        /// <summary>
        /// reads command and it's data and returns them
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <returns>command and read data</returns>
        internal static (CommandsEnum command, byte[] data) ReadCommandCombined(this NetworkStream stream)
        {
            CommandsEnum command = stream.ReadCommand();
            return Commands.IsLong(command) ? (command, stream.ReadData(true)) : (command, stream.ReadData());
        }
        
        /// <summary>
        /// reads encrypted data from stream and decrypts them if they are short, otherwise returns IV and decryption is needed to be done manually
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="decryptor">decryptor to be used</param>
        /// <returns>read command, read data and IV if data is long based on command type</returns>
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
        /// reads data from stream
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="isLong">whether data is long</param>
        /// <returns></returns>
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
        /// Reads encrypted data and decrypts them
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="decryptor">decryptor to be used</param>
        /// <returns>decrypted byte array</returns>
        internal static byte[] ReadData(this NetworkStream stream, ref RSACryptoServiceProvider decryptor)
        {
            return decryptor.Decrypt(stream.SafeRead(NetworkManagerCommon.RsaDataSize), true);
        }
        
        
        /// <summary>
        /// Reads encrypted long data and decrypts them
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="decryptor">decryptor to be used</param>
        /// <param name="aes">aes to decrypt with</param>
        /// <returns>decrypted byte array</returns>
        internal static byte[] ReadData(this NetworkStream stream, ref RSACryptoServiceProvider decryptor, ref Aes aes)
        {
            byte[] buffer = decryptor.Decrypt(stream.SafeRead(NetworkManagerCommon.RsaDataSize), true);
            Array.Copy(buffer, aes.IV, 16);
            long length = BitConverter.ToInt64(buffer, 16);
            
            return stream.ReadEncrypted(ref aes, length);
            
        }

        /// <summary>
        /// Reads encrypted long data and decrypts them using aes assuming length is known
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="aes">aes to decrypt with</param>
        /// <param name="readLength">number of bytes to read</param>
        /// <returns>decrypted byte array</returns>
        internal static byte[] ReadEncrypted(this Stream stream, ref Aes aes, long readLength)
        {
            byte[] retArr = new byte[readLength];
            long totalRead = 0;
            CryptoStream csDecrypt = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Read, true);
            while (readLength > 0)
            {
                int readThisCycle = readLength > int.MaxValue ? int.MaxValue : Convert.ToInt32(readLength);
                Array.Copy(stream.SafeRead(readThisCycle), 0, retArr, totalRead, readThisCycle);
                readLength -= readThisCycle;
                totalRead = +readThisCycle;
            }
            csDecrypt.Dispose();
            return retArr;
        }

        /// <summary>
        /// Reads encrypted file from stream to file specified at path (this file will be created/overwritten)
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="path">path to write to</param>
        /// <param name="decryptor">rsa decryptor to be used</param>
        /// <param name="aes">aes decryptor to be used</param>
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
    }

    internal static class WriteExtensions
    {
        /// <summary>
        /// Writes unencrypted command to stream
        /// </summary>
        /// <param name="stream">stream to write to</param>
        /// <param name="command">command to write</param>
        internal static void WriteCommand(this NetworkStream stream, byte[] command)
        {
            stream.Write(command, 0, 1);
        }

        /// <summary>
        /// Writes encrypted command to stream
        /// </summary>
        /// <param name="stream">stream to write to</param>
        /// <param name="command">command to write</param>
        /// <param name="encryptor">encryptor to be used</param>
        internal static void WriteCommand(this NetworkStream stream, byte[] command,
            ref RSACryptoServiceProvider encryptor)
        {
            byte[] enc = encryptor.Encrypt(command, true);
            stream.Write(enc, 0, enc.Length);
            
        }
        
        /// <summary>
        /// Writes command, data length and data as one unencrypted array to stream
        /// </summary>
        /// <param name="stream">stream to write to</param>
        /// <param name="command">command to write</param>
        /// <param name="data">data to write</param>
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
        /// Writes command, data length and data as one encrypted array to stream
        /// </summary>
        /// <param name="stream">stream to write to</param>
        /// <param name="command">command to write</param>
        /// <param name="data">data to write</param>
        /// <param name="encryptor">encryptor to be used</param>
        /// <exception cref="InvalidDataException">RSA with key length of 2048 has max data length of 190 bytes. 5 bytes are reserved for command and data length leaving us 185 bytes for data </exception>
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
        ///Writes command, IV, encrypted data length as one encrypted array to stream, afterwards writes long encrypted data to stream
        /// </summary>
        /// <param name="stream">stream to write to</param>
        /// <param name="command">command to write</param>
        /// <param name="data">long data to write</param>
        /// <param name="encryptor">rsa encryptor to be used</param>
        /// <param name="aes">aes encryptor to be used</param>
        internal static void WriteCommand(this NetworkStream stream, byte[] command, byte[] data,
            ref RSACryptoServiceProvider encryptor, ref Aes aes)
        {
            const int len = 1 + 16 + 8; //command(1), aes IV (16), encrypted data length as long(8)
            long encryptedDataLength = data.LongLength + (16 - data.LongLength % 16);
            byte[] rv = new byte[len];
            aes.GenerateIV();
            
            Buffer.BlockCopy(command, 0, rv, 0, 1);
            Buffer.BlockCopy(aes.IV, 0, rv, 1, 16);
            Buffer.BlockCopy(BitConverter.GetBytes(encryptedDataLength), 0, rv, 17, 4);
            rv = encryptor.Encrypt(rv, true);
            stream.Write(rv, 0, rv.Length);

            CryptoStream csEncrypt = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write, true);
            csEncrypt.WriteLongData(data);
            csEncrypt.Dispose();
        }
        
        /// <summary>
        /// Writes command, data length and data as one encrypted array to stream
        /// </summary>
        /// <param name="stream">stream to write to</param>
        /// <param name="data">data to write</param>
        /// <param name="encryptor">encryptor to be used</param>
        /// <exception cref="InvalidDataException">RSA with key length of 2048 has max data length of 190 bytes.</exception>
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
        /// Writes command, IV, encrypted data length as one encrypted array to stream, afterwards writes long encrypted data to stream
        /// </summary>
        /// <param name="stream">stream to write to</param>
        /// <param name="data">long data to write</param>
        /// <param name="encryptor">rsa encryptor to be used</param>
        /// <param name="aes">aes encryptor to be used</param>
        internal static void WriteData(this NetworkStream stream, byte[] data,
            ref RSACryptoServiceProvider encryptor, ref Aes aes)
        {
            const int len =  16 + 8; //aes IV (16), encrypted data length as long(8)
            long encryptedDataLength = data.LongLength + (16 - data.LongLength % 16);
            byte[] rv = new byte[len];
            aes.GenerateIV();
            
            Buffer.BlockCopy(aes.IV, 0, rv, 0, 16);
            Buffer.BlockCopy(BitConverter.GetBytes(encryptedDataLength), 0, rv, 16, 4);
            rv = encryptor.Encrypt(rv, true);
            stream.Write(rv, 0, rv.Length);

            CryptoStream csEncrypt = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write, true);
            csEncrypt.WriteLongData(data);
            csEncrypt.Dispose();
        }

        /// <summary>
        /// Writes long data to stream
        /// </summary>
        /// <param name="stream">stream to be written to</param>
        /// <param name="data">data to be written</param>
        private static void WriteLongData(this Stream stream, byte[] data)
        {
            long writeLength = data.LongLength;
            long totalWritten = 0;
            while (writeLength > 0)
            {
                int writeThisCycle = writeLength > int.MaxValue ? int.MaxValue : Convert.ToInt32(writeLength);
                writeLength -= writeThisCycle;
                totalWritten += writeThisCycle;
                byte[] toBeWritten = new byte[writeThisCycle];
                Array.Copy(data, totalWritten, toBeWritten, 0, writeThisCycle);
                stream.Write(toBeWritten, 0, writeThisCycle);
            }
        }
        
        /// <summary>
        /// Encrypts and writes file to stream
        /// </summary>
        /// <param name="stream">stream to be written to</param>
        /// <param name="path">path to file that's to be written to stream</param>
        /// <param name="encryptor">rsa encryptor to be used</param>
        /// <param name="aes">aes encryptor to be used</param>
        /// <param name="writeCommand">whether file command should be written</param>
        internal static void WriteFile(this NetworkStream stream, string path,
            ref RSACryptoServiceProvider encryptor, ref Aes aes, bool writeCommand = false)
        {
            const int lenWithCommand = 1 + 16 + 8; //command(1), aes IV (16), encrypted data length as long(8)
            const int lenWithOutCommand =  16 + 8; //aes IV (16), encrypted data length as long(8)
            FileInfo fi = new FileInfo(path);
            using FileStream fs = fi.Open(FileMode.Open);
            long encryptedDataLength = fi.Length + (16 - fi.Length % 16);
            byte[] rv = new byte[writeCommand ? lenWithCommand : lenWithOutCommand];
            aes.GenerateIV();

            if (writeCommand)
            {
                Buffer.BlockCopy(CommandsArr.FileSend, 0, rv, 0, 1);
            }
            Buffer.BlockCopy(aes.IV, 0, rv, writeCommand ? 1 : 0, 16);
            Buffer.BlockCopy(BitConverter.GetBytes(encryptedDataLength), 0, rv, writeCommand ? 17 : 16, 4);
            rv = encryptor.Encrypt(rv, true);
            stream.Write(rv, 0, rv.Length);

            long writeLength = fi.Length;
            CryptoStream csEncrypt = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write, true);
            while (writeLength > 0)
            {
                int writeThisCycle = writeLength > int.MaxValue ? int.MaxValue : Convert.ToInt32(writeLength);
                fs.CopyTo(csEncrypt, writeThisCycle);
                writeLength -= writeThisCycle;
            }
            csEncrypt.Dispose();
        }
    }
}