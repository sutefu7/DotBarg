using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotBarg.Libraries.DBs
{
    [DebuggerDisplay("{DefineName} : {DefineType}")]
    public class MethodArgument : TableBase
    {
        public bool IsByVal { get; set; } = false;

        public bool IsByRef { get; set; } = false;

        public bool IsIn { get; set; } = false;

        public bool IsOut { get; set; } = false;

        public bool IsParamArray { get; set; } = false;

        public bool IsOptional { get; set; } = false;

        public string DefineName { get; set; } = string.Empty;

        public string DefineType { get; set; } = string.Empty;

        public string DisplayDefineType
        {
            get { return base.ConvertCurrentLanguageType(DefineType); }
        }

        // int, ref int, params string[], [int]
        public string DisplaySyntax
        {
            get
            {
                var value = string.Empty;

                switch (AppEnv.Languages)
                {
                    case Languages.CSharp:
                        value = ToCSharpDisplaySyntax();
                        break;

                    case Languages.VBNet:
                        value = ToVBNetDisplaySyntax();
                        break;
                }

                return value;
            }
        }

        private string ToCSharpDisplaySyntax()
        {
            var sb = new StringBuilder();

            if (IsByRef)
                sb.Append("ref ");

            if (IsIn)
                sb.Append("in ");

            if (IsOut)
                sb.Append("out ");

            if (IsParamArray)
                sb.Append("params ");

            if (IsOptional)
                sb.Append("[");

            sb.Append(DisplayDefineType);

            if (IsOptional)
                sb.Append("]");


            return sb.ToString();
        }

        private string ToVBNetDisplaySyntax()
        {
            var sb = new StringBuilder();

            if (IsByRef)
                sb.Append("ByRef ");

            if (IsParamArray)
                sb.Append("ParamArray ");

            if (IsOptional)
                sb.Append("[");

            sb.Append(DisplayDefineType);

            if (IsOptional)
                sb.Append("]");


            return sb.ToString();
        }
    }
}
