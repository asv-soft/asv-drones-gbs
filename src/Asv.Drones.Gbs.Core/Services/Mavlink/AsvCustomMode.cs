namespace Asv.Drones.Gbs.Core;

[Flags]
public enum AsvGbsCompatibility:byte
{
    Unknown = 0b0000_0000,
    RtkMode = 0b0000_0001,
}
public struct AsvCustomMode
{
    public uint Value { get; private set; }
    public AsvCustomMode(uint value)
    {
        Value = value;
    }
    private const uint ClassMask = 0b0000_0000_0000_0000_0000_0000_1111_1111;
    private const int ClassOffset = 0;
    public AsvGbsCompatibility Compatibility
    {
        get => (AsvGbsCompatibility)((Value & ClassMask) >> ClassOffset);
        set => Value = (Value & ~ClassMask) | ((uint)value << ClassOffset) & ClassMask;
    }
}