using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Mooseware.CantorInABox;

/// <summary>
/// Converts a boolean value into a FontWeight (bold) value for binding UI appearance behaviour to a boolean property.
/// </summary>
public class FontBoldConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (targetType != typeof(FontWeight))
        {
            throw new ArgumentOutOfRangeException(nameof(targetType), "FontWeightConverter can only convert to FontWeight");
        }
        else
        {
            if (value is not null and bool)
            {
                bool bIsBold = (bool)value;
                return bIsBold ? FontWeights.Bold : FontWeights.Normal;
            }
            else
            {
                return FontWeights.Normal;
            }
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a boolean value into a System.Windows.Media.Brush value for binding UI appearance behaviour to a boolean property.
/// </summary>
public class ForgroundEnabledColourConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (targetType != typeof(System.Windows.Media.Brush))
        {
            throw new ArgumentOutOfRangeException(nameof(targetType), "ForgroundEnabledColourConverter can only convert to System.Windows.Media.Brush");
        }
        else
        {
            if (value is not null and bool)
            {
                bool bIsEnabled = (bool)value;
                return bIsEnabled ? new SolidColorBrush(Color.FromRgb(0, 0, 0)) : new SolidColorBrush(Color.FromRgb(196, 196, 196));
            }
            else
            {
                return new SolidColorBrush(Color.FromRgb(0, 0, 0));
            }
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a nullability check into a System.Windows.Media.Brush value for binding UI appearance behaviour to a boolean property.
/// </summary>
public class BackgroundNotNullColourConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (targetType != typeof(System.Windows.Media.Brush))
        {
            throw new ArgumentOutOfRangeException(nameof(targetType), "BackgroundNotNullColourConverter can only convert to System.Windows.Media.Brush");
        }
        else
        {
            if (value != null)
            {
                return new SolidColorBrush(Color.FromRgb(245, 245, 245));
            }
            else
            {
                return new SolidColorBrush(Color.FromRgb(220, 220, 220));
            }
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a boolean value into a System.Windows.Media.Brush value for binding UI appearance behaviour to a boolean property.
/// </summary>
public class BackgroundPlayingColourConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (targetType != typeof(System.Windows.Media.Brush))
        {
            throw new ArgumentOutOfRangeException(nameof(targetType), "BackgroundPlayingColourConverter can only convert to System.Windows.Media.Brush");
        }
        else
        {
            if (value is not null and bool)
            {
                bool bIsEnabled = (bool)value;
                return bIsEnabled ? new SolidColorBrush(Color.FromArgb(48, 144, 0, 0)) : new SolidColorBrush(Color.FromRgb(85, 85, 85));
            }
            else
            {
                return new SolidColorBrush(Color.FromRgb(85, 85, 85));
            }
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a boolean value into a System.Windows.Media.Brush value for binding UI appearance behaviour to a boolean property.
/// </summary>
public class ForegroundPlayingColourConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        if (targetType != typeof(System.Windows.Media.Brush))
        {
            throw new ArgumentOutOfRangeException(nameof(targetType), "BackgroundPlayingColourConverter can only convert to System.Windows.Media.Brush");
        }
        else
        {
            if (value is not null and bool)
            {
                bool bIsEnabled = (bool)value;
                return bIsEnabled ? new SolidColorBrush(Color.FromArgb(255, 221, 0, 0)) : new SolidColorBrush(Color.FromRgb(204, 204, 204));
            }
            else
            {
                return new SolidColorBrush(Color.FromRgb(204, 204, 204));
            }
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}