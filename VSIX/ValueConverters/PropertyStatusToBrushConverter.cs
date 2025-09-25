using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using AltaSoft.Storm.Models;

namespace AltaSoft.Storm.ValueConverters;

public class PropertyStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not StormPropertyStatus status)
            return Binding.DoNothing;

        return status switch
        {
            StormPropertyStatus.NotChecked => Brushes.Gray,
            StormPropertyStatus.Ok => Brushes.Green,
            StormPropertyStatus.NullableMismatch => Brushes.Yellow,
            StormPropertyStatus.KeyMismatch => Brushes.Gold,
            StormPropertyStatus.DbTypePartiallyCompatible => Brushes.Orange,
            StormPropertyStatus.SizeMismatch => Brushes.DarkOrange,
            StormPropertyStatus.PrecisionMismatch => Brushes.DarkOrange,
            StormPropertyStatus.ScaleMismatch => Brushes.DarkOrange,
            StormPropertyStatus.DbTypeNotCompatible => Brushes.OrangeRed,
            StormPropertyStatus.ColumnMissing => Brushes.Red,
            StormPropertyStatus.DetailTableNotFound => Brushes.Red,
            _ => Brushes.White
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
