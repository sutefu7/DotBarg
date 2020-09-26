using DotBarg.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DotBarg.Converters
{
    public class TreeNodeKindsToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var img = new BitmapImage(new Uri("/Images/Miscellaneousfile.png", UriKind.Relative));

            if (!(value is TreeNodeKinds))
                return img;

            var kinds = (TreeNodeKinds)value;
            switch (kinds)
            {
                case TreeNodeKinds.Folder: img = new BitmapImage(new Uri("/Images/Folder_Collapse.png", UriKind.Relative)); break;
                case TreeNodeKinds.SolutionFile: img = new BitmapImage(new Uri("/Images/Solution.png", UriKind.Relative)); break;

                case TreeNodeKinds.CSharpProjectFile: img = new BitmapImage(new Uri("/Images/CSharpProject.png", UriKind.Relative)); break;
                case TreeNodeKinds.VBNetProjectFile: img = new BitmapImage(new Uri("/Images/VBProject.png", UriKind.Relative)); break;

                case TreeNodeKinds.CSharpSourceFileForHeader: img = new BitmapImage(new Uri("/Images/CSharpFile.png", UriKind.Relative)); break;
                case TreeNodeKinds.VBNetSourceFileForHeader: img = new BitmapImage(new Uri("/Images/VBFile.png", UriKind.Relative)); break;
                case TreeNodeKinds.CSharpSourceFile: img = new BitmapImage(new Uri("/Images/CSharpFile.png", UriKind.Relative)); break;
                case TreeNodeKinds.VBNetSourceFile: img = new BitmapImage(new Uri("/Images/VBFile.png", UriKind.Relative)); break;
                case TreeNodeKinds.GeneratedFile: img = new BitmapImage(new Uri("/Images/Generatedfile.png", UriKind.Relative)); break;

                case TreeNodeKinds.Dependency: img = new BitmapImage(new Uri("/Images/Dependencies.png", UriKind.Relative)); break;

                case TreeNodeKinds.Namespace: img = new BitmapImage(new Uri("/Images/Namespace.png", UriKind.Relative)); break;
                case TreeNodeKinds.Class: img = new BitmapImage(new Uri("/Images/Class.png", UriKind.Relative)); break;
                case TreeNodeKinds.Struct: img = new BitmapImage(new Uri("/Images/Structure.png", UriKind.Relative)); break;
                case TreeNodeKinds.Interface: img = new BitmapImage(new Uri("/Images/Interface.png", UriKind.Relative)); break;
                case TreeNodeKinds.Module: img = new BitmapImage(new Uri("/Images/Module.png", UriKind.Relative)); break;
                
                case TreeNodeKinds.Enum: img = new BitmapImage(new Uri("/Images/Enum.png", UriKind.Relative)); break;
                case TreeNodeKinds.EnumItem: img = new BitmapImage(new Uri("/Images/EnumItem.png", UriKind.Relative)); break;
                
                case TreeNodeKinds.Delegate: img = new BitmapImage(new Uri("/Images/Delegate.png", UriKind.Relative)); break;
                case TreeNodeKinds.Event: img = new BitmapImage(new Uri("/Images/Event.png", UriKind.Relative)); break;
                case TreeNodeKinds.Field: img = new BitmapImage(new Uri("/Images/Field.png", UriKind.Relative)); break;
                case TreeNodeKinds.Property: img = new BitmapImage(new Uri("/Images/Property.png", UriKind.Relative)); break;
                case TreeNodeKinds.Operator: img = new BitmapImage(new Uri("/Images/Operator.png", UriKind.Relative)); break;
                case TreeNodeKinds.Method: img = new BitmapImage(new Uri("/Images/Method.png", UriKind.Relative)); break;
            }

            return img;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
