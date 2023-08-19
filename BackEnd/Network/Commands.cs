namespace Ass_Pain.BackEnd.Network
{
    internal static class Commands
    {
        internal const byte None = (byte)CommandsEnum.None;
        internal const byte Host = (byte)CommandsEnum.Host;
        internal const byte RsaExchange = (byte)CommandsEnum.RsaExchange;
        internal const byte AesSend = (byte)CommandsEnum.AesSend;
        internal const byte AesReceived = (byte)CommandsEnum.AesReceived;
        internal const byte SyncRequest = (byte)CommandsEnum.SyncRequest;
        internal const byte SyncAccepted = (byte)CommandsEnum.SyncAccepted;
        internal const byte SyncInfoRequest = (byte)CommandsEnum.SyncInfoRequest;
        internal const byte SyncRejected = (byte)CommandsEnum.SyncRejected;
        internal const byte SyncInfo = (byte)CommandsEnum.SyncInfo;
        internal const byte FileSend = (byte)CommandsEnum.FileSend;
        internal const byte End = (byte)CommandsEnum.End;
    }

    internal static class CommandsArr
    {
        internal static readonly byte[] None = { Commands.None };
        internal static readonly byte[] Host = { Commands.Host };
        internal static readonly byte[] RsaExchange = { Commands.RsaExchange };
        internal static readonly byte[] AesSend = { Commands.AesSend };
        internal static readonly byte[] AesReceived = { Commands.AesReceived };
        internal static readonly byte[] SyncRequest = { Commands.SyncRequest };
        internal static readonly byte[] SyncAccepted = { Commands.SyncAccepted };
        internal static readonly byte[] SyncInfoRequest = { Commands.SyncInfoRequest };
        internal static readonly byte[] SyncRejected = { Commands.SyncRejected };
        internal static readonly byte[] SyncInfo = { Commands.SyncInfo };
        internal static readonly byte[] FileSend = { Commands.FileSend };
        internal static readonly byte[] End = { Commands.End };
        internal static readonly byte[] LongCommands = { Commands.SyncInfo, Commands.FileSend };
    }

    internal enum CommandsEnum : byte
    {
        None = 0,
        Host = 10,
        RsaExchange = 11,
        AesSend = 12,
        AesReceived = 13,
        SyncRequest = 20,
        SyncAccepted = 21,
        SyncInfoRequest = 22,
        SyncRejected = 23,
        SyncInfo = 24,
        FileSend = 30,
        End = 100
    }
}