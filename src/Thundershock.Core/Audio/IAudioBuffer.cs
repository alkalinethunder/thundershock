using System;

namespace Thundershock.Core.Audio
{
    public interface IAudioBuffer : IDisposable
    {
        uint Id { get; }
        
        int Channels { get; }
        int SampleRate { get; }
        
        double Power { get; }
        
        ReadOnlySpan<byte> Buffer { get; }

        void SetBuffer(ReadOnlySpan<byte> buffer);
    }
}