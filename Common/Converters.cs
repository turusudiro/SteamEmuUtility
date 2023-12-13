using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace ConvertersCommon
{
    public class MultiBooleanAndConverterBothTrue : MarkupExtension, IMultiValueConverter
    {
        private static MultiBooleanAndConverterBothTrue _instance;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new MultiBooleanAndConverterBothTrue());
        }
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool firstvalue = values[0] as bool? ?? false;
            bool second = values[1] as bool? ?? false;
            if (firstvalue && second)
            {
                return true;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class MultiBooleanAndConverter : MarkupExtension, IMultiValueConverter
    {
        private static MultiBooleanAndConverter _instance;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new MultiBooleanAndConverter());
        }
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool firstvalue = values[0] as bool? ?? false;
            bool second = values[1] as bool? ?? false;
            if (firstvalue && !second)
            {
                return true;
            }
            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BooleanToVisibilityConverter : MarkupExtension, IValueConverter
    {
        private static BooleanToVisibilityConverter _instance;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new BooleanToVisibilityConverter());
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class InverseBooleanConverter : MarkupExtension, IValueConverter
    {
        private static InverseBooleanConverter _instance;
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new InverseBooleanConverter());
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
