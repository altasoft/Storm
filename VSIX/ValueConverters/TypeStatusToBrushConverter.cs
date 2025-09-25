using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using AltaSoft.Storm.Models;

namespace AltaSoft.Storm.ValueConverters;

public class TypeStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not StormTypeStatus status)
            return Binding.DoNothing;

        return status switch
        {
            StormTypeStatus.Ok => Brushes.Green,
            StormTypeStatus.Warning => Brushes.Yellow,
            StormTypeStatus.TableNotFound => Brushes.Red,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
