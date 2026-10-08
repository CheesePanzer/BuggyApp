using BuggyApp.Configuration;

namespace BuggyApp.Services;

public sealed class MockDelayProvider : IMockDelayProvider
{
    private readonly Lock _sync = new();
    private readonly Random _random;
    private readonly MockServerOptions _options;

    public MockDelayProvider(MockServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.MinimumDelayMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Minimum delay cannot be negative.");
        }

        if (options.MaximumDelayMilliseconds < options.MinimumDelayMilliseconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Maximum delay cannot be less than minimum delay.");
        }

        _options = options;
        _random = new Random(options.RandomSeed);
    }

    public Task DelayAsync(CancellationToken cancellationToken = default)
    {
        int delayMilliseconds;

        lock (_sync)
        {
            delayMilliseconds = _random.Next(
                _options.MinimumDelayMilliseconds,
                _options.MaximumDelayMilliseconds + 1);
        }

        return Task.Delay(delayMilliseconds, cancellationToken);
    }
}
