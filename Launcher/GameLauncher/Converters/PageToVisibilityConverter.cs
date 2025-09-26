using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GameLauncher.Enums;

namespace GameLauncher.Converters
{
    public class PageToVisibilityConverter : IValueConverter
    {
        public PageType Page { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PageType currentPage)
                return currentPage == Page ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
