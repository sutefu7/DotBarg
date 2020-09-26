using DotBarg.Libraries;
using DotBarg.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DotBarg.Converters
{
    public class TreeNodeKindsToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is TreeNodeKinds))
                return string.Empty;

            var memberName = Enum.GetName(typeof(TreeNodeKinds), value);
            return memberName;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
