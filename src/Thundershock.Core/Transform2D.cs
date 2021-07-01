using System.Numerics;

namespace Thundershock.Core
{
    public sealed class Transform2D
    {
        public Vector2 Position = Vector2.Zero;
        public float Rotation;
        public Vector2 Scale = Vector2.One;

        public Matrix4x4 GetTransformMatrix()
        {
            return Matrix4x4.CreateTranslation(Position.X, Position.Y, 0)
                   * Matrix4x4.CreateScale(Scale.X, Scale.Y, 1) 
                   * Matrix4x4.CreateRotationZ(Rotation);
        }
    }

    public sealed class Transform
    {
        public Vector3 Position = Vector3.Zero;
        public Rotation Rotation;
        public Vector3 Scale = Vector3.One;

        public Matrix4x4 GetTransformMatrix()
        {
            return Matrix4x4.CreateTranslation(Position.X, Position.Y, Position.Z) *
                   Matrix4x4.CreateScale(Scale.X, Scale.Y, Scale.Z) *
                   Rotation.CreateMatrix();
        }

        public override string ToString()
        {
            return $"(Position = {Position}, Rotation = {Rotation}, Scale = {Scale})";
        }
    }

    public struct Rotation
    {
        public float Pitch;
        public float Yaw;
        public float Roll;

        public Matrix4x4 CreateMatrix()
        {
            var yaw = MathHelper.ToRadians(Yaw);
            var pitch = MathHelper.ToRadians(Pitch);
            var roll = MathHelper.ToRadians(Roll);

            return Matrix4x4.CreateFromYawPitchRoll(yaw, pitch, roll);
        }

        public override string ToString()
        {
            return $"(Pitch = {Pitch}, Yaw = {Yaw}, Roll = {Roll})";
        }
    }
}