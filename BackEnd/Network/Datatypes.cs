using System;
using System.Collections.Generic;

namespace MWP.BackEnd.Network
{
    internal class P2PState
    {
        internal bool IsValid;
        private readonly byte[] value;
        internal P2PState(byte[] data)
        {
            value = data;
            IsValid = data.Length == 4 && Type != P2PStateTypes.None;
        }
        
        internal P2PStateTypes Type {
            get
            {
                if (value[0] == byte.MaxValue && value[1] == byte.MaxValue && value[2] == byte.MaxValue)
                {
                    return P2PStateTypes.Request;
                }
                if (value[0] == byte.MaxValue && value[1] == byte.MaxValue)
                {
                    return P2PStateTypes.State;
                }
                if (Port is >= 1024 and <= 65535)
                {
                    return P2PStateTypes.Port;
                }

                return P2PStateTypes.None;
            }
        }

        internal byte Cnt => value[3];
        internal byte State => value[2];
        internal int Port => BitConverter.ToInt32(value);

        internal static byte[] Request(byte cnt)
        {
            return new[] { byte.MaxValue, byte.MaxValue, byte.MaxValue, cnt };
        }
        
        
        internal static byte[] Send(byte cnt, byte state)
        {
            return new[] { byte.MaxValue, byte.MaxValue, state, cnt };
        }
    }

    internal enum P2PStateTypes : byte
    {
        State,
        Port,
        Request,
        None
    }

    internal class ConnectionState
    {
        internal ConnectionState(bool isServer, List<Song> songsToSend)
        {
            IsServer = isServer;
            sendOnetimeSendFlag = songsToSend.Count > 0;
            this.songsToSend = songsToSend;
            oneTimeSendCount = songsToSend.Count;
        }

        internal readonly bool IsServer;
        internal bool ending = false;
        internal EncryptionState encryptionState = EncryptionState.None;
        internal SyncRequestState syncRequestState = SyncRequestState.None;
        internal SongSendRequestState songSendRequestState = SongSendRequestState.None;
        internal string remoteHostname = string.Empty;
        internal int ackCount = 0;
        internal bool? isTrustedSyncTarget = null;
        internal int timeoutCounter = 0;
        
        private List<Song> syncSongs = new List<Song>();
        internal readonly List<Song> songsToSend;
        internal readonly Dictionary<string, string> albumArtistPair = new Dictionary<string, string>();
        internal readonly List<string> artistImageRequests = new List<string>();
        internal readonly List<string> albumImageRequests = new List<string>();
        internal bool sendOnetimeSendFlag;
        internal bool gotOneTimeSendFlag = false;

        internal int syncSendCount = 0;
        internal int syncSentCount = 0;
        internal int syncReceiveCount = 0;
        internal int syncReceivedCount = 0;
        internal int oneTimeReceiveCount = 0;
        internal int oneTimeReceivedCount = 0;
        internal int oneTimeSendCount;
        internal int oneTimeSentCount = 0;

        internal bool CanReceiveFiles => (isTrustedSyncTarget ?? false) ||
            (StateHandler.OneTimeSendStates.TryGetValue(remoteHostname, out UserAcceptedState userAcceptedState) &&
             userAcceptedState == UserAcceptedState.SongsAccepted);

        internal bool CanSendFiles =>
            ((isTrustedSyncTarget ?? false) && syncRequestState == SyncRequestState.Accepted) ||
            (sendOnetimeSendFlag && songSendRequestState == SongSendRequestState.Accepted);

        internal int TotalSyncCount => syncReceiveCount + SyncSongs.Count;
        internal int SyncCount => syncReceivedCount + syncSentCount;

        internal int OneTimeSendCountLeft => songsToSend.Count;

        internal List<Song> SyncSongs
        {
            get => syncSongs;
            set
            {
                syncSongs = value;
                syncSendCount = value.Count;
            }
        }
        
    }
}