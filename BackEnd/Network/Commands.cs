using System.Linq;

namespace Ass_Pain.BackEnd.Network
{
    internal static class Commands
    {
        internal const byte None = (byte)CommandsEnum.None;
        internal const byte Wait = (byte)CommandsEnum.Wait;
        internal const byte Host = (byte)CommandsEnum.Host;
        internal const byte RsaExchange = (byte)CommandsEnum.RsaExchange;
        internal const byte AesSend = (byte)CommandsEnum.AesSend;
        internal const byte AesReceived = (byte)CommandsEnum.AesReceived;
        internal const byte SyncRequest = (byte)CommandsEnum.SyncRequest;
        internal const byte SyncAccepted = (byte)CommandsEnum.SyncAccepted;
        internal const byte SyncInfoRequest = (byte)CommandsEnum.SyncInfoRequest;
        internal const byte SyncRejected = (byte)CommandsEnum.SyncRejected;
        internal const byte SyncInfo = (byte)CommandsEnum.SyncInfo;
        internal const byte SongSend = (byte)CommandsEnum.SongSend;
        internal const byte ImageSend = (byte)CommandsEnum.ImageSend;
        internal const byte ArtistImageRequest = (byte)CommandsEnum.ArtistImageRequest;
        internal const byte AlbumImageRequest = (byte)CommandsEnum.AlbumImageRequest;
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
            return Commands.IsLong(command[0]);
        }
        internal static bool IsFileCommand(byte[] command)
        {
            return Commands.IsFileCommand(command[0]);
        }
        internal static bool IsEncryptedOnlyCommand(byte[] command)
        {
            return Commands.IsEncryptedOnlyCommand(command[0]);
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
        internal static readonly byte[] Wait = { Commands.Wait };
        internal static readonly byte[] Host = { Commands.Host };
        internal static readonly byte[] RsaExchange = { Commands.RsaExchange };
        internal static readonly byte[] AesSend = { Commands.AesSend };
        internal static readonly byte[] AesReceived = { Commands.AesReceived };
        internal static readonly byte[] SyncRequest = { Commands.SyncRequest };
        internal static readonly byte[] SyncAccepted = { Commands.SyncAccepted };
        internal static readonly byte[] SyncInfoRequest = { Commands.SyncInfoRequest };
        internal static readonly byte[] SyncRejected = { Commands.SyncRejected };
        internal static readonly byte[] SyncInfo = { Commands.SyncInfo };
        internal static readonly byte[] SongSend = { Commands.SongSend };
        internal static readonly byte[] ImageSend = { Commands.ImageSend };
        internal static readonly byte[] ArtistImageRequest = { Commands.ArtistImageRequest };
        internal static readonly byte[] AlbumImageRequest = { Commands.AlbumImageRequest };
        internal static readonly byte[] End = { Commands.End };
        internal static readonly byte[] LongCommands = { Commands.SyncInfo, Commands.SongSend, Commands.ImageSend };
        internal static readonly CommandsEnum[] LongCommandsEnum = { CommandsEnum.SyncInfo, CommandsEnum.SongSend, CommandsEnum.ImageSend };
        internal static readonly byte[] FileCommands = { Commands.SongSend, Commands.ImageSend };
        internal static readonly CommandsEnum[] FileCommandsEnum = { CommandsEnum.SongSend, CommandsEnum.ImageSend };
        internal static readonly byte[] EncryptedOnlyCommands =
        {
            Commands.SyncRequest, Commands.SyncAccepted, Commands.SyncInfoRequest,
            Commands.SyncRejected, Commands.SyncInfo, Commands.SongSend, Commands.ImageSend,
            Commands.ArtistImageRequest, Commands.AlbumImageRequest
        };
        internal static readonly CommandsEnum[] EncryptedOnlyCommandsEnums =
        {
            CommandsEnum.SyncRequest, CommandsEnum.SyncAccepted, CommandsEnum.SyncInfoRequest,
            CommandsEnum.SyncRejected, CommandsEnum.SyncInfo, CommandsEnum.SongSend, CommandsEnum.ImageSend,
            CommandsEnum.ArtistImageRequest, CommandsEnum.AlbumImageRequest
        };
    }

    internal enum CommandsEnum : byte
    {
        None = 0,
        Wait = 1,
        Host = 10,
        RsaExchange = 11,
        AesSend = 12,
        AesReceived = 13,
        SyncRequest = 20,
        SyncAccepted = 21,
        SyncInfoRequest = 22,
        SyncRejected = 23,
        SyncInfo = 24,
        SongSend = 30,
        ImageSend = 31,
        ArtistImageRequest = 32,
        AlbumImageRequest = 33,
        End = 255
    }

    internal enum EncryptionState : byte
    {
        None,
        RsaExchange,
        AesSend,
        AesReceived,
        Encrypted,
    }
}