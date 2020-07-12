using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace PaperLess_Emeeting
{
    public class thumbNailListBoxWidthHeightConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(Double))
            {
                throw new InvalidOperationException("it is not a double");
            }

            double actualValue = Double.Parse(value.ToString());

            if (actualValue.Equals(0))
            {
                return 0;
            }

            double minusValue = Double.Parse(parameter.ToString());

            return actualValue - minusValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //if(targetType != typeof(Brush
            return null;
        }
    }
}
