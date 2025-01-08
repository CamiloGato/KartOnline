using System;
using Unity.Netcode;
using UnityEngine;

namespace CarController.Payload
{
    
    public struct InputPayload : INetworkSerializable, IEquatable<InputPayload>
    {
        [Serializable]
        public struct InputData : INetworkSerializable, IEquatable<InputData>
        {
            public bool forward;
            public bool backward;
            public bool left;
            public bool right;
            public bool brake;
            public bool respawn;
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref forward);
                serializer.SerializeValue(ref backward);
                serializer.SerializeValue(ref left);
                serializer.SerializeValue(ref right);
                serializer.SerializeValue(ref brake);
                serializer.SerializeValue(ref respawn);
            }

            public bool Equals(InputData other)
            {
                return forward == other.forward && backward == other.backward && left == other.left && right == other.right && brake == other.brake && respawn == other.respawn;
            }

            public override bool Equals(object obj)
            {
                return obj is InputData other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(forward, backward, left, right, brake, respawn);
            }

            public static bool operator ==(InputData left, InputData right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(InputData left, InputData right)
            {
                return !left.Equals(right);
            }
        }
        
        public int Tick;
        public DateTime Timestamp;
        public ulong NetworkObjectId;
        public InputData Input;
        public Vector3 Position;
    
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref Timestamp);
            serializer.SerializeValue(ref NetworkObjectId);
            serializer.SerializeValue(ref Input);
            serializer.SerializeValue(ref Position);
        }
        
        public bool Equals(InputPayload other)
        {
            return Tick == other.Tick && Timestamp.Equals(other.Timestamp) && NetworkObjectId == other.NetworkObjectId && Input.Equals(other.Input) && Position.Equals(other.Position);
        }

        public override bool Equals(object obj)
        {
            return obj is InputPayload other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Tick, Timestamp, NetworkObjectId, Input, Position);
        }

        public static bool operator ==(InputPayload left, InputPayload right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(InputPayload left, InputPayload right)
        {
            return !left.Equals(right);
        }
    }
}