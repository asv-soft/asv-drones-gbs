namespace Asv.Drones.Gbs;

/// <summary>
/// Represents the configuration settings for the UbloxRtkModule.
/// </summary>
public class RtkDeviceOptions
{
    public const string Section = "Rtk";
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether this GNSS device is allowed to act as an RTK base.
    /// </summary>
    /// <remarks>
    /// When disabled, the device still provides telemetry, position and satellite status, but
    /// RTCMv3 output and RTK mode commands are not enabled.
    /// </remarks>
    public bool IsEnabledRtk { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether fixed RTK base mode should be started automatically.
    /// </summary>
    /// <remarks>
    /// The mode is started after a GNSS device with UBX microservice is initialized.
    /// Requires <see cref="IsEnabledRtk"/> to be enabled.
    /// </remarks>
    public bool IsAutoStartFixedMode { get; set; } = false;

    /// <summary>
    /// Gets or sets the fixed base latitude in degrees.
    /// </summary>
    /// <remarks>
    /// Auto-start fixed mode is skipped when latitude and longitude are both zero.
    /// </remarks>
    public double FixedModeLat { get; set; } = 0;

    /// <summary>
    /// Gets or sets the fixed base longitude in degrees.
    /// </summary>
    /// <remarks>
    /// Auto-start fixed mode is skipped when latitude and longitude are both zero.
    /// </remarks>
    public double FixedModeLon { get; set; } = 0;

    /// <summary>
    /// Gets or sets the fixed base altitude in meters.
    /// </summary>
    public double FixedModeAlt { get; set; } = 0;

    /// <summary>
    /// Gets or sets the fixed base position accuracy in meters.
    /// </summary>
    /// <remarks>
    /// Must be between 0.001 and 100 meters.
    /// </remarks>
    public float FixedModeAccuracy { get; set; } = 0.3f;

    /// <summary>
    /// Gets or sets the connection string for the serial port.
    /// </summary>
    /// <value>
    /// The connection string in the format: "serial:/dev/ttyACM0?br=115200".
    /// </value>
    public string ConnectionString { get; set; } = "serial:/dev/ttyACM0?br=115200";

    /// <summary>
    /// Gets or sets the rate at which the GBS status is updated in milliseconds.
    /// </summary>
    /// <value>
    /// The rate at which the GBS status is updated in milliseconds. The default value is 1000 milliseconds.
    /// </value>
    public int GbsStatusRateMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the rate at which the status is updated from the device, in milliseconds.
    /// </summary>
    /// <value>
    /// The rate at which the status is updated from the device, in milliseconds.
    /// </value>
    public int UpdateStatusFromDeviceRateMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the array of RTCMv3 message IDs to send.
    /// </summary>
    /// <remarks>
    /// The message IDs indicate the specific RTCMv3 messages that
    /// should be sent by the software. The array contains ushort values
    /// representing the message IDs.
    /// </remarks>
    /// <value>
    /// An array of ushort values representing RTCMv3 message IDs.
    /// </value>
    public ushort[] RtcmV3MessagesIdsToSend { get; set; } =
        { 1005, 1006, 1074, 1077, 1084, 1087, 1094, 1097, 1124, 1127, 1230, 4072 };

    /// <summary>
    /// Gets or sets the rate at which messages are processed in hertz (Hz).
    /// </summary>
    /// <remarks>
    /// The MessageRateHz property determines how often messages are processed.
    /// It represents the number of messages that can be processed in one second.
    /// By default, the value is set to 1 Hz.
    /// </remarks>
    /// <value>
    /// The rate at which messages are processed in hertz.
    /// </value>
    public byte MessageRateHz { get; set; } = 1;

    /// <summary>
    /// Gets or sets the timeout value in milliseconds for reconnecting.
    /// </summary>
    /// <value>
    /// The timeout value in milliseconds for reconnecting. The default value is 10,000 milliseconds.
    /// </value>
    public int ReconnectTimeoutMs { get; set; } = 10_000;
}
