using Asv.Hal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Asv.Drones.Gbs.Led;

public class LedServiceOptions
{
    public const string Section = "Led";
    public bool IsEnabled { get; set; } = false;
    public GpioRgbLedConfig Gpio { get; set; } = new();
    public int DefaultTickDurationMs { get; set; } = 100;
}

public class LedService : ILedService, IDisposable, IAsyncDisposable
{
    private readonly TimeProvider _timeProvider;
    private readonly IRgbLed _led;
    private readonly TimeSpan _defaultTickDurationMs;
    private readonly ILogger<LedAnimation> _animationLogger;
    private LedAnimation? _blink;
    private readonly ILogger<LedService> _logger;

    public LedService(
        IOptions<LedServiceOptions> options,
        ILoggerFactory loggerFactory,
        IGpioProvider gpioService,
        TimeProvider timeProvider
    )
    {
        _logger = loggerFactory.CreateLogger<LedService>();
        _timeProvider = timeProvider;
        _animationLogger = loggerFactory.CreateLogger<LedAnimation>();
        _led = options.Value.IsEnabled
            ? new GpioRgbLed(
                options.Value.Gpio,
                gpioService,
                loggerFactory.CreateLogger<GpioRgbLed>()
            )
            : NullLed.Instance;
        _defaultTickDurationMs = TimeSpan.FromMilliseconds(options.Value.DefaultTickDurationMs);
    }

    public IDisposable LedAnimation(string record, TimeSpan? tickDuration = null)
    {
        _blink?.Dispose();
        return _blink = new LedAnimation(
            _led,
            _timeProvider,
            tickDuration ?? _defaultTickDurationMs,
            record,
            _animationLogger
        );
    }

    public void Dispose()
    {
        _blink?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_blink is IAsyncDisposable blinkAsyncDisposable)
        {
            await blinkAsyncDisposable.DisposeAsync();
        }
        else
        {
            _blink?.Dispose();
        }
    }
}
