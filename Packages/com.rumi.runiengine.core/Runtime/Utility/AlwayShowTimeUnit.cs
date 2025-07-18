#nullable enable
namespace RuniEngine
{
    public enum AlwayShowTimeUnit
    {
        none = 0b000,
        minute = 0b001,
        hour = 0b010 | minute, //0b011
        day = 0b100 | hour,    //0b111
    }
}
