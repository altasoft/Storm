using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using AltaSoft.Storm.Models;

namespace AltaSoft.Storm.ValueConverters;

public class DbObjectStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DbObjectStatus status)
            return Binding.DoNothing;

        return status switch
        {
            DbObjectStatus.Ok => Brushes.Green,
            DbObjectStatus.Warning => Brushes.Yellow,
            DbObjectStatus.TypeNotFound => Brushes.Red,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
