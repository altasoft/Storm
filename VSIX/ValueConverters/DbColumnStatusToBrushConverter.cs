using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using AltaSoft.Storm.Models;

namespace AltaSoft.Storm.ValueConverters;

public class DbColumnStatusToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DbColumnStatus status)
            return Binding.DoNothing;

        return status switch
        {
            DbColumnStatus.Ok => Brushes.Green,
            DbColumnStatus.NullableMismatch => Brushes.Yellow,
            DbColumnStatus.KeyMismatch => Brushes.Gold,
            DbColumnStatus.SizeMismatch => Brushes.Orange,
            DbColumnStatus.DbTypeMismatch => Brushes.Coral,
            DbColumnStatus.ColumnMissing => Brushes.Red,
            _ => Brushes.Gray
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}
