using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotBarg.Libraries.DBs
{
    public class TableBase
    {
        public string ConvertCurrentLanguageType(string value)
        {
            value = ConvertGenericTypes(value);
            value = ConvertArrayTypes(value);
            value = ConvertPrimitiveTypes(value);

            return value;
        }

        private string ConvertGenericTypes(string value)
        {
            switch (AppEnv.Languages)
            {
                case Languages.VBNet:

                    // Class1<T>, Class1<T, U>
                    // Func1<T>, Func1<T, U>
                    // ↓
                    // Class1(Of T), Class1(Of T, U)
                    // Func1(Of T), Func1(Of T, U)
                    value = value.Replace("<", "(Of ");
                    value = value.Replace(">", ")");
                    break;
            }

            return value;
        }

        private string ConvertArrayTypes(string value)
        {
            switch (AppEnv.Languages)
            {
                case Languages.VBNet:

                    // items[], items[,]
                    // ↓
                    // items(), items(,)
                    value = value.Replace("[", "(");
                    value = value.Replace("]", ")");
                    break;
            }

            return value;
        }

        private string ConvertPrimitiveTypes(string value)
        {
            switch (AppEnv.Languages)
            {
                case Languages.VBNet:

                    // int
                    // ↓
                    // Integer

                    var items = AppEnv.LanguageConversions;

                    while (items.Any(x => value.Contains(x.CSharpType)))
                    {
                        var item = items.First(x => value.Contains(x.CSharpType));
                        value = value.Replace(item.CSharpType, item.VBNetType);
                    }

                    break;
            }

            return value;
        }
    }
}
