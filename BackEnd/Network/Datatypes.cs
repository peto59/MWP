using System;
using System.Collections.Generic;
using AngleSharp.Io;

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
        }

        internal bool IsServer;
        internal bool ending = false;
        internal EncryptionState encryptionState = EncryptionState.None;
        internal SyncRequestState syncRequestState = SyncRequestState.None;
        internal SongSendRequestState songSendRequestState = SongSendRequestState.None;
        internal string remoteHostname = string.Empty;
        internal int ackCount = 0;
        internal bool? isTrustedSyncTarget = null;
        internal int timeoutCounter = 0;
        
        internal List<Song> syncSongs = new List<Song>();
        internal List<Song> songsToSend;
        internal Dictionary<string, string> albumArtistPair = new Dictionary<string, string>();
        internal List<string> artistImageRequests = new List<string>();
        internal List<string> albumImageRequests = new List<string>();
        internal bool sendOnetimeSendFlag;
        internal bool gotOneTimeSendFlag = false;
    }
}