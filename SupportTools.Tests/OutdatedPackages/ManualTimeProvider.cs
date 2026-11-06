using System;

namespace SupportTools.Tests.OutdatedPackages;

//a clock that moves only when a test moves it; a timestamp is a TimeSpan tick
internal sealed class ManualTimeProvider : TimeProvider
{
    private long _timestamp;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp()
    {
        return _timestamp;
    }

    public void Advance(TimeSpan time)
    {
        _timestamp += time.Ticks;
    }
}
