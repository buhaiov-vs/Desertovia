namespace Desertod.Api.Tests;

/// <summary>
/// Real time plus an offset tests move forward, so a cycle or a raid that takes minutes is over at once.
/// Timers still tick in real time: the background workers keep polling as in production.
/// </summary>
public sealed class TestClock : TimeProvider
{
    private long offsetTicks;

    public override DateTimeOffset GetUtcNow() => System.GetUtcNow() + TimeSpan.FromTicks(Interlocked.Read(ref offsetTicks));

    /// <summary>Only forward: every test in the collection shares the clock, and time never runs back in the game.</summary>
    public void Advance(TimeSpan by)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(by, TimeSpan.Zero);
        Interlocked.Add(ref offsetTicks, by.Ticks);
    }
}
