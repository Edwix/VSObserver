using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace VSObserver
{
    class StringToBackground : IValueConverter
    {
        public bool IsReversed { get; set; }
        public bool UseHidden { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string val = System.Convert.ToString(value, CultureInfo.InvariantCulture);

            if (val == null)
            {
                return new SolidColorBrush(Colors.AliceBlue);
            }
            
            if (val.Equals(VariableObserver.F_VAL))
            {
                return new SolidColorBrush(Color.FromRgb(254,46,46));
            }
            else
            {
                return new SolidColorBrush(Colors.Violet);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
