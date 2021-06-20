using System.Numerics;

namespace Thundershock.Core
{
    public sealed class Transform2D
    {
        public Vector2 Position { get; set; } = Vector2.Zero;
        public float Rotation { get; set; }
        public Vector2 Scale { get; set; } = Vector2.One;

        public Matrix4x4 GetTransformMatrix()
        {
            return Matrix4x4.CreateTranslation(Position.X, Position.Y, 0)
                   * Matrix4x4.CreateScale(Scale.X, Scale.Y, 1) 
                   * Matrix4x4.CreateRotationZ(Rotation);
        }
    }

    public sealed class Transform
    {
        public Vector3 Position { get; set; }
        public Rotation Rotation { get; set; }
        public Vector3 Scale { get; set; } = Vector3.One;

        public Matrix4x4 GetTransformMatrix()
        {
            return Matrix4x4.CreateTranslation(Position.X, Position.Y, Position.Z) *
                   Matrix4x4.CreateScale(Scale.X, Scale.Y, Scale.Z) *
                   Matrix4x4.CreateFromYawPitchRoll(Rotation.Yaw, Rotation.Pitch, Rotation.Roll);
        }
    }

    public struct Rotation
    {
        public float Pitch;
        public float Yaw;
        public float Roll;
    }
}