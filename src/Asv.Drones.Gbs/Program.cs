

using Asv.Drones.Gbs;
using Asv.Drones.Gbs.Contracts;
using Asv.Drones.Gbs.Gpio;
using Asv.Drones.Rsga;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
#if DEBUG
    //EnvironmentName = Environments.Development,
    EnvironmentName = "Virtual",
#else
    EnvironmentName = Environments.Production,
#endif
    Args = args,
});

builder
    .AddSystemTimeProvider()
    .AddExceptionHandler()
    .AddDefaultLogging()
    .AddUserConfig("usersettings.json")
    .AddMavlinkServer(MavParams.Instance)
    .AddMavlinkWorkModeHandler()
    .AddPrintWelcomeHandler()
    // System control
    .AddSystemControl()
    .AddSystemControlHandler()
    // LED
    .AddDefaultGpioService()
    .AddLedService()
    .AddLedHandler();
    
var host = builder.Build();

host.Start();

host.WaitForShutdown();