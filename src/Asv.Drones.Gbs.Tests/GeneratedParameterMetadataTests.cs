using Asv.Drones.Gbs.Contracts;
using Asv.Mavlink;
using Asv.Mavlink.Common;

namespace Asv.Drones.Gbs.Tests;

public class GeneratedParameterMetadataTests
{
    [Fact]
    public void Params_ShouldMatchBoardParameterContract()
    {
        var expected = new[]
        {
            new ExpectedParameter(MavParams.BrdSysId, "BRD_SYS_ID", 0, 255, 1),
            new ExpectedParameter(MavParams.BrdComId, "BRD_COM_ID", 0, 255, 254),
            new ExpectedParameter(MavParams.BrdSerialNum, "BRD_SERIAL_NUM", 0, 9_999_999, 13),
            new ExpectedParameter(MavParams.BrdRebootCmd, "BRD_REBOOT_CMD", 0, 1, 0),
            new ExpectedParameter(MavParams.BrdShutdownCmd, "BRD_SHUTDOWN_CMD", 0, 1, 0),
            new ExpectedParameter(MavParams.BrdRestartCmd, "BRD_RESTART_CMD", 0, 1, 0),
            new ExpectedParameter(MavParams.BrdV2extOn, "BRD_V2EXT_ON", 0, 1, 1),
        };

        var actual = MavParams.Instance.Params.ToArray();

        Assert.Equal(expected.Length, actual.Length);
        Assert.Equal(expected.Select(x => x.Metadata), actual);

        foreach (var parameter in expected)
        {
            Assert.Equal(parameter.Name, parameter.Metadata.Name);
            Assert.Equal(MavParamType.MavParamTypeInt32, parameter.Metadata.Type);
            Assert.Equal(parameter.Minimum, (int)parameter.Metadata.MinValue);
            Assert.Equal(parameter.Maximum, (int)parameter.Metadata.MaxValue);
            Assert.Equal(parameter.Default, (int)parameter.Metadata.DefaultValue);
            Assert.Equal(1, (int)parameter.Metadata.Increment);
            Assert.Equal(MavParams.BOARD, parameter.Metadata.Group);
            Assert.Equal(MavParams.System, parameter.Metadata.Category);
            Assert.False(parameter.Metadata.RebootRequired);
            Assert.False(parameter.Metadata.Volatile);
        }
    }

    [Fact]
    public void Params_ShouldHaveUniqueNames()
    {
        var names = MavParams.Instance.Params.Select(x => x.Name).ToArray();

        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void V2ExtensionParameter_ShouldExposeEnabledAndDisabledValues()
    {
        var values = MavParams.BrdV2extOn.Values!;

        Assert.Collection(
            values,
            value =>
            {
                Assert.Equal(1, (int)value.Item1);
                Assert.Equal("Enabled", value.Item2);
            },
            value =>
            {
                Assert.Equal(0, (int)value.Item1);
                Assert.Equal("Disabled", value.Item2);
            }
        );
    }

    private sealed record ExpectedParameter(
        IMavParamTypeMetadata Metadata,
        string Name,
        int Minimum,
        int Maximum,
        int Default
    );
}
