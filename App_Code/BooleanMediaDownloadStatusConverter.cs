using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace PaperLess_Emeeting
{
    public class BooleanMediaDownloadStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(String))
            {
                throw new InvalidOperationException("it is not a string");
            }

            bool actualBoolean = Boolean.Parse(value.ToString());

            if (actualBoolean)
            {
                return "已下載";
            }
            else
            {
                return "未下載";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if(targetType != typeof(Brush
            return null;
        }
    }
}
