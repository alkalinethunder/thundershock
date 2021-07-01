using System.Numerics;

namespace Thundershock.Core.Rendering
{
    public abstract class EffectParameter
    {
        public abstract string Name { get; }

        public abstract void SetValue(int value);
        public abstract void SetValue(float value);
        public abstract void SetValue(double value);
        public abstract void SetValue(uint value);
        public abstract void SetValue(Vector2 value);
        public abstract void SetValue(Vector3 value);
        public abstract void SetValue(Vector4 value);
        public abstract void SetValue(Matrix4x4 value);

        public abstract byte GetValueByte();
        public abstract int GetValueInt32();
        public abstract uint GetValueUInt32();
        public abstract float GetValueFloat();
        public abstract double GetValueDouble();
        public abstract Vector2 GetVector2();
        public abstract Vector3 GetVector3();
        public abstract Vector4 GetVector4();
        public abstract Matrix4x4 GetMatric4x4();

        public abstract void SetValue(float[] array);
        public abstract void SetValue(Vector2[] array);
    }
}