using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace Miniräknare
{
    [ContentProperty(nameof(Content))]
    public class EnabledContentPresenter : ContentPresenter
    {
        static EnabledContentPresenter()
        {
            IsEnabledProperty.OverrideMetadata(
                typeof(EnabledContentPresenter),
                new UIPropertyMetadata(
                    defaultValue: true, 
                    EnabledContentPresenter_IsEnabledChanged,
                    CoerceIsEnabled));
        }

        private static void EnabledContentPresenter_IsEnabledChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(d);
            for (int i = 0; i < childrenCount; ++i)
            {
                var child = VisualTreeHelper.GetChild(d, i);
                child.CoerceValue(IsEnabledProperty);
            }
        }

        private static object CoerceIsEnabled(DependencyObject d, object baseValue)
        {
            //if (VisualTreeHelper.GetParent(d) is FrameworkElement parent && parent.IsEnabled == false)
            //{
            //    if (d.ReadLocalValue(IsEnabledProperty) == DependencyProperty.UnsetValue)
            //    {
            //        return true;
            //    }
            //}
            return baseValue;
        }
    }
}
