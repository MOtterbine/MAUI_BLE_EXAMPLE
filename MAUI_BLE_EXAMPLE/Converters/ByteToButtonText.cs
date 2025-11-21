
using System.Globalization;


namespace MAUI_BLE_EXAMPLE.Converters;

public class ByteToSwitchText : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if ((int)value == 0x00)
        {
            return "Off";
        }
        return "On";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (string.Compare(((string)value).ToUpper(), "OFF") == 0x00) return 0x00;
        return 0x01;
    }
}
