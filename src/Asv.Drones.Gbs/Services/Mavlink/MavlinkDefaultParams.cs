using System.ComponentModel.Composition;
using Asv.Mavlink;
using Asv.Mavlink.V2.Common;

namespace Asv.Drones.Gbs;

/// Static class for storing default parameters for Mavlink.
/// /
public static class MavlinkDefaultParams
{
    /// <summary>
    /// Represents the constant string value for the group name.
    /// </summary>
    public const string Group = "GBS";

    /// <summary>
    /// Represents the category of an item.
    /// </summary>
    public const string Category = "Common";

    /// <summary>
    /// Represents the metadata for the BoardSerialNumber parameter.
    /// </summary>
    [Export(typeof(IMavParamTypeMetadata))]
    public static IMavParamTypeMetadata BoardSerialNumber =  new MavParamTypeMetadata("BRD_SERIAL_NUM", MavParamType.MavParamTypeInt32)
    {
        Group = Group,
        Category = Category,
        ShortDesc = "Serial number",
        LongDesc = "Board serial number",
        Units = null,
        RebootRequired = false,
        MinValue = Int32.MinValue,
        MaxValue = Int32.MaxValue,
        DefaultValue = 0,
        Increment = 1,
    };
}