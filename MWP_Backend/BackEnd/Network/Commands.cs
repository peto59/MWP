using System.Linq;

namespace MWP.BackEnd.Network
{
    internal static class Commands
    {
        internal const byte None = (byte)CommandsEnum.None;
        internal const byte Wait = (byte)CommandsEnum.Wait;
        internal const byte OnetimeSend = (byte)CommandsEnum.OnetimeSend;
        internal const byte ConnectionAccepted = (byte)CommandsEnum.ConnectionAccepted;
        internal const byte ConnectionRejected = (byte)CommandsEnum.ConnectionRejected;
        internal const byte Host = (byte)CommandsEnum.Host;
        internal const byte RsaExchange = (byte)CommandsEnum.RsaExchange;
        internal const byte AesSend = (byte)CommandsEnum.AesSend;
        internal const byte AesReceived = (byte)CommandsEnum.AesReceived;
        internal const byte SyncRequest = (byte)CommandsEnum.SyncRequest;
        internal const byte SyncAccepted = (byte)CommandsEnum.SyncAccepted;
        internal const byte SyncRejected = (byte)CommandsEnum.SyncRejected;
        internal const byte SyncCount = (byte)CommandsEnum.SyncCount;
        internal const byte SongSendRequest = (byte)CommandsEnum.SongSendRequest;
        internal const byte SongRequestInfoRequest = (byte)CommandsEnum.SongRequestInfoRequest;
        internal const byte SongRequestInfo = (byte)CommandsEnum.SongRequestInfo;
        internal const byte SongRequestAccepted = (byte)CommandsEnum.SongRequestAccepted;
        internal const byte SongRequestRejected = (byte)CommandsEnum.SongRequestRejected;
        internal const byte SongSend = (byte)CommandsEnum.SongSend;
        internal const byte ArtistImageSend = (byte)CommandsEnum.ArtistImageSend;
        internal const byte AlbumImageSend = (byte)CommandsEnum.AlbumImageSend;
        internal const byte ArtistImageRequest = (byte)CommandsEnum.ArtistImageRequest;
        internal const byte AlbumImageRequest = (byte)CommandsEnum.AlbumImageRequest;
        internal const byte ArtistImageNotFound = (byte)CommandsEnum.ArtistImageNotFound;
        internal const byte AlbumImageNotFound = (byte)CommandsEnum.AlbumImageNotFound;
        internal const byte Ack = (byte)CommandsEnum.Ack;
        internal const byte End = (byte)CommandsEnum.End;
        internal static bool IsLong(byte command)
        {
            return CommandsArr.LongCommands.Contains(command);
        }
        internal static bool IsFileCommand(byte command)
        {
            return CommandsArr.FileCommands.Contains(command);
        }
        internal static bool IsEncryptedOnlyCommand(byte command)
        {
            return CommandsArr.EncryptedOnlyCommands.Contains(command);
        }
        internal static bool IsLong(byte[] command)
        {
            return IsLong(command[0]);
        }
        internal static bool IsFileCommand(byte[] command)
        {
            return IsFileCommand(command[0]);
        }
        internal static bool IsEncryptedOnlyCommand(byte[] command)
        {
            return IsEncryptedOnlyCommand(command[0]);
        }
        internal static bool IsLong(CommandsEnum command)
        {
            return CommandsArr.LongCommandsEnum.Contains(command);
        }
        internal static bool IsFileCommand(CommandsEnum command)
        {
            return CommandsArr.FileCommandsEnum.Contains(command);
        }
        internal static bool IsEncryptedOnlyCommand(CommandsEnum command)
        {
            return CommandsArr.EncryptedOnlyCommandsEnums.Contains(command);
        }
    }

