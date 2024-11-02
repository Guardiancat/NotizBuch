using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace _4._1
{
    internal class DatumConverter:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DateTime date)
            {
                string format = "dd.MM.yyyy";
                if (parameter != null && parameter.ToString() == "background")
                {
                    return date.Date == DateTime.Today ? Brushes.LightGreen : Brushes.Orange;
                }

                return date.ToString(format);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
