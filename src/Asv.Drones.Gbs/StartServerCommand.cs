using System.ComponentModel;
using Asv.Cfg.Json;
using NLog;
using Spectre.Console.Cli;

namespace Asv.Drones.Gbs;

/// <summary>
/// Represents a command to start the server.
/// </summary>
internal class StartServerCommand : Command<StartServerCommand.Settings>
{
    /// <summary>
    /// Represents an instance of a logger used for logging messages.
    /// </summary>
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Represents the settings for the command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the path to the configuration file.
        /// </summary>
        /// <value>
        /// The path to the configuration file.
        /// </value>
        /// <remarks>
        /// This property represents the path to the configuration file that will be used
        /// by the application. By default, it is set to "config.json". The file should be
        /// in JSON format.
        /// </remarks>
        /// <seealso cref="ConfigFilePath"/>
        /// <seealso cref="LoadConfigFile"/>
        /// <seealso cref="SaveConfigFile"/>
        [Description("Config file path")]
        [CommandArgument(0, "[config_file]")]
        public string ConfigFilePath { get; init; } = "config.json";
    }

    /// <summary>
    /// Executes a command in the given context with the specified settings.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The settings for executing the command.</param>
    /// <returns>Returns the execution result as an integer. Zero indicates successful execution.</returns>
    public override int Execute(CommandContext context, Settings settings)
    {
        using var cfgSvc = new JsonOneFileConfiguration(settings.ConfigFilePath, true, null);
        
        
        var waitForProcessShutdownStart = new ManualResetEventSlim();
        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            // We got a SIGTERM, signal that graceful shutdown has started
            waitForProcessShutdownStart.Set();
        };
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            _logger.Info($"Cancel key pressed=> shutdown server");
            waitForProcessShutdownStart.Set();
        };

        using var gbsService = new GbsService(cfgSvc);

        // Wait for shutdown to start
        waitForProcessShutdownStart.Wait();


        return 0;
    }
}