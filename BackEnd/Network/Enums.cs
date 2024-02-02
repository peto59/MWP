namespace MWP.BackEnd.Network
{
    internal enum EncryptionState : byte
    {
        None,
        RsaExchange,
        AesSend,
        AesReceived,
        Encrypted,
    }
    
    internal enum SyncRequestState : byte
    {
        None,
        Sent,
        Rejected,
        Accepted
    }

    internal enum SongSendRequestState : byte
    {
        None,
        Sent,
        Rejected,
        Accepted
    }

    internal enum UserAcceptedState : byte
    {
        None,
        Showed,
        Cancelled,
        ConnectionAccepted,
        SongsShowed,
        SongsAccepted,
    }

    internal enum NotificationTypes : byte
    {
        OneTimeSend,
        OneTimeReceive,
        Sync
    }
}