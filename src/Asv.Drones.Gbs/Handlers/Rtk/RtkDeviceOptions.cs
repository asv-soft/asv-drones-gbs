namespace Asv.Drones.Gbs;

/// <summary>
/// Represents the configuration settings for the UbloxRtkModule.
/// </summary>
public class RtkDeviceOptions
{
    public const string Section = "Rtk";
    public bool IsEnabled { get; set; } = false;

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
