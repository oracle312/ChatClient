// RoomNameConverter.cs
using System;
using System.Globalization;
using System.Windows.Data;

namespace AuthClient
{
    public class RoomNameConverter : IValueConverter
    {
        public string MyName { get; set; } = string.Empty;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var roomId = value?.ToString() ?? string.Empty;
            var names = roomId.Split('_');
            if (names.Length == 2)
                return names[0] == MyName ? names[1] : names[0];
            return roomId;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}



