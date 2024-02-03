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
            sentOnetimeSendFlag = songsToSend.Count > 0;
            this.songsToSend = songsToSend;
            oneTimeSendCount = songsToSend.Count;
        }

        internal readonly bool IsServer;
        internal EncryptionState encryptionState = EncryptionState.None;
        internal SyncRequestState syncRequestState = SyncRequestState.None;
        internal SongSendRequestState songSendRequestState = SongSendRequestState.None;
        internal string remoteHostname = string.Empty;
        internal int ackCount = 0;
        internal bool? isTrustedSyncTarget = null;
        internal int timeoutCounter = 0;
        
        /// <summary>
        /// Songs to sync
        /// </summary>
        private List<Song> syncSongs = new List<Song>();
        /// <summary>
        /// Songs to use on one time send
        /// </summary>
        internal readonly List<Song> songsToSend;
        /// <summary>
        /// Pairing of which album belongs to which artist
        /// </summary>
        internal readonly Dictionary<string, string> albumArtistPair = new Dictionary<string, string>();
        /// <summary>
        /// List of which artist images were requested
        /// </summary>
        internal readonly List<string> artistImageRequests = new List<string>();
        /// <summary>
        /// List of which album images were requested
        /// </summary>
        internal readonly List<string> albumImageRequests = new List<string>();
        
        /// <summary>
        /// Whether we initiated one time connection
        /// </summary>
        private bool sentOnetimeSendFlag;
        /// <summary>
        /// Whether remote side initiated one time connection
        /// </summary>
        internal bool gotOneTimeSendFlag = false;
        /// <summary>
        /// Whether attempt to fetch songs to be synced to this target was already made
        /// </summary>
        internal bool fetchedSyncSongs = false;

        /// <summary>
        /// number of songs to send when syncing
        /// </summary>
        internal int syncSendCount;
        /// <summary>
        /// number of songs already sent when syncing
        /// </summary>
        internal int syncSentCount = 0;
        /// <summary>
        /// number of songs to receive when syncing
        /// </summary>
        internal int syncReceiveCount = 0;
        /// <summary>
        /// number of songs already received when syncing
        /// </summary>
        internal int syncReceivedCount = 0;
        /// <summary>
        /// number of songs to receive one on one time connection
        /// </summary>
        internal int oneTimeReceiveCount = 0;
        /// <summary>
        /// number of songs already received on one time connection
        /// </summary>
        internal int oneTimeReceivedCount = 0;
        /// <summary>
        /// number of songs to send on one time connection
        /// </summary>
        internal int oneTimeSendCount;
        /// <summary>
        /// number of songs already sent on one time connection
        /// </summary>
        internal int oneTimeSentCount = 0;
        
        /// <summary>
        /// Whether we can receive files on current connection
        /// </summary>
        internal bool CanReceiveFiles => (isTrustedSyncTarget ?? false) ||
            (ConnectionType == ConnectionType.OneTimeReceive && StateHandler.OneTimeSendStates.TryGetValue(remoteHostname, out UserAcceptedState userAcceptedState) &&
             userAcceptedState == UserAcceptedState.SongsAccepted);

        /// <summary>
        /// Whether we can send files on current connection
        /// </summary>
        internal bool CanSendFiles =>
            ((isTrustedSyncTarget ?? false) && syncRequestState == SyncRequestState.Accepted) ||
            (ConnectionType == ConnectionType.OneTimeSend && songSendRequestState == SongSendRequestState.Accepted);
        
        /// <summary>
        /// Whether one time connection is established
        /// </summary>
        internal bool IsOneTimeConnection => ConnectionType is ConnectionType.OneTimeReceive or ConnectionType.OneTimeSend;

        /// <summary>
        /// Total number of songs to be transferred when syncing
        /// </summary>
        internal int TotalSyncCount => syncReceiveCount + SyncSongs.Count;
        
        /// <summary>
        /// Number of songs already transferred when syncing
        /// </summary>
        internal int SyncCount => syncReceivedCount + syncSentCount;

        /// <summary>
        /// Songs remaining to be sent on one time connection
        /// </summary>
        internal int OneTimeSendCountLeft => songsToSend.Count;
        
        /// <summary>
        /// Songs remaining to be sent when syncing
        /// </summary>
        internal int SyncSendCountLeft => syncSongs.Count;

        /// <summary>
        /// What type of connection is currently established
        /// </summary>
        internal ConnectionType ConnectionType
        {
            get
            {
                switch (isTrustedSyncTarget)
                {
                    case null:
                        return ConnectionType.None;
                    case true:
                        return ConnectionType.Sync;
                }

                if (gotOneTimeSendFlag)
                {
                    return ConnectionType.OneTimeReceive;
                }

                return sentOnetimeSendFlag ? ConnectionType.OneTimeSend : ConnectionType.None;
            }
        }

        /// <summary>
        /// Songs to sync
        /// </summary>
        internal List<Song> SyncSongs
        {
            get => syncSongs;
            set
            {
                syncSongs = value;
                syncSendCount = value.Count;
            }
        }

        /// <summary>
        /// Whether connection can be ended safely
        /// </summary>
        internal bool Ending =>
            ConnectionType != ConnectionType.None
            &&
            (
                CanSendFiles
                &&
                (
                    (
                        ConnectionType == ConnectionType.OneTimeSend
                        &&
                        OneTimeSendCountLeft == 0
                    )
                    ||
                    (
                        ConnectionType == ConnectionType.Sync
                        &&
                        SyncSendCountLeft == 0
                        &&
                        fetchedSyncSongs
                    )
                )
                &&
                ackCount >= 0
                &&
                (
                    (
                        ConnectionType == ConnectionType.Sync
                        &&
                        (
                            syncRequestState == SyncRequestState.Accepted
                            ||
                            syncRequestState == SyncRequestState.Rejected
                        )
                    )
                    ||
                    (
                        ConnectionType == ConnectionType.OneTimeSend
                        &&
                        (
                            songSendRequestState == SongSendRequestState.Accepted
                            ||
                            songSendRequestState == SongSendRequestState.Rejected
                        )
                    )
                )
                        
            )
            &&
            (
                CanReceiveFiles
                &&
                (
                    (
                        ConnectionType == ConnectionType.OneTimeReceive
                        &&
                        (
                            oneTimeReceiveCount == 0
                            ||
                            oneTimeReceivedCount >= oneTimeReceiveCount
                        )
                    )
                    ||
                    (
                        ConnectionType == ConnectionType.Sync
                        &&
                        (
                            syncReceiveCount == 0
                            ||
                            syncReceivedCount >= syncReceiveCount
                        )
                    )
                )
                &&
                artistImageRequests.Count == 0
                &&
                albumImageRequests.Count == 0
            );
    }
}