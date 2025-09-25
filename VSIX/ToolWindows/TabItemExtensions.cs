using System.Windows;
using System.Windows.Controls;

namespace AltaSoft.Storm.ToolWindows;

public class TabItemExtensions
{
    public static readonly DependencyProperty HoverTabTemplateProperty = DependencyProperty.RegisterAttached(
        "HoverTabTemplate", typeof(DataTemplate), typeof(TabItemExtensions), new PropertyMetadata(default(DataTemplate)));

    [AttachedPropertyBrowsableForType(typeof(TabItem))]
    public static void SetHoverTabTemplate(DependencyObject element, DataTemplate value)
    {
        element.SetValue(HoverTabTemplateProperty, value);
    }

    [AttachedPropertyBrowsableForType(typeof(TabItem))]
    public static DataTemplate GetHoverTabTemplate(DependencyObject element)
    {
        return (DataTemplate)element.GetValue(HoverTabTemplateProperty);
    }

    public static readonly DependencyProperty SelectedTabTemplateProperty = DependencyProperty.RegisterAttached(
        "SelectedTabTemplate", typeof(DataTemplate), typeof(TabItemExtensions), new PropertyMetadata(default(DataTemplate)));

    [AttachedPropertyBrowsableForType(typeof(TabItem))]
    public static void SetSelectedTabTemplate(DependencyObject element, DataTemplate value)
    {
        element.SetValue(SelectedTabTemplateProperty, value);
    }

    [AttachedPropertyBrowsableForType(typeof(TabItem))]
    public static DataTemplate GetSelectedTabTemplate(DependencyObject element)
    {
        return (DataTemplate)element.GetValue(SelectedTabTemplateProperty);
    }

    public static readonly DependencyProperty DefaultTabTemplateProperty = DependencyProperty.RegisterAttached(
        "DefaultTabTemplate", typeof(DataTemplate), typeof(TabItemExtensions), new PropertyMetadata(default(DataTemplate)));

    [AttachedPropertyBrowsableForType(typeof(TabItem))]
    public static void SetDefaultTabTemplate(DependencyObject element, DataTemplate value)
    {
        element.SetValue(DefaultTabTemplateProperty, value);
    }

    [AttachedPropertyBrowsableForType(typeof(TabItem))]
    public static DataTemplate GetDefaultTabTemplate(DependencyObject element)
    {
        return (DataTemplate)element.GetValue(DefaultTabTemplateProperty);
    }
}
