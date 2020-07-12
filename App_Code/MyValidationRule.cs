using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace PaperLess_Emeeting.App_Code.ValidationRule
{
    public class MyValidationRule : System.Windows.Controls.ValidationRule
    {
	    public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
	    {
		    double d = 0.0;
		    if (double.TryParse((string)value, out d) && d >= 20 && d <= 35)
		    {
			    return new ValidationResult(true, "OK");
		    }
		    else
		    {
			    return new ValidationResult(false, "Error");
		    }
	    }
    }
}
