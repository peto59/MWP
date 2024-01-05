using System;
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
}