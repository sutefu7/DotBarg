using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotBarg.Libraries.DBs
{
    public class LanguageConversion : TableBase
    {
        public string CSharpType { get; set; } = string.Empty;

        public string VBNetType { get; set; } = string.Empty;

        public static List<LanguageConversion> InitializeItems()
        {
            var items = new List<LanguageConversion>
            {
                new LanguageConversion { CSharpType = "bool", VBNetType = "Boolean" },
                new LanguageConversion { CSharpType = "byte", VBNetType = "Byte" },
                new LanguageConversion { CSharpType = "sbyte", VBNetType = "SByte" },
                new LanguageConversion { CSharpType = "char", VBNetType = "Char" },
                new LanguageConversion { CSharpType = "decimal", VBNetType = "Decimal" },
                new LanguageConversion { CSharpType = "double", VBNetType = "Double" },
                new LanguageConversion { CSharpType = "float", VBNetType = "Single" },
                new LanguageConversion { CSharpType = "int", VBNetType = "Integer" },
                new LanguageConversion { CSharpType = "uint", VBNetType = "UInteger" },
                new LanguageConversion { CSharpType = "long", VBNetType = "Long" },
                new LanguageConversion { CSharpType = "ulong", VBNetType = "ULong" },
                new LanguageConversion { CSharpType = "short", VBNetType = "Short" },
                new LanguageConversion { CSharpType = "ushort", VBNetType = "UShort" },
                new LanguageConversion { CSharpType = "string", VBNetType = "String" },
                new LanguageConversion { CSharpType = "object", VBNetType = "Object" },
                new LanguageConversion { CSharpType = "DateTime", VBNetType = "Date" },
                new LanguageConversion { CSharpType = "void", VBNetType = "Void" },
            };

            return items;
        }
    }
}
