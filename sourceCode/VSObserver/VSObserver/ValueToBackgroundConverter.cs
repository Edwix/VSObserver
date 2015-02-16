using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace VSObserver
{
    class ValueToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                bool hasChanged = (bool)value;

                if (hasChanged)
                {
                    return Brushes.LightBlue;
                }
                else
                {
                    return Brushes.Transparent;
                }
            }
            //value is not an integer. Do not throw an exception in the converter, but return something that is obviousl ywrong
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
