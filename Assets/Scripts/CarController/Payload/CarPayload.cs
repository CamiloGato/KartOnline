using System;
using Unity.Netcode;
using UnityEngine;

namespace CarController.Payload
{
    public struct StatePayload : INetworkSerializable, IEquatable<StatePayload>
    {
        public int Tick;
        public ulong NetworkObjectId;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
    
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref NetworkObjectId);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref AngularVelocity);
        }

        public bool Equals(StatePayload other)
        {
            return Tick == other.Tick && NetworkObjectId == other.NetworkObjectId && Position.Equals(other.Position) && Rotation.Equals(other.Rotation) && Velocity.Equals(other.Velocity) && AngularVelocity.Equals(other.AngularVelocity);
        }

        public override bool Equals(object obj)
        {
            return obj is StatePayload other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Tick, NetworkObjectId, Position, Rotation, Velocity, AngularVelocity);
        }

        public static bool operator ==(StatePayload left, StatePayload right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StatePayload left, StatePayload right)
        {
            return !left.Equals(right);
        }
    }
}