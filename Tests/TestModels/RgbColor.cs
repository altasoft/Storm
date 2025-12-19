using System;
using AltaSoft.Storm.Attributes;

namespace AltaSoft.Storm.TestModels;

[StormStringEnum<RgbColor, RgbColorExt>(16)]
[Flags]
public enum RgbColor : sbyte
{
    Red = 1,
    Green = 2,
    Blue = 4,
    White = 8,
    Black = 16
}

internal sealed class RgbColorExt : IStormStringToEnumConverter<RgbColor>
{
    public static string ToDbString(RgbColor value)
    {
        return value switch
        {
            RgbColor.Red => "#FF0000",
            RgbColor.Green => "#00FF00",
            RgbColor.Blue => "#0000FF",
            RgbColor.White => "#FFFFFF",
            RgbColor.Black => "#000000",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }

    public static RgbColor FromDbString(string value)
    {
        return value.ToUpper() switch
        {
            "#FF0000" => RgbColor.Red,
            "#00FF00" => RgbColor.Green,
            "#0000FF" => RgbColor.Blue,
            "#FFFFFF" => RgbColor.White,
            "#000000" => RgbColor.Black,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }
}