    internal static class CommandsArr
    {
        internal static readonly byte[] None = { Commands.None };
        internal static readonly byte[] OnetimeSend = { Commands.OnetimeSend };
        internal static readonly byte[] ConnectionAccepted = { Commands.ConnectionAccepted };
        internal static readonly byte[] ConnectionRejected = { Commands.ConnectionRejected };
        internal static readonly byte[] Wait = { Commands.Wait };
        internal static readonly byte[] Host = { Commands.Host };
        internal static readonly byte[] RsaExchange = { Commands.RsaExchange };
        internal static readonly byte[] AesSend = { Commands.AesSend };
        internal static readonly byte[] AesReceived = { Commands.AesReceived };
        internal static readonly byte[] SyncRequest = { Commands.SyncRequest };
        internal static readonly byte[] SyncAccepted = { Commands.SyncAccepted };
        internal static readonly byte[] SyncRejected = { Commands.SyncRejected };
        internal static readonly byte[] SyncCount = { Commands.SyncCount };
        internal static readonly byte[] SongSendRequest = { Commands.SongSendRequest };
        internal static readonly byte[] SongRequestInfoRequest = { Commands.SongRequestInfoRequest };
        internal static readonly byte[] SongRequestInfo = { Commands.SongRequestInfo };
        internal static readonly byte[] SongRequestAccepted = { Commands.SongRequestAccepted };
        internal static readonly byte[] SongRequestRejected = { Commands.SongRequestRejected };
        internal static readonly byte[] SongSend = { Commands.SongSend };
        internal static readonly byte[] ArtistImageSend = { Commands.ArtistImageSend };
        internal static readonly byte[] AlbumImageSend = { Commands.AlbumImageSend };
        internal static readonly byte[] ArtistImageRequest = { Commands.ArtistImageRequest };
        internal static readonly byte[] AlbumImageRequest = { Commands.AlbumImageRequest };
        internal static readonly byte[] ArtistImageNotFound = { Commands.ArtistImageNotFound };
        internal static readonly byte[] AlbumImageNotFound = { Commands.AlbumImageNotFound };
        internal static readonly byte[] Ack = { Commands.Ack };
        internal static readonly byte[] End = { Commands.End };
        internal static readonly byte[] LongCommands =
        {
            Commands.SongRequestInfo,
            Commands.SongSend,
            Commands.ArtistImageSend,
            Commands.AlbumImageSend
        };
        internal static readonly CommandsEnum[] LongCommandsEnum =
        {
            CommandsEnum.SongRequestInfo,
            CommandsEnum.SongSend,
            CommandsEnum.ArtistImageSend,
            CommandsEnum.AlbumImageSend
        };
        internal static readonly byte[] FileCommands =
        {
            Commands.SongSend,
            Commands.ArtistImageSend,
            Commands.AlbumImageSend
        };
        internal static readonly CommandsEnum[] FileCommandsEnum =
        {
            CommandsEnum.SongSend,
            CommandsEnum.ArtistImageSend,
            CommandsEnum.AlbumImageSend
        };
        internal static readonly byte[] EncryptedOnlyCommands =
        {
            Commands.SyncRequest,
            Commands.SyncAccepted,
            Commands.SyncRejected,
            Commands.SyncCount,
            Commands.SongSendRequest,
            Commands.SongRequestInfoRequest,
            Commands.SongRequestInfo,
            Commands.SongRequestAccepted,
            Commands.SongRequestRejected,
            Commands.SongSend,
            Commands.ArtistImageSend,
            Commands.AlbumImageSend,
            Commands.ArtistImageRequest,
            Commands.AlbumImageRequest,
            Commands.ArtistImageNotFound,
            Commands.AlbumImageNotFound
        };
        internal static readonly CommandsEnum[] EncryptedOnlyCommandsEnums =
        {
            CommandsEnum.SyncRequest,
            CommandsEnum.SyncAccepted,
            CommandsEnum.SyncRejected,
            CommandsEnum.SyncCount,
            CommandsEnum.SongSendRequest,
            CommandsEnum.SongRequestInfoRequest,
            CommandsEnum.SongRequestInfo,
            CommandsEnum.SongRequestAccepted,
            CommandsEnum.SongRequestRejected,
            CommandsEnum.SongSend,
            CommandsEnum.ArtistImageSend,
            CommandsEnum.AlbumImageSend,
            CommandsEnum.ArtistImageRequest,
            CommandsEnum.AlbumImageRequest,
            CommandsEnum.ArtistImageNotFound,
            CommandsEnum.AlbumImageNotFound
        };
    }

    internal enum CommandsEnum : byte
    {
        None = 0,
        Wait = 1,
        OnetimeSend = 5,
        ConnectionAccepted = 7,
        ConnectionRejected = 8,
        Host = 10,
        RsaExchange = 11,
        AesSend = 12,
        AesReceived = 13,
        SyncRequest = 20,
        SyncAccepted = 21,
        SyncRejected = 22,
        SyncCount = 23,
        SongSendRequest = 30,
        SongRequestInfoRequest = 31,
        SongRequestInfo = 32,
        SongRequestAccepted = 33,
        SongRequestRejected = 34,
        SongSend = 40,
        ArtistImageSend = 41,
        AlbumImageSend = 42,
        ArtistImageRequest = 43,
        AlbumImageRequest = 44,
        ArtistImageNotFound = 45,
        AlbumImageNotFound = 46,
        Ack = 254,
        End = 255
    }
}