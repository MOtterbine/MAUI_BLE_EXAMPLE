
using System.Globalization;


namespace MAUI_BLE_EXAMPLE.Converters;

public class InvertedByteToSwitchText : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if ((int)value == 0x00)
        {
            return "On";
        }
        return "Off";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (string.Compare(((string)value).ToUpper(), "OFF") == 0x00) return 0x01;
        return 0x00;
    }
}
