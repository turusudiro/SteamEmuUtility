using System.Windows;
using System.Windows.Controls;

namespace XamlCommon
{
    public static class ColumnResizeBehavior
    {
        public static readonly DependencyProperty DisableColumnResizeProperty =
            DependencyProperty.RegisterAttached("DisableColumnResize", typeof(bool), typeof(ColumnResizeBehavior), new PropertyMetadata(false, OnDisableColumnResizeChanged));

        public static bool GetDisableColumnResize(DependencyObject obj)
        {
            return (bool)obj.GetValue(DisableColumnResizeProperty);
        }

        public static void SetDisableColumnResize(DependencyObject obj, bool value)
        {
            obj.SetValue(DisableColumnResizeProperty, value);
        }

        private static void OnDisableColumnResizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ListView listView && (bool)e.NewValue)
            {
                listView.AddHandler(GridViewColumnHeader.PreviewMouseMoveEvent, new RoutedEventHandler(ColumnHeader_PreviewMouseMove));
            }
        }

        private static void ColumnHeader_PreviewMouseMove(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}