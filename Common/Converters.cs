﻿using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace ConvertersCommon
{
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
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            throw new NotImplementedException();
        }
    }
    public class ItemsSourceCountToEnabledConverter : MarkupExtension, IValueConverter
    {
        private static ItemsSourceCountToEnabledConverter _instance;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _instance ?? (_instance = new ItemsSourceCountToEnabledConverter());
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable items)
            {
                return (items.GetEnumerator().MoveNext());
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
