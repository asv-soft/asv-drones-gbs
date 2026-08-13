using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Asv.Drones.Gbs.Tests;

public class MavlinkServerOptionsBindingTests
{
    [Fact]
    public void Bind_ShouldPopulateNestedMavlinkSections()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Mavlink:Connections:0"] = "tcp://127.0.0.1:9000",
                    ["Mavlink:Heartbeat:HeartbeatRateMs"] = "1234",
                    ["Mavlink:StatusText:MaxQueueSize"] = "321",
                    ["Mavlink:StatusText:MaxSendRateHz"] = "7",
                    ["Mavlink:Diagnostic:MaxSendIntervalMs"] = "222",
                    ["Mavlink:Diagnostic:IsEnabled"] = "false",
                    ["Mavlink:Params:SendingParamItemDelayMs"] = "44",
                    ["Mavlink:Params:CfgPrefix"] = "TEST_CFG",
                }
            )
            .Build();

        var options = Bind(configuration);

        Assert.Equal(["tcp://127.0.0.1:9000"], options.Connections);
        Assert.Equal(1234, options.Heartbeat.HeartbeatRateMs);
        Assert.Equal(321, options.StatusText.MaxQueueSize);
        Assert.Equal(7, options.StatusText.MaxSendRateHz);
        Assert.Equal(222, options.Diagnostic.MaxSendIntervalMs);
        Assert.False(options.Diagnostic.IsEnabled);
        Assert.Equal(44, options.Params.SendingParamItemDelayMs);
        Assert.Equal("TEST_CFG", options.Params.CfgPrefix);
    }

    [Fact]
    public void Bind_ShouldIgnoreTopLevelMicroserviceSections()
    {
        var defaults = new MavlinkServerOptions();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Heartbeat:HeartbeatRateMs"] = "1234",
                    ["StatusText:MaxQueueSize"] = "321",
                    ["StatusText:MaxSendRateHz"] = "7",
                    ["Diagnostic:MaxSendIntervalMs"] = "222",
                    ["Diagnostic:IsEnabled"] = "false",
                    ["Params:SendingParamItemDelayMs"] = "44",
                    ["Params:CfgPrefix"] = "TEST_CFG",
                }
            )
            .Build();

        var options = Bind(configuration);

        Assert.Equal(defaults.Heartbeat.HeartbeatRateMs, options.Heartbeat.HeartbeatRateMs);
        Assert.Equal(defaults.StatusText.MaxQueueSize, options.StatusText.MaxQueueSize);
        Assert.Equal(defaults.StatusText.MaxSendRateHz, options.StatusText.MaxSendRateHz);
        Assert.Equal(defaults.Diagnostic.MaxSendIntervalMs, options.Diagnostic.MaxSendIntervalMs);
        Assert.Equal(defaults.Diagnostic.IsEnabled, options.Diagnostic.IsEnabled);
        Assert.Equal(
            defaults.Params.SendingParamItemDelayMs,
            options.Params.SendingParamItemDelayMs
        );
        Assert.Equal(defaults.Params.CfgPrefix, options.Params.CfgPrefix);
    }

    [Fact]
    public void CommittedBaseConfiguration_ShouldKeepTopLevelSectionsOutsideMavlinkOptions()
    {
        var path = RepositoryPaths.Get("src", "Asv.Drones.Gbs", "appsettings.json");
        var configuration = new ConfigurationBuilder().AddJsonFile(path).Build();
        var defaults = new MavlinkServerOptions();

        var options = Bind(configuration);

        Assert.Equal("1000", configuration["Heartbeat:HeartbeatRateMs"]);
        Assert.Null(configuration["Mavlink:Heartbeat:HeartbeatRateMs"]);
        Assert.Equal(defaults.Heartbeat.HeartbeatRateMs, options.Heartbeat.HeartbeatRateMs);
        Assert.Equal(defaults.StatusText.MaxQueueSize, options.StatusText.MaxQueueSize);
        Assert.Equal(defaults.Diagnostic.MaxSendIntervalMs, options.Diagnostic.MaxSendIntervalMs);
        Assert.Equal(defaults.Params.CfgPrefix, options.Params.CfgPrefix);
    }

    private static MavlinkServerOptions Bind(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services
            .AddOptions<MavlinkServerOptions>()
            .Bind(configuration.GetSection(MavlinkServerOptions.Section));
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<MavlinkServerOptions>>().Value;
    }
}
