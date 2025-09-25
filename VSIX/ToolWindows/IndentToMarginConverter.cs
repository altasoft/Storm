using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AltaSoft.Storm.ToolWindows
{
    public sealed class IndentToMarginConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return new Thickness((double)1, 0, 0, 0);

            //return new Thickness((double)(value ?? 0), 0, 0, 0);
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="value">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <param name="targetType">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <param name="parameter">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <param name="culture">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        /// <returns>This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</returns>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            Debug.Assert(false, "IndentToMarginConverter can only be used for forward conversion.");
            return null;
        }
    }
}
