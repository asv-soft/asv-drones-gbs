using Asv.Cfg;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Asv.Drones.Gbs;

public static class AppHostMixin
{
    public static IHostApplicationBuilder AddDefaultLogging(this IHostApplicationBuilder builder)
    {
        builder
            .Logging.ClearProviders()
            .SetMinimumLevel(LogLevel.Information)
            .AddZLoggerRollingFile((dt, index) => $"logs/{dt:yyyy-MM-dd}_{index}.logs", 1024 * 1024)
            .AddZLoggerConsole(options =>
            {
                options.IncludeScopes = true;
                options.UsePlainTextFormatter(formatter =>
                {
                    formatter.SetPrefixFormatter(
                        $"{0:HH:mm:ss.fff} | ={1:short}= | {2, -40} ",
                        (in MessageTemplate template, in LogInfo info) =>
                            template.Format(info.Timestamp, info.LogLevel, info.Category)
                    );
                    formatter.SetExceptionFormatter(
                        (writer, ex) =>
                            Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}")
                    );
                });
            });
        return builder;
    }

    public static IHostApplicationBuilder AddSystemTimeProvider(
        this IHostApplicationBuilder builder
    )
    {
        builder.Services.AddSingleton(TimeProvider.System);
        return builder;
    }

    public static IHostApplicationBuilder AddUserConfig(
        this IHostApplicationBuilder builder,
        string fileName
    )
    {
        builder.Services.AddSingleton<IConfiguration>(provider =>
        {
            var logger = provider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<JsonOneFileConfiguration>();
            return new JsonOneFileConfiguration(
                fileName,
                true,
                TimeSpan.FromMilliseconds(500),
                true,
                logger
            );
        });
        return builder;
    }
}
