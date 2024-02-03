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
    
    internal enum P2PStateTypes : byte
    {
        State,
        Port,
        Request,
        None
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

    internal enum ConnectionType : byte
    {
        None,
        OneTimeSend,
        OneTimeReceive,
        Sync
    }
}