using Asv.Drones.Gbs;
using Asv.Drones.Gbs.Contracts;
using Asv.Drones.Gbs.Gpio;
using Asv.Drones.Rsga;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(
    new HostApplicationBuilderSettings {
#if DEBUG
        // EnvironmentName = Environments.Development,
        EnvironmentName = "Virtual",
#else
        EnvironmentName = Environments.Production,
#endif
#pragma warning disable SA1413
        Args = args }
#pragma warning restore SA1413
);

builder
    .AddSystemTimeProvider()
    .AddExceptionHandler()
    .AddDefaultLogging()
    .AddUserConfig("usersettings.json")
    .AddMavlinkServer(MavParams.Instance)
    .AddMavlinkWorkModeHandler()
    .AddPrintWelcomeHandler()
    /* System control */
    .AddSystemControl()
    .AddSystemControlHandler()
    /* LED */
    .AddDefaultGpioService()
    .AddLedService()
    .AddLedHandler()
    /* RTK */
    .AddUBloxConnectionService()
    .AddRtkHandler();

var host = builder.Build();

host.Start();

host.WaitForShutdown();
